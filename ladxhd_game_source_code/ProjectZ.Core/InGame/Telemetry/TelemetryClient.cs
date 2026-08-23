using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectZ.InGame.Telemetry
{
    public enum TelemetryGameState
    {
        Startup,
        Menu,
        Gameplay,
        Shutdown,
        Unknown,
    }

    public enum TelemetryConnectionError
    {
        Network,
        Authentication,
        SeedMismatch,
        Protocol,
        Timeout,
        Unknown,
    }

    public enum TelemetryDisconnectReason
    {
        Network,
        Server,
        Client,
        Protocol,
        Unknown,
    }

    public sealed class TelemetryClientOptions
    {
        public Uri Endpoint { get; init; }
        public string StorageRoot { get; init; }
        public string AppVersion { get; init; }
        public string Platform { get; init; }
        public bool DiagnosticsEnabled { get; init; }
        public bool RandomizerEnabled { get; init; }
        public HttpClient HttpClient { get; init; }
        public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Opt-in telemetry transport with a bounded durable queue. Its public API accepts only
    /// typed, aggregate values so arbitrary logs and randomizer secrets cannot be uploaded.
    /// </summary>
    public sealed class TelemetryClient : IDisposable
    {
        private const int MaxQueueEvents = 256;
        private const int MaxQueueBytes = 512 * 1024;
        private const int BatchSize = 20;
        private static readonly TimeSpan InstallationRotation = TimeSpan.FromDays(30);
        private static readonly Regex VersionPattern = new("^[0-9A-Za-z][0-9A-Za-z.+_-]{0,31}$", RegexOptions.CultureInvariant);
        private static readonly Regex WorldVersionPattern = new("^[0-9]{1,4}(?:\\.[0-9A-Za-z_-]{1,16}){0,3}$", RegexOptions.CultureInvariant);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null,
        };

        private readonly object _queueLock = new();
        private readonly SemaphoreSlim _flushLock = new(1, 1);
        private readonly Uri _endpoint;
        private readonly string _storageDirectory;
        private readonly string _installationPath;
        private readonly string _queuePath;
        private readonly string _appVersion;
        private readonly string _platform;
        private readonly string _sessionId = Guid.NewGuid().ToString("D");
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly Timer _timer;
        private volatile bool _diagnosticsEnabled;
        private volatile bool _randomizerEnabled;
        private bool _disposed;

        public TelemetryClient(TelemetryClientOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.Endpoint == null || !IsSafeEndpoint(options.Endpoint))
                throw new ArgumentException("Telemetry endpoint must use HTTPS (or localhost for tests).", nameof(options));
            if (string.IsNullOrWhiteSpace(options.StorageRoot))
                throw new ArgumentException("Telemetry storage root is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.AppVersion) || !VersionPattern.IsMatch(options.AppVersion))
                throw new ArgumentException("Invalid application version.", nameof(options));
            if (options.Platform is not ("android" or "windows" or "linux" or "macos"))
                throw new ArgumentException("Unsupported telemetry platform.", nameof(options));

            _endpoint = options.Endpoint;
            _storageDirectory = Path.Combine(options.StorageRoot, "Telemetry");
            _installationPath = Path.Combine(_storageDirectory, "installation.json");
            _queuePath = Path.Combine(_storageDirectory, "pending.jsonl");
            _appVersion = options.AppVersion;
            _platform = options.Platform;
            _diagnosticsEnabled = options.DiagnosticsEnabled;
            _randomizerEnabled = options.RandomizerEnabled;
            _httpClient = options.HttpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _ownsHttpClient = options.HttpClient == null;

            var interval = options.FlushInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : options.FlushInterval;
            _timer = new Timer(_ => _ = FlushSafelyAsync(), null, interval, interval);
            RemoveEventsWithoutConsent();
        }

        public bool DiagnosticsEnabled => _diagnosticsEnabled;
        public bool RandomizerEnabled => _randomizerEnabled;

        public int PendingCount
        {
            get
            {
                lock (_queueLock)
                    return ReadQueueUnsafe().Count;
            }
        }

        public bool HasPendingCrash
        {
            get
            {
                lock (_queueLock)
                    return ReadQueueUnsafe().Any(entry => entry.Name == "crash");
            }
        }

        public void SetConsent(bool diagnosticsEnabled, bool randomizerEnabled)
        {
            ThrowIfDisposed();
            _diagnosticsEnabled = diagnosticsEnabled;
            _randomizerEnabled = randomizerEnabled;
            RemoveEventsWithoutConsent();
        }

        public void RecordAppStarted(string launchSource, bool previousCrash)
        {
            var source = launchSource is "companion" or "direct" or "resume" ? launchSource : "unknown";
            Record("diagnostics", "app_started", new()
            {
                ["launch_source"] = source,
                ["previous_crash"] = previousCrash,
            });
        }

        public void RecordAppStopped(int runtimeSeconds) =>
            Record("diagnostics", "app_stopped", new() { ["runtime_seconds"] = Clamp(runtimeSeconds, 0, 604800) });

        public void RecordCrash(Exception exception, TelemetryGameState state, bool fatal)
        {
            if (exception == null)
                return;

            var typeName = exception.GetType().FullName;
            if (string.IsNullOrEmpty(typeName) || !IsSafeTypeName(typeName))
                typeName = "System.Exception";
            var stackMaterial = exception.StackTrace;
            if (string.IsNullOrEmpty(stackMaterial))
                stackMaterial = typeName;

            Record("diagnostics", "crash", new()
            {
                ["exception_type"] = typeName,
                ["stack_hash"] = Sha256(stackMaterial),
                ["game_state"] = ToWireName(state),
                ["fatal"] = fatal,
            });
        }

        public void RecordConnectAttempt(int attempt) =>
            Record("randomizer", "ap_connect_attempt", new() { ["attempt"] = Clamp(attempt, 1, 1000) });

        public void RecordConnectSuccess(int attempt, int durationMs, string worldVersion)
        {
            var attributes = new Dictionary<string, object>
            {
                ["attempt"] = Clamp(attempt, 1, 1000),
                ["duration_ms"] = Clamp(durationMs, 0, 3600000),
            };
            AddWorldVersion(attributes, worldVersion);
            Record("randomizer", "ap_connect_success", attributes);
        }

        public void RecordConnectFailure(int attempt, int durationMs, TelemetryConnectionError category) =>
            Record("randomizer", "ap_connect_failure", new()
            {
                ["attempt"] = Clamp(attempt, 1, 1000),
                ["duration_ms"] = Clamp(durationMs, 0, 3600000),
                ["error_category"] = ToWireName(category),
            });

        public void RecordDisconnected(int connectedSeconds, TelemetryDisconnectReason category) =>
            Record("randomizer", "ap_disconnected", new()
            {
                ["connected_seconds"] = Clamp(connectedSeconds, 0, 604800),
                ["reason_category"] = ToWireName(category),
            });

        public void RecordReconnectScheduled(int attempt, int delaySeconds) =>
            Record("randomizer", "ap_reconnect_scheduled", new()
            {
                ["attempt"] = Clamp(attempt, 1, 1000),
                ["delay_seconds"] = Clamp(delaySeconds, 0, 3600),
            });

        public void RecordSessionSummary(
            int connectedSeconds,
            int disconnectCount,
            int reconnectCount,
            int checksReported,
            int itemsReceived,
            int unsupportedItems) =>
            Record("randomizer", "ap_session_summary", new()
            {
                ["connected_seconds"] = Clamp(connectedSeconds, 0, 604800),
                ["disconnect_count"] = Clamp(disconnectCount, 0, 1000000),
                ["reconnect_count"] = Clamp(reconnectCount, 0, 1000000),
                ["checks_reported"] = Clamp(checksReported, 0, 1000000),
                ["items_received"] = Clamp(itemsReceived, 0, 1000000),
                ["unsupported_items"] = Clamp(unsupportedItems, 0, 1000000),
            });

        public void RecordRandomizerManifest(
            string worldVersion,
            string logic,
            bool? tradeQuest,
            bool? rooster,
            bool? warpToStart)
        {
            var normalizedLogic = logic is "normal" or "hard" or "glitched" or "hell" ? logic : "unknown";
            var attributes = new Dictionary<string, object>
            {
                ["logic"] = normalizedLogic,
            };
            if (tradeQuest.HasValue)
                attributes["trade_quest"] = tradeQuest.Value;
            if (rooster.HasValue)
                attributes["rooster"] = rooster.Value;
            if (warpToStart.HasValue)
                attributes["warp_to_start"] = warpToStart.Value;
            AddWorldVersion(attributes, worldVersion);
            Record("randomizer", "randomizer_manifest", attributes);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!_diagnosticsEnabled && !_randomizerEnabled)
                return;

            await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                List<TelemetryQueueEntry> batch;
                string installationId;
                lock (_queueLock)
                {
                    var entries = ReadQueueUnsafe();
                    batch = entries.Where(HasConsentFor).Take(BatchSize).ToList();
                    if (batch.Count == 0)
                        return;
                    installationId = GetInstallationIdUnsafe();
                }

                var envelope = new TelemetryEnvelope
                {
                    SchemaVersion = 1,
                    InstallationId = installationId,
                    SessionId = _sessionId,
                    AppVersion = _appVersion,
                    Platform = _platform,
                    Events = batch,
                };
                using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(envelope, JsonOptions), Encoding.UTF8, "application/json"),
                };
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.Accepted ||
                    response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                {
                    var acceptedIds = batch.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
                    lock (_queueLock)
                    {
                        var remaining = ReadQueueUnsafe().Where(entry => !acceptedIds.Contains(entry.Id)).ToList();
                        WriteQueueUnsafe(remaining);
                    }
                }
            }
            finally
            {
                _flushLock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _timer.Dispose();
            _flushLock.Dispose();
            if (_ownsHttpClient)
                _httpClient.Dispose();
        }

        private void Record(string category, string name, Dictionary<string, object> attributes)
        {
            if (_disposed || (category == "diagnostics" ? !_diagnosticsEnabled : !_randomizerEnabled))
                return;
            try
            {
                var entry = new TelemetryQueueEntry
                {
                    Id = Guid.NewGuid().ToString("D"),
                    OccurredAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    Category = category,
                    Name = name,
                    Attributes = attributes,
                };
                lock (_queueLock)
                {
                    Directory.CreateDirectory(_storageDirectory);
                    var entries = ReadQueueUnsafe();
                    entries.Add(entry);
                    TrimQueue(entries);
                    WriteQueueUnsafe(entries);
                }
            }
            catch
            {
                // Telemetry must never affect game execution.
            }
        }

        private async Task FlushSafelyAsync()
        {
            try
            {
                await FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                // Offline play and endpoint failures leave the bounded queue intact.
            }
        }

        private void RemoveEventsWithoutConsent()
        {
            try
            {
                lock (_queueLock)
                {
                    if (!File.Exists(_queuePath))
                        return;
                    WriteQueueUnsafe(ReadQueueUnsafe().Where(HasConsentFor).ToList());
                }
            }
            catch
            {
                // Consent changes must not crash the game. A disabled category is still
                // excluded from every upload even if local cleanup temporarily fails.
            }
        }

        private bool HasConsentFor(TelemetryQueueEntry entry) =>
            entry.Category == "diagnostics" ? _diagnosticsEnabled :
            entry.Category == "randomizer" && _randomizerEnabled;

        private List<TelemetryQueueEntry> ReadQueueUnsafe()
        {
            var entries = new List<TelemetryQueueEntry>();
            if (!File.Exists(_queuePath))
                return entries;
            try
            {
                foreach (var line in File.ReadLines(_queuePath))
                {
                    if (string.IsNullOrWhiteSpace(line) || Encoding.UTF8.GetByteCount(line) > 8192)
                        continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<TelemetryQueueEntry>(line, JsonOptions);
                        if (entry != null && IsValidStoredEntry(entry))
                            entries.Add(entry);
                    }
                    catch (JsonException)
                    {
                        // Skip a torn or corrupt line; a later rewrite removes it.
                    }
                }
            }
            catch (IOException)
            {
                return [];
            }
            return entries;
        }

        private void WriteQueueUnsafe(List<TelemetryQueueEntry> entries)
        {
            Directory.CreateDirectory(_storageDirectory);
            var temporaryPath = _queuePath + ".tmp";
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                foreach (var entry in entries)
                    writer.WriteLine(JsonSerializer.Serialize(entry, JsonOptions));
            }
            File.Move(temporaryPath, _queuePath, true);
        }

        private static void TrimQueue(List<TelemetryQueueEntry> entries)
        {
            while (entries.Count > MaxQueueEvents || SerializedSize(entries) > MaxQueueBytes)
            {
                var removable = entries.FindIndex(entry => entry.Name != "crash");
                entries.RemoveAt(removable >= 0 ? removable : 0);
            }
        }

        private static int SerializedSize(IEnumerable<TelemetryQueueEntry> entries) =>
            entries.Sum(entry => Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(entry, JsonOptions)) + 1);

        private string GetInstallationIdUnsafe()
        {
            Directory.CreateDirectory(_storageDirectory);
            try
            {
                if (File.Exists(_installationPath))
                {
                    var state = JsonSerializer.Deserialize<InstallationState>(File.ReadAllText(_installationPath), JsonOptions);
                    if (state != null && Guid.TryParse(state.Id, out _) &&
                        DateTimeOffset.TryParse(state.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdAt) &&
                        DateTimeOffset.UtcNow - createdAt < InstallationRotation)
                        return state.Id;
                }
            }
            catch
            {
                // Replace corrupt state with a new anonymous identifier.
            }

            var replacement = new InstallationState
            {
                Id = Guid.NewGuid().ToString("D"),
                CreatedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            };
            var temporaryPath = _installationPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(replacement, JsonOptions), new UTF8Encoding(false));
            File.Move(temporaryPath, _installationPath, true);
            return replacement.Id;
        }

        private static bool IsValidStoredEntry(TelemetryQueueEntry entry) =>
            Guid.TryParse(entry.Id, out _) &&
            DateTimeOffset.TryParse(entry.OccurredAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _) &&
            entry.Category is "diagnostics" or "randomizer" &&
            !string.IsNullOrWhiteSpace(entry.Name) &&
            entry.Attributes != null;

        private static bool IsSafeEndpoint(Uri endpoint) =>
            endpoint.IsAbsoluteUri &&
            endpoint.AbsolutePath == "/v1/events" &&
            (endpoint.Scheme == Uri.UriSchemeHttps ||
             (endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback));

        private static bool IsSafeTypeName(string value) =>
            value.Length <= 96 && value.All(character =>
                char.IsLetterOrDigit(character) || character is '_' or '.' or '+' or '`');

        private static void AddWorldVersion(Dictionary<string, object> attributes, string worldVersion)
        {
            if (!string.IsNullOrWhiteSpace(worldVersion) && WorldVersionPattern.IsMatch(worldVersion))
                attributes["world_version"] = worldVersion;
        }

        private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

        private static string Sha256(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string ToWireName<T>(T value) where T : struct, Enum =>
            Regex.Replace(value.ToString(), "(?<!^)([A-Z])", "_$1").ToLowerInvariant();

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private sealed class InstallationState
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("created_at")]
            public string CreatedAt { get; set; }
        }

        private sealed class TelemetryEnvelope
        {
            [JsonPropertyName("schema_version")]
            public int SchemaVersion { get; set; }

            [JsonPropertyName("installation_id")]
            public string InstallationId { get; set; }

            [JsonPropertyName("session_id")]
            public string SessionId { get; set; }

            [JsonPropertyName("app_version")]
            public string AppVersion { get; set; }

            [JsonPropertyName("platform")]
            public string Platform { get; set; }

            [JsonPropertyName("events")]
            public List<TelemetryQueueEntry> Events { get; set; }
        }

        private sealed class TelemetryQueueEntry
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("occurred_at")]
            public string OccurredAt { get; set; }

            [JsonPropertyName("category")]
            public string Category { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("attributes")]
            public Dictionary<string, object> Attributes { get; set; }
        }
    }
}
