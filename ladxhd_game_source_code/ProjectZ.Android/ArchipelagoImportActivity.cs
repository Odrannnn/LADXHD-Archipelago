using System;
using System.IO;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Text;
using Android.Text.Method;
using Android.Views;
using Android.Widget;
using ProjectZ.InGame.Archipelago;

namespace ProjectZ.Android
{
    [Activity(
        Name = "com.zelda.ladxhd.archipelago.ArchipelagoImportActivity",
        Label = "Import LADXHD Archipelago Seed",
        Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
        Exported = true,
        LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "content",
        DataMimeType = "application/x-apladxhd")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "content",
        DataMimeType = "application/octet-stream",
        DataPathPattern = @".*\.apladxhd")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "content",
        DataMimeType = "application/json",
        DataPathPattern = @".*\.apladxhd")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "file",
        DataMimeType = "application/octet-stream",
        DataPathPattern = @".*\.apladxhd")]
    [IntentFilter(
        new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "application/x-apladxhd")]
    [IntentFilter(
        new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "application/octet-stream")]
    [IntentFilter(
        new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "application/json")]
    [IntentFilter(
        new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "text/plain")]
    public sealed class ArchipelagoImportActivity : Activity
    {
        public const string ExtraServer = "com.zelda.ladxhd.archipelago.extra.SERVER";
        public const string ExtraPassword = "com.zelda.ladxhd.archipelago.extra.PASSWORD";
        public const string ExtraSaveSlot = "com.zelda.ladxhd.archipelago.extra.SAVE_SLOT";

        private const string LegacyExtraServer = "com.zelda.ladxhd.extra.SERVER";
        private const string LegacyExtraPassword = "com.zelda.ladxhd.extra.PASSWORD";
        private const string LegacyExtraSaveSlot = "com.zelda.ladxhd.extra.SAVE_SLOT";

        private const long MaximumSeedBytes = 16 * 1024 * 1024;
        private const int MaximumServerCharacters = 512;
        private const int MaximumPasswordCharacters = 1024;

        private string _temporarySeedPath;
        private ArchipelagoSeedManifest _seed;
        private string _userDataRoot;
        private string _intentServer;
        private string _intentPassword;
        private int? _intentSaveSlot;
        private bool _hasIntentServer;
        private bool _hasIntentPassword;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window?.SetSoftInputMode(SoftInput.AdjustResize);
            ImportFromIntent(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            DeleteTemporarySeed();
            ImportFromIntent(intent);
        }

        protected override void OnDestroy()
        {
            DeleteTemporarySeed();
            base.OnDestroy();
        }

        private void ImportFromIntent(Intent intent)
        {
            try
            {
                var uri = GetSharedUri(intent) ??
                          throw new InvalidDataException("No randomizer file was supplied.");
                ReadConnectionHints(intent);

                _userDataRoot = Application.Context.GetExternalFilesDir(null)?.AbsolutePath ??
                                throw new IOException("Android did not provide an app data directory.");
                var archipelagoDirectory = ArchipelagoConnectionSettings.GetDirectory(_userDataRoot);
                Directory.CreateDirectory(archipelagoDirectory);

                _temporarySeedPath = Path.Combine(
                    archipelagoDirectory,
                    $"seed.import-{Guid.NewGuid():N}.apladxhd");
                CopyUriToFile(uri, _temporarySeedPath);
                _seed = ArchipelagoSeedManifest.Load(_temporarySeedPath);

                ShowImportDialog();
            }
            catch (Exception ex)
            {
                DeleteTemporarySeed();
                ShowError("Could not import randomizer", ex.Message);
            }
        }

        private void ReadConnectionHints(Intent intent)
        {
            var serverExtra = GetPresentExtra(intent, ExtraServer, LegacyExtraServer);
            var passwordExtra = GetPresentExtra(intent, ExtraPassword, LegacyExtraPassword);
            var saveSlotExtra = GetPresentExtra(intent, ExtraSaveSlot, LegacyExtraSaveSlot);

            _hasIntentServer = serverExtra != null;
            _hasIntentPassword = passwordExtra != null;
            _intentServer = _hasIntentServer ? intent.GetStringExtra(serverExtra)?.Trim() ?? string.Empty : null;
            _intentPassword = _hasIntentPassword ? intent.GetStringExtra(passwordExtra) ?? string.Empty : null;
            _intentSaveSlot = null;

            if (_intentServer?.Length > MaximumServerCharacters ||
                _intentServer?.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new InvalidDataException("The shared server address is invalid.");
            if (_intentPassword?.Length > MaximumPasswordCharacters)
                throw new InvalidDataException("The shared server password is unexpectedly long.");

            if (saveSlotExtra == null)
                return;

            var saveSlot = intent.GetIntExtra(saveSlotExtra, -1);
            if (saveSlot is < 0 or >= ArchipelagoConnectionSettings.ProfileCount)
                throw new InvalidDataException(
                    $"The shared save position must be between 0 and {ArchipelagoConnectionSettings.ProfileCount - 1}.");
            _intentSaveSlot = saveSlot;
        }

        private static string GetPresentExtra(Intent intent, string currentName, string legacyName)
        {
            if (intent?.HasExtra(currentName) == true)
                return currentName;
            return intent?.HasExtra(legacyName) == true ? legacyName : null;
        }

        private global::Android.Net.Uri GetSharedUri(Intent intent)
        {
            if (intent == null)
                return null;

            if (intent.Data != null)
                return intent.Data;

            var clipUri = intent.ClipData?.ItemCount > 0
                ? intent.ClipData.GetItemAt(0)?.Uri
                : null;
            if (clipUri != null)
                return clipUri;

        #pragma warning disable CS0618
            return intent.GetParcelableExtra(Intent.ExtraStream) as global::Android.Net.Uri;
        #pragma warning restore CS0618
        }

        private void CopyUriToFile(global::Android.Net.Uri uri, string destination)
        {
            using var input = ContentResolver?.OpenInputStream(uri) ??
                              throw new IOException("The selected file could not be opened.");
            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > MaximumSeedBytes)
                    throw new InvalidDataException("The selected randomizer is unexpectedly large.");
                output.Write(buffer, 0, read);
            }
        }

        private void ShowImportDialog()
        {
            var initialSaveSlot = FindInitialSaveSlot();
            var intentTargetSaveSlot = _intentSaveSlot ?? initialSaveSlot;

            var density = Resources?.DisplayMetrics?.Density ?? 1f;
            var padding = (int)(24 * density);
            var spacing = (int)(8 * density);

            var layout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };
            layout.SetPadding(padding, spacing, padding, 0);

            layout.AddView(new TextView(this)
            {
                Text = $"Seed: {_seed.SeedName}\nPlayer: {_seed.SlotName}\nChecks: {_seed.Locations.Count}",
                TextSize = 16
            });

            var server = new EditText(this)
            {
                Hint = "Server address (host:port)",
                InputType = InputTypes.ClassText | InputTypes.TextVariationUri
            };
            server.SetSingleLine(true);
            layout.AddView(server);

            var password = new EditText(this)
            {
                Hint = "Password (optional)",
                InputType = InputTypes.ClassText | InputTypes.TextVariationPassword
            };
            password.SetSingleLine(true);
            password.TransformationMethod = PasswordTransformationMethod.Instance;
            layout.AddView(password);

            var saveSlotLabel = new TextView(this)
            {
                Text = "New or empty in-game save slot:",
                TextSize = 14
            };
            saveSlotLabel.SetPadding(0, spacing, 0, 0);
            layout.AddView(saveSlotLabel);

            var saveSlot = new Spinner(this);
            var saveSlotAdapter = new ArrayAdapter<string>(
                this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                new[] { "Save 1", "Save 2", "Save 3", "Save 4" });
            saveSlotAdapter.SetDropDownViewResource(global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            saveSlot.Adapter = saveSlotAdapter;

            void PopulateConnectionFields(int selectedSaveSlot)
            {
                var selectedSettings = LoadExistingSettings(selectedSaveSlot);
                var useIntentHints = selectedSaveSlot == intentTargetSaveSlot;
                server.Text = useIntentHints && _hasIntentServer
                    ? _intentServer
                    : selectedSettings?.Server ?? string.Empty;
                password.Text = useIntentHints && _hasIntentPassword
                    ? _intentPassword
                    : selectedSettings?.Password ?? string.Empty;
            }

            saveSlot.ItemSelected += (_, args) => PopulateConnectionFields(args.Position);
            saveSlot.SetSelection(initialSaveSlot);
            PopulateConnectionFields(initialSaveSlot);
            layout.AddView(saveSlot);

            var note = new TextView(this)
            {
                Text = (_hasIntentServer || _hasIntentPassword || _intentSaveSlot.HasValue
                    ? "Connection details were supplied by the sharing app. Review them before importing. "
                    : string.Empty) +
                    "Only the selected save profile is replaced, with a backup. Create a new save there for a new seed; other save profiles are unaffected.",
                TextSize = 12
            };
            note.SetPadding(0, spacing, 0, 0);
            layout.AddView(note);

            var dialog = new AlertDialog.Builder(this)
                .SetTitle("Import Archipelago randomizer")
                .SetView(layout)
                .SetNegativeButton("Cancel", (_, _) => Finish())
                .SetPositiveButton("Import and launch", (EventHandler<DialogClickEventArgs>)null)
                .Create();

            dialog.SetOnShowListener(new DialogShownListener(() =>
            {
                dialog.GetButton((int)DialogButtonType.Positive).Click += (_, _) =>
                {
                    var serverValue = server.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(serverValue))
                    {
                        server.Error = "Enter the Archipelago server as host:port.";
                        return;
                    }

                    try
                    {
                        InstallSeed(new ArchipelagoConnectionSettings
                        {
                            Enabled = true,
                            Server = serverValue,
                            Slot = _seed.SlotName,
                            Password = password.Text ?? string.Empty,
                            SeedFile = ArchipelagoConnectionSettings.DefaultSeedFileName,
                            SaveSlot = saveSlot.SelectedItemPosition,
                            AutoConnect = true
                        });
                        dialog.Dismiss();
                        Toast.MakeText(
                            this,
                            $"Imported {_seed.SlotName}. Create a new game in Save {saveSlot.SelectedItemPosition + 1}.",
                            ToastLength.Long)?.Show();
                        LaunchGame();
                    }
                    catch (Exception ex)
                    {
                        ShowError("Could not save randomizer", ex.Message, finishAfterDismiss: false);
                    }
                };
            }));
            dialog.Show();
        }

        private void InstallSeed(ArchipelagoConnectionSettings settings)
        {
            var saveSlot = settings.SaveSlot ??
                           throw new InvalidDataException("Choose an in-game save position.");
            var directory = ArchipelagoConnectionSettings.GetProfileDirectory(_userDataRoot, saveSlot);
            Directory.CreateDirectory(directory);
            var seedPath = ArchipelagoConnectionSettings.GetProfileSeedPath(_userDataRoot, saveSlot);
            var connectionPath = ArchipelagoConnectionSettings.GetProfilePath(_userDataRoot, saveSlot);
            var connectionTemporaryPath = Path.Combine(directory, $"connection.import-{Guid.NewGuid():N}.json");

            File.WriteAllText(connectionTemporaryPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            var seedBackup = seedPath + ".previous";
            var connectionBackup = connectionPath + ".previous";
            var hadSeed = File.Exists(seedPath);
            var hadConnection = File.Exists(connectionPath);

            try
            {
                if (hadSeed)
                    File.Copy(seedPath, seedBackup, true);
                if (hadConnection)
                    File.Copy(connectionPath, connectionBackup, true);

                File.Copy(_temporarySeedPath, seedPath, true);
                File.Move(connectionTemporaryPath, connectionPath, true);
                File.Delete(_temporarySeedPath);
                _temporarySeedPath = null;
            }
            catch
            {
                RestoreBackup(seedPath, seedBackup, hadSeed);
                RestoreBackup(connectionPath, connectionBackup, hadConnection);
                throw;
            }
            finally
            {
                if (File.Exists(connectionTemporaryPath))
                    File.Delete(connectionTemporaryPath);
            }
        }

        private int FindInitialSaveSlot()
        {
            if (_intentSaveSlot.HasValue)
                return _intentSaveSlot.Value;

            for (var saveSlot = 0; saveSlot < ArchipelagoConnectionSettings.ProfileCount; saveSlot++)
            {
                var settings = LoadExistingSettings(saveSlot, includeLegacy: false);
                if (string.Equals(settings?.Slot, _seed.SlotName, StringComparison.Ordinal))
                    return saveSlot;
            }

            var legacy = LoadLegacySettings();
            if (legacy?.SaveSlot is >= 0 and < ArchipelagoConnectionSettings.ProfileCount)
                return legacy.SaveSlot.Value;

            for (var saveSlot = 0; saveSlot < ArchipelagoConnectionSettings.ProfileCount; saveSlot++)
            {
                if (!File.Exists(ArchipelagoConnectionSettings.GetProfilePath(_userDataRoot, saveSlot)))
                    return saveSlot;
            }

            return 0;
        }

        private ArchipelagoConnectionSettings LoadExistingSettings(int saveSlot, bool includeLegacy = true)
        {
            try
            {
                var profile = ArchipelagoConnectionSettings.LoadProfile(_userDataRoot, saveSlot);
                if (profile != null)
                    return profile;
            }
            catch
            {
                // An invalid profile should not prevent replacing it.
            }

            if (!includeLegacy)
                return null;

            var legacy = LoadLegacySettings();
            return legacy?.SaveSlot == null || legacy.SaveSlot == saveSlot ? legacy : null;
        }

        private ArchipelagoConnectionSettings LoadLegacySettings()
        {
            try
            {
                return ArchipelagoConnectionSettings.Load(_userDataRoot);
            }
            catch
            {
                // An invalid legacy connection file should not prevent importing a profile.
                return null;
            }
        }

        private static void RestoreBackup(string destination, string backup, bool existed)
        {
            if (existed && File.Exists(backup))
                File.Copy(backup, destination, true);
            else if (!existed && File.Exists(destination))
                File.Delete(destination);
        }

        private void LaunchGame()
        {
            var launchIntent = new Intent(this, typeof(SplashActivity));
            launchIntent.PutExtra(MainActivity.ExtraLaunchSource, "companion");
            launchIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            StartActivity(launchIntent);
            Finish();
        }

        private void ShowError(string title, string message, bool finishAfterDismiss = true)
        {
            new AlertDialog.Builder(this)
                .SetTitle(title)
                .SetMessage(message)
                .SetPositiveButton("Close", (_, _) =>
                {
                    if (finishAfterDismiss)
                        Finish();
                })
                .Show();
        }

        private void DeleteTemporarySeed()
        {
            if (!string.IsNullOrEmpty(_temporarySeedPath) && File.Exists(_temporarySeedPath))
                File.Delete(_temporarySeedPath);
            _temporarySeedPath = null;
        }

        private sealed class DialogShownListener : Java.Lang.Object, IDialogInterfaceOnShowListener
        {
            private readonly Action _onShow;

            public DialogShownListener(Action onShow)
            {
                _onShow = onShow;
            }

            public void OnShow(IDialogInterface dialog)
            {
                _onShow();
            }
        }
    }
}
