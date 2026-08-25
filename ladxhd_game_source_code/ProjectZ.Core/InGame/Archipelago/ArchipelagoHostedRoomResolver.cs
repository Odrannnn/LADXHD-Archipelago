using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectZ.InGame.Archipelago
{
    /// <summary>
    /// Wakes an archipelago.gg hosted room and resolves its current websocket port.
    /// Hosted room URLs are stable even when the assigned server port changes.
    /// </summary>
    public sealed class ArchipelagoHostedRoomResolver
    {
        private const string OfficialHost = "archipelago.gg";
        private const int DefaultStatusAttempts = 8;
        private static readonly TimeSpan DefaultSpinUpDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan DefaultPollDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan DefaultTotalTimeout = TimeSpan.FromSeconds(20);
        private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();

        private readonly HttpClient _httpClient;
        private readonly TimeSpan _spinUpDelay;
        private readonly TimeSpan _pollDelay;
        private readonly TimeSpan _totalTimeout;
        private readonly int _statusAttempts;

        public ArchipelagoHostedRoomResolver()
            : this(SharedHttpClient, DefaultSpinUpDelay, DefaultPollDelay, DefaultTotalTimeout,
                DefaultStatusAttempts)
        {
        }

        public ArchipelagoHostedRoomResolver(HttpClient httpClient, TimeSpan spinUpDelay,
            TimeSpan pollDelay, TimeSpan totalTimeout, int statusAttempts)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (spinUpDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(spinUpDelay));
            if (pollDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(pollDelay));
            if (totalTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(totalTimeout));
            if (statusAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(statusAttempts));

            _spinUpDelay = spinUpDelay;
            _pollDelay = pollDelay;
            _totalTimeout = totalTimeout;
            _statusAttempts = statusAttempts;
        }

        public static string NormalizeRoomUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("The Archipelago room page URL is empty.");
            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, OfficialHost, StringComparison.OrdinalIgnoreCase) ||
                !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidDataException(
                    "Use an official room page URL such as https://archipelago.gg/room/RoomId.");

            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2 ||
                !string.Equals(segments[0], "room", StringComparison.OrdinalIgnoreCase) ||
                !IsRoomId(segments[1]))
                throw new InvalidDataException(
                    "Use an official room page URL such as https://archipelago.gg/room/RoomId.");

            return $"https://{OfficialHost}/room/{segments[1]}";
        }

        public async Task<string> ResolveServerAsync(string roomUrl,
            CancellationToken cancellationToken = default)
        {
            var normalizedRoomUrl = NormalizeRoomUrl(roomUrl);
            var roomUri = new Uri(normalizedRoomUrl, UriKind.Absolute);
            var roomId = roomUri.AbsolutePath.Trim('/').Split('/')[1];
            var statusUri = new Uri($"https://{OfficialHost}/api/room_status/{roomId}");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_totalTimeout);
            try
            {
                using (var wakeResponse = await _httpClient.GetAsync(
                           roomUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                       .ConfigureAwait(false))
                {
                    if (!wakeResponse.IsSuccessStatusCode)
                        throw new InvalidDataException(
                            $"The room page returned HTTP {(int)wakeResponse.StatusCode}.");
                }

                if (_spinUpDelay > TimeSpan.Zero)
                    await Task.Delay(_spinUpDelay, timeout.Token).ConfigureAwait(false);

                for (var attempt = 0; attempt < _statusAttempts; attempt++)
                {
                    using var statusResponse = await _httpClient.GetAsync(
                            statusUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                        .ConfigureAwait(false);
                    if (!statusResponse.IsSuccessStatusCode)
                        throw new InvalidDataException(
                            $"The room status API returned HTTP {(int)statusResponse.StatusCode}.");

                    var json = await statusResponse.Content.ReadAsStringAsync(timeout.Token)
                        .ConfigureAwait(false);
                    var port = ParseLastPort(json);
                    if (port == -1)
                        throw new InvalidDataException(
                            "Archipelago could not host this room. Open its room page for details.");
                    if (port is > 0 and <= 65535)
                        return $"{OfficialHost}:{port}";
                    if (port != 0)
                        throw new InvalidDataException("The room status API returned an invalid port.");

                    if (attempt + 1 < _statusAttempts && _pollDelay > TimeSpan.Zero)
                        await Task.Delay(_pollDelay, timeout.Token).ConfigureAwait(false);
                }

                throw new TimeoutException("The hosted room did not finish waking up.");
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out while waking the hosted room.", ex);
            }
        }

        public static int ParseLastPort(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty("last_port", out var portElement) ||
                    !portElement.TryGetInt32(out var port))
                    throw new InvalidDataException("The room status response did not contain last_port.");
                return port;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("The room status response was not valid JSON.", ex);
            }
        }

        private static bool IsRoomId(string value)
        {
            if (value?.Length != 22 || value.Any(character =>
                    !(character is >= 'A' and <= 'Z') &&
                    !(character is >= 'a' and <= 'z') &&
                    !(character is >= '0' and <= '9') &&
                    character != '-' && character != '_'))
                return false;

            try
            {
                var base64 = value.Replace('-', '+').Replace('_', '/') + "==";
                return Convert.FromBase64String(base64).Length == 16;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static HttpClient CreateSharedHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LADXHD-Archipelago/2");
            return client;
        }
    }
}
