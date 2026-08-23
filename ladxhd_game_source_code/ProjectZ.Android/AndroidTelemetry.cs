using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ProjectZ.InGame.Telemetry;
using ProjectZ.InGame.Things;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ProjectZ.Android
{
    internal static class AndroidTelemetry
    {
        private const string PreferencesName = "telemetry_preferences";
        private const string ConsentVersionKey = "consent_version";
        private const string DiagnosticsEnabledKey = "diagnostics_enabled";
        private const string RandomizerEnabledKey = "randomizer_enabled";
        private const int CurrentConsentVersion = 1;

        private static readonly Stopwatch Runtime = new();
        private static int _crashRecorded;
        private static bool _handlersRegistered;
        private static bool _appStartedRecorded;
        private static string _launchSource = "unknown";
        private static bool _previousCrash;

        public static bool IsAvailable => TryGetEndpoint(out _);

        public static void Initialize(Activity activity, string storageRoot, string launchSource)
        {
            if (!TryGetEndpoint(out var endpoint))
                return;

            var preferences = GetPreferences(activity);
            var client = new TelemetryClient(new TelemetryClientOptions
            {
                Endpoint = endpoint,
                StorageRoot = storageRoot,
                AppVersion = Values.VersionString.TrimStart('v'),
                Platform = "android",
                DiagnosticsEnabled = preferences.GetBoolean(DiagnosticsEnabledKey, false),
                RandomizerEnabled = preferences.GetBoolean(RandomizerEnabledKey, false),
            });

            TelemetryManager.Configure(client);
            _launchSource = launchSource is "companion" or "direct" or "resume" ? launchSource : "unknown";
            _previousCrash = client.HasPendingCrash;
            _appStartedRecorded = false;
            Interlocked.Exchange(ref _crashRecorded, 0);
            Runtime.Restart();
            RegisterCrashHandlers();
            RecordStartIfEnabled();
            _ = FlushSafelyAsync();

            if (preferences.GetInt(ConsentVersionKey, 0) < CurrentConsentVersion)
                activity.RunOnUiThread(() => ShowConsentDialog(activity, firstRun: true));
        }

        public static void ShowConsentDialog(Activity activity, bool firstRun)
        {
            if (!IsAvailable || activity.IsFinishing)
                return;

            var preferences = GetPreferences(activity);
            var density = activity.Resources?.DisplayMetrics?.Density ?? 1f;
            var padding = (int)(24 * density);
            var layout = new LinearLayout(activity) { Orientation = Orientation.Vertical };
            layout.SetPadding(padding, (int)(8 * density), padding, 0);

            layout.AddView(new TextView(activity)
            {
                Text = "Telemetry is optional and off by default. It never sends Archipelago server addresses or passwords, player/slot names, seed names, save data, exact item/location names, file paths, or raw logs. Anonymous IDs rotate every 30 days and stored events are deleted after 60 days. Cloudflare still processes your IP while accepting a request.",
                TextSize = 14,
            });

            var diagnostics = new CheckBox(activity)
            {
                Text = "Share crash diagnostics",
                Checked = preferences.GetBoolean(DiagnosticsEnabledKey, false),
            };
            var randomizer = new CheckBox(activity)
            {
                Text = "Share randomizer connection statistics",
                Checked = preferences.GetBoolean(RandomizerEnabledKey, false),
            };
            layout.AddView(diagnostics);
            layout.AddView(randomizer);

            var builder = new AlertDialog.Builder(activity)
                .SetTitle("Privacy & diagnostics")
                .SetView(layout)
                .SetPositiveButton("Save choices", (_, _) =>
                    ApplyConsent(activity, diagnostics.Checked, randomizer.Checked));

            if (firstRun)
            {
                builder.SetNegativeButton("Keep disabled", (_, _) => ApplyConsent(activity, false, false));
            }
            else
            {
                builder.SetNegativeButton("Cancel", (_, _) => { });
                builder.SetNeutralButton("Disable all", (_, _) => ApplyConsent(activity, false, false));
            }

            builder.Show();
        }

        public static void OnPause() => _ = FlushSafelyAsync();

        public static void OnFinishing()
        {
            var client = TelemetryManager.Client;
            if (client == null)
                return;
            Game1.GameManager?.ArchipelagoManager.OnApplicationStopping();
            client.RecordAppStopped((int)Math.Min(Runtime.Elapsed.TotalSeconds, 604800));
            _ = FlushSafelyAsync();
        }

        public static void Shutdown()
        {
            if (_handlersRegistered)
            {
                AndroidEnvironment.UnhandledExceptionRaiser -= OnAndroidUnhandledException;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
                _handlersRegistered = false;
            }
            Runtime.Stop();
            TelemetryManager.Shutdown();
        }

        private static void ApplyConsent(Activity activity, bool diagnosticsEnabled, bool randomizerEnabled)
        {
            GetPreferences(activity).Edit()
                .PutInt(ConsentVersionKey, CurrentConsentVersion)
                .PutBoolean(DiagnosticsEnabledKey, diagnosticsEnabled)
                .PutBoolean(RandomizerEnabledKey, randomizerEnabled)
                .Apply();

            var client = TelemetryManager.Client;
            client?.SetConsent(diagnosticsEnabled, randomizerEnabled);
            RecordStartIfEnabled();
            _ = FlushSafelyAsync();
        }

        private static void RecordStartIfEnabled()
        {
            var client = TelemetryManager.Client;
            if (_appStartedRecorded || client?.DiagnosticsEnabled != true)
                return;
            client.RecordAppStarted(_launchSource, _previousCrash);
            _appStartedRecorded = true;
        }

        private static void RegisterCrashHandlers()
        {
            if (_handlersRegistered)
                return;
            AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            _handlersRegistered = true;
        }

        private static void OnAndroidUnhandledException(object sender, RaiseThrowableEventArgs args) =>
            RecordCrashOnce(args.Exception, fatal: true);

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args) =>
            RecordCrashOnce(args.ExceptionObject as Exception, args.IsTerminating);

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args) =>
            TelemetryManager.RecordCrash(args.Exception, GetGameState(), fatal: false);

        private static void RecordCrashOnce(Exception exception, bool fatal)
        {
            if (exception == null || Interlocked.Exchange(ref _crashRecorded, 1) != 0)
                return;
            TelemetryManager.RecordCrash(exception, GetGameState(), fatal);
        }

        private static TelemetryGameState GetGameState()
        {
            if (!Game1.FinishedLoading)
                return TelemetryGameState.Startup;
            return Game1.InProgress ? TelemetryGameState.Gameplay : TelemetryGameState.Menu;
        }

        private static async Task FlushSafelyAsync()
        {
            try
            {
                await TelemetryManager.FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                // Network or shutdown failures leave the bounded queue for a later launch.
            }
        }

        private static ISharedPreferences GetPreferences(Context context) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private);

        private static bool TryGetEndpoint(out Uri endpoint)
        {
            var value = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "TelemetryEndpoint")?.Value;
            return Uri.TryCreate(value, UriKind.Absolute, out endpoint) &&
                   endpoint.Scheme == Uri.UriSchemeHttps &&
                   endpoint.AbsolutePath == "/v1/events";
        }
    }

    internal sealed class AndroidDiagnosticsSettingsService : IDiagnosticsSettingsService
    {
        private readonly WeakReference<Activity> _activity;

        public AndroidDiagnosticsSettingsService(Activity activity)
        {
            _activity = new WeakReference<Activity>(activity);
        }

        public bool IsAvailable => AndroidTelemetry.IsAvailable;

        public void Show()
        {
            if (!_activity.TryGetTarget(out var activity))
                return;
            activity.RunOnUiThread(() => AndroidTelemetry.ShowConsentDialog(activity, firstRun: false));
        }
    }
}
