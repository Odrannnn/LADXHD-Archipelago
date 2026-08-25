using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectZ.InGame.Archipelago
{
    public sealed class MagpieTrackerBridge : IDisposable
    {
        private const int MaximumHandshakeBytes = 16 * 1024;
        private const int MaximumMessageBytes = 64 * 1024;

        private readonly object _stateLock = new object();
        private readonly object _lifecycleLock = new object();
        private readonly int _port;
        private readonly Dictionary<int, MagpieItemContribution> _receivedItems =
            new Dictionary<int, MagpieItemContribution>();
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _checks = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<int, TrackerClient> _clients =
            new ConcurrentDictionary<int, TrackerClient>();

        private CancellationTokenSource _cancellation;
        private TcpListener _listener;
        private Dictionary<string, object> _slotData = new Dictionary<string, object>(StringComparer.Ordinal);
        private MagpieTrackerLocation? _location;
        private int _nextClientId;

        public MagpieTrackerBridge(int port = MagpieTrackerProtocol.DefaultPort)
        {
            _port = port;
            foreach (var id in MagpieTrackerProtocol.ItemIds)
                _items[id] = 0;
        }

        public int BoundPort { get; private set; }

        public void Configure(bool enabled, bool allowLan, ArchipelagoSeedManifest seed)
        {
            Stop();

            lock (_stateLock)
            {
                _receivedItems.Clear();
                _items.Clear();
                foreach (var id in MagpieTrackerProtocol.ItemIds)
                    _items[id] = 0;

                _checks.Clear();
                _location = null;
                if (seed != null)
                {
                    foreach (var location in seed.Locations)
                    {
                        var id = MagpieTrackerProtocol.GetCheckId(location);
                        if (!string.IsNullOrEmpty(id))
                            _checks[id] = false;
                    }
                }

                _slotData = CreateSlotData(seed);
            }

            if (enabled && seed != null)
                Start(allowLan);
        }

        public bool Start(bool allowLan)
        {
            lock (_lifecycleLock)
            {
                if (_listener != null)
                    return true;

                _cancellation = new CancellationTokenSource();
                _listener = new TcpListener(allowLan ? IPAddress.Any : IPAddress.Loopback, _port);
                try
                {
                    _listener.Start();
                    BoundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
                    _ = Task.Run(() => AcceptClientsAsync(_listener, _cancellation.Token));
                    return true;
                }
                catch
                {
                    _listener.Stop();
                    _listener = null;
                    BoundPort = 0;
                    _cancellation.Dispose();
                    _cancellation = null;
                    return false;
                }
            }
        }

        public void RecordReceivedItem(int index, string itemName)
        {
            if (index < 0)
                return;

            KeyValuePair<string, int>[] changes;
            lock (_stateLock)
            {
                if (_receivedItems.ContainsKey(index))
                    return;
                if (!MagpieTrackerProtocol.TryGetItemContribution(itemName, out var contribution))
                    return;

                var oldItems = new Dictionary<string, int>(_items, StringComparer.Ordinal);
                _receivedItems[index] = contribution;
                RebuildReceivedItemQuantities();
                changes = GetItemChanges(oldItems);
            }

            if (changes.Length > 0)
                Broadcast("items", MagpieTrackerProtocol.CreateItemsMessage(changes, diff: true));
        }

        public void SynchronizeReceivedItems(IEnumerable<string> itemNames)
        {
            if (itemNames == null)
                return;

            var index = 0;
            foreach (var itemName in itemNames)
                RecordReceivedItem(index++, itemName);
        }

        public void SetItemQuantity(string id, int quantity)
        {
            var changed = false;
            var difference = 0;
            lock (_stateLock)
            {
                if (!_items.TryGetValue(id, out var oldQuantity))
                    return;
                quantity = Math.Max(0, quantity);
                if (oldQuantity != quantity)
                {
                    _items[id] = quantity;
                    difference = quantity - oldQuantity;
                    changed = true;
                }
            }

            if (changed)
                Broadcast("items", MagpieTrackerProtocol.CreateItemsMessage(
                    new[] { new KeyValuePair<string, int>(id, difference) }, diff: true));
        }

        public void RecordCheck(ArchipelagoSeedLocation location, bool isChecked = true)
        {
            var id = MagpieTrackerProtocol.GetCheckId(location);
            if (string.IsNullOrEmpty(id))
                return;

            var changed = false;
            lock (_stateLock)
            {
                if (_checks.TryGetValue(id, out var oldValue) && oldValue != isChecked)
                {
                    _checks[id] = isChecked;
                    changed = true;
                }
            }

            if (changed)
                Broadcast("checks", MagpieTrackerProtocol.CreateChecksMessage(
                    new[] { new KeyValuePair<string, bool>(id, isChecked) }, diff: true));
        }

        public void SetLocation(MagpieTrackerLocation location)
        {
            lock (_stateLock)
            {
                if (_location.HasValue && _location.Value.Equals(location))
                    return;
                _location = location;
            }

            Broadcast("gps", MagpieTrackerProtocol.CreateLocationMessage(location));
        }

        public void Stop()
        {
            CancellationTokenSource cancellation;
            TcpListener listener;
            lock (_lifecycleLock)
            {
                cancellation = _cancellation;
                listener = _listener;
                _cancellation = null;
                _listener = null;
                BoundPort = 0;
            }

            try
            {
                cancellation?.Cancel();
                listener?.Stop();
            }
            catch
            {
                // Shutdown is best-effort and must never interfere with save switching.
            }

            foreach (var client in _clients.Values)
                client.Dispose();
            _clients.Clear();
            cancellation?.Dispose();
        }

        public void Dispose() => Stop();

        private async Task AcceptClientsAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient tcpClient = null;
                try
                {
                    tcpClient = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                    _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken));
                }
                catch (OperationCanceledException)
                {
                    tcpClient?.Dispose();
                    break;
                }
                catch (ObjectDisposedException)
                {
                    tcpClient?.Dispose();
                    break;
                }
                catch
                {
                    tcpClient?.Dispose();
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            TrackerClient client = null;
            var clientId = Interlocked.Increment(ref _nextClientId);
            try
            {
                tcpClient.NoDelay = true;
                var stream = tcpClient.GetStream();
                if (!await CompleteWebSocketHandshakeAsync(stream, cancellationToken).ConfigureAwait(false))
                    return;

                var webSocket = WebSocket.CreateFromStream(
                    stream, isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.FromSeconds(20));
                client = new TrackerClient(tcpClient, webSocket);
                _clients[clientId] = client;
                await ReceiveMessagesAsync(client, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // A tracker disconnect or malformed request must not affect gameplay or AP.
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client?.Dispose();
                if (client == null)
                    tcpClient.Dispose();
            }
        }

        private async Task ReceiveMessagesAsync(TrackerClient client, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            using var message = new MemoryStream();
            while (!cancellationToken.IsCancellationRequested && client.IsOpen)
            {
                var result = await client.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;
                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                message.Write(buffer, 0, result.Count);
                if (message.Length > MaximumMessageBytes)
                    return;
                if (!result.EndOfMessage)
                    continue;

                var json = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                message.SetLength(0);
                await HandleMessageAsync(client, json, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleMessageAsync(TrackerClient client, string json, CancellationToken cancellationToken)
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("type", out var typeElement))
                return;

            var type = typeElement.GetString();
            if (string.Equals(type, "handshake", StringComparison.Ordinal))
            {
                var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (document.RootElement.TryGetProperty("features", out var featuresElement) &&
                    featuresElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var feature in featuresElement.EnumerateArray())
                    {
                        if (feature.ValueKind == JsonValueKind.String)
                            features.Add(feature.GetString());
                    }
                }

                client.Features = features;
                await client.SendAsync(MagpieTrackerProtocol.CreateHandshakeAcknowledgement(), cancellationToken)
                    .ConfigureAwait(false);
                await client.SendAsync(CreateSlotDataMessage(), cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!string.Equals(type, "sendFull", StringComparison.Ordinal))
                return;

            if (client.Supports("items"))
                await client.SendAsync(CreateFullItemsMessage(), cancellationToken).ConfigureAwait(false);
            if (client.Supports("checks"))
                await client.SendAsync(CreateFullChecksMessage(), cancellationToken).ConfigureAwait(false);
            if (client.Supports("gps"))
            {
                var location = CreateFullLocationMessage();
                if (location != null)
                    await client.SendAsync(location, cancellationToken).ConfigureAwait(false);
            }
        }

        private string CreateFullItemsMessage()
        {
            lock (_stateLock)
                return MagpieTrackerProtocol.CreateItemsMessage(_items.ToArray(), diff: false);
        }

        private string CreateFullChecksMessage()
        {
            lock (_stateLock)
                return MagpieTrackerProtocol.CreateChecksMessage(_checks.ToArray(), diff: false);
        }

        private string CreateFullLocationMessage()
        {
            lock (_stateLock)
                return _location.HasValue
                    ? MagpieTrackerProtocol.CreateLocationMessage(_location.Value)
                    : null;
        }

        private string CreateSlotDataMessage()
        {
            lock (_stateLock)
                return JsonSerializer.Serialize(new { type = "slot_data", slot_data = _slotData });
        }

        private void Broadcast(string feature, string payload)
        {
            foreach (var pair in _clients.ToArray())
            {
                if (!pair.Value.Supports(feature))
                    continue;
                _ = SendToClientAsync(pair.Key, pair.Value, payload);
            }
        }

        private async Task SendToClientAsync(int clientId, TrackerClient client, string payload)
        {
            try
            {
                await client.SendAsync(payload, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                if (_clients.TryRemove(clientId, out var removed))
                    removed.Dispose();
            }
        }

        private void RebuildReceivedItemQuantities()
        {
            var dynamicRupees = _items.TryGetValue("RUPEE_COUNT", out var rupees) ? rupees : 0;
            foreach (var id in MagpieTrackerProtocol.ItemIds)
                _items[id] = 0;
            _items["RUPEE_COUNT"] = dynamicRupees;

            foreach (var contribution in _receivedItems.Values)
            {
                var quantity = (_items.TryGetValue(contribution.Id, out var current) ? current : 0) +
                               contribution.Quantity;
                _items[contribution.Id] = Math.Min(contribution.Maximum, quantity);
            }
        }

        private KeyValuePair<string, int>[] GetItemChanges(IReadOnlyDictionary<string, int> oldItems)
        {
            return _items
                .Where(item => !oldItems.TryGetValue(item.Key, out var oldValue) || oldValue != item.Value)
                .Select(item => new KeyValuePair<string, int>(item.Key,
                    item.Value - (oldItems.TryGetValue(item.Key, out var oldValue) ? oldValue : 0)))
                .ToArray();
        }

        private static Dictionary<string, object> CreateSlotData(ArchipelagoSeedManifest seed)
        {
            var slotData = new Dictionary<string, object>(StringComparer.Ordinal);
            if (seed == null)
                return slotData;

            slotData["seed_name"] = seed.SeedName;
            slotData["world_version"] = seed.WorldVersion;
            foreach (var option in seed.Options ?? new Dictionary<string, JsonElement>())
                slotData[option.Key] = NormalizeSlotDataOption(option.Key, option.Value);
            return slotData;
        }

        private static object NormalizeSlotDataOption(string optionName, JsonElement option)
        {
            if (option.ValueKind == JsonValueKind.True)
                return true;
            if (option.ValueKind == JsonValueKind.False)
                return false;
            if (option.ValueKind != JsonValueKind.Number || !option.TryGetInt32(out var numericValue))
                return option.Clone();

            // Archipelago manifests serialize Choice values numerically. Magpie's slot_data
            // parser expects the public AP option names and otherwise copies the number into
            // LADXR's string settings. A numeric logic value leaves Magpie with zero logic
            // levels and crashes its accessibility renderer at logics[0].
            if (optionName == "tradequest" || optionName == "rooster" ||
                optionName == "experimental_dungeon_shuffle")
                return numericValue != 0;

            string choiceName = null;
            switch (optionName)
            {
                case "logic":
                    choiceName = numericValue switch
                    {
                        1 => "normal",
                        2 => "hard",
                        3 => "glitched",
                        4 => "hell",
                        _ => null
                    };
                    break;
                case "goal":
                    choiceName = numericValue switch
                    {
                        1 => "instruments",
                        2 => "seashells",
                        3 => "open",
                        _ => null
                    };
                    break;
                case "experimental_entrance_shuffle":
                    choiceName = numericValue switch
                    {
                        0 => "none",
                        1 => "simple",
                        _ => null
                    };
                    break;
                case "hard_mode":
                    choiceName = numericValue switch
                    {
                        0 => "none",
                        1 => "oracle",
                        2 => "hero",
                        3 => "ohko",
                        _ => null
                    };
                    break;
                case "overworld":
                    choiceName = numericValue switch
                    {
                        0 => "normal",
                        1 => "open_mabe",
                        _ => null
                    };
                    break;
                case "shuffle_nightmare_keys":
                case "shuffle_small_keys":
                case "shuffle_maps":
                case "shuffle_compasses":
                case "shuffle_stone_beaks":
                case "shuffle_instruments":
                    choiceName = numericValue switch
                    {
                        0 => "original_dungeon",
                        1 => "own_dungeons",
                        2 => "own_world",
                        3 => "any_world",
                        4 => "different_world",
                        100 when optionName == "shuffle_instruments" => "vanilla",
                        _ => null
                    };
                    break;
            }

            return choiceName ?? (object)option.Clone();
        }

        private static async Task<bool> CompleteWebSocketHandshakeAsync(
            NetworkStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            using var request = new MemoryStream();
            while (request.Length < MaximumHandshakeBytes)
            {
                var count = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (count <= 0)
                    return false;
                request.Write(buffer, 0, count);

                var text = Encoding.ASCII.GetString(request.GetBuffer(), 0, (int)request.Length);
                var headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (headerEnd < 0)
                    continue;

                var key = text.Split(new[] { "\r\n" }, StringSplitOptions.None)
                    .Select(line => line.Split(new[] { ':' }, 2))
                    .Where(parts => parts.Length == 2 &&
                                    string.Equals(parts[0].Trim(), "Sec-WebSocket-Key",
                                        StringComparison.OrdinalIgnoreCase))
                    .Select(parts => parts[1].Trim())
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                var acceptBytes = SHA1.HashData(Encoding.ASCII.GetBytes(
                    key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
                var response = "HTTP/1.1 101 Switching Protocols\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               $"Sec-WebSocket-Accept: {Convert.ToBase64String(acceptBytes)}\r\n\r\n";
                var responseBytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(responseBytes.AsMemory(), cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private sealed class TrackerClient : IDisposable
        {
            private readonly TcpClient _tcpClient;
            private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
            private int _disposed;

            public TrackerClient(TcpClient tcpClient, WebSocket webSocket)
            {
                _tcpClient = tcpClient;
                WebSocket = webSocket;
            }

            public WebSocket WebSocket { get; }
            public HashSet<string> Features { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public bool IsOpen => WebSocket.State == WebSocketState.Open;
            public bool Supports(string feature) => Features.Contains(feature);

            public async Task SendAsync(string payload, CancellationToken cancellationToken)
            {
                var bytes = Encoding.UTF8.GetBytes(payload);
                await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (WebSocket.State == WebSocketState.Open)
                        await WebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text,
                            endOfMessage: true, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _sendLock.Release();
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;
                try
                {
                    WebSocket.Abort();
                    WebSocket.Dispose();
                    _tcpClient.Dispose();
                }
                catch
                {
                }
                _sendLock.Dispose();
            }
        }
    }
}
