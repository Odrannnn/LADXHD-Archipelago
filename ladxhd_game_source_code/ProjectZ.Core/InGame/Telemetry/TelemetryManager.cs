using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectZ.InGame.Telemetry
{
    public static class TelemetryManager
    {
        private static readonly object Sync = new();
        private static TelemetryClient _client;

        public static TelemetryClient Client
        {
            get
            {
                lock (Sync)
                    return _client;
            }
        }

        public static void Configure(TelemetryClient client)
        {
            lock (Sync)
            {
                var previous = _client;
                _client = client;
                previous?.Dispose();
            }
        }

        public static void RecordCrash(Exception exception, TelemetryGameState state, bool fatal) =>
            Client?.RecordCrash(exception, state, fatal);

        public static async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            var client = Client;
            if (client != null)
                await client.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public static void Shutdown()
        {
            lock (Sync)
            {
                var previous = _client;
                _client = null;
                previous?.Dispose();
            }
        }
    }
}
