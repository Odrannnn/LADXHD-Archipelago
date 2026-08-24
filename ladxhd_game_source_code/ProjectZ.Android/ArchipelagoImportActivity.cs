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
        public const string ActionConfigure = "com.zelda.ladxhd.archipelago.action.CONFIGURE";
        public const string ExtraServer = "com.zelda.ladxhd.archipelago.extra.SERVER";
        public const string ExtraPassword = "com.zelda.ladxhd.archipelago.extra.PASSWORD";
        public const string ExtraSaveSlot = "com.zelda.ladxhd.archipelago.extra.SAVE_SLOT";

        private const string LegacyExtraServer = "com.zelda.ladxhd.extra.SERVER";
        private const string LegacyExtraPassword = "com.zelda.ladxhd.extra.PASSWORD";
        private const string LegacyExtraSaveSlot = "com.zelda.ladxhd.extra.SAVE_SLOT";

        private const long MaximumSeedBytes = 16 * 1024 * 1024;
        private const int MaximumServerCharacters = 512;
        private const int MaximumPasswordCharacters = 1024;
        private const int OpenSeedRequest = 7302;

        private string _temporarySeedPath;
        private ArchipelagoSeedManifest _seed;
        private string _userDataRoot;
        private string _intentServer;
        private string _intentPassword;
        private int? _intentSaveSlot;
        private bool _hasIntentServer;
        private bool _hasIntentPassword;
        private bool _manualSetup;
        private bool _editingInstalledProfile;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window?.SetSoftInputMode(SoftInput.AdjustResize);
            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            DeleteTemporarySeed();
            HandleIntent(intent);
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
                InitializeUserDataRoot();
                ImportFromUri(uri);
            }
            catch (Exception ex)
            {
                DeleteTemporarySeed();
                ShowError("Could not import randomizer", ex.Message);
            }
        }

        private void HandleIntent(Intent intent)
        {
            _manualSetup = string.Equals(intent?.Action, ActionConfigure, StringComparison.Ordinal);
            if (_manualSetup)
            {
                try
                {
                    InitializeUserDataRoot();
                    ClearConnectionHints();
                    ShowManualSetupDialog();
                }
                catch (Exception ex)
                {
                    ShowError("Could not open Archipelago setup", ex.Message);
                }
                return;
            }

            ImportFromIntent(intent);
        }

        private void InitializeUserDataRoot()
        {
            _userDataRoot ??= Application.Context.GetExternalFilesDir(null)?.AbsolutePath ??
                              throw new IOException("Android did not provide an app data directory.");
            Directory.CreateDirectory(ArchipelagoConnectionSettings.GetDirectory(_userDataRoot));
        }

        private void ClearConnectionHints()
        {
            _intentServer = null;
            _intentPassword = null;
            _intentSaveSlot = null;
            _hasIntentServer = false;
            _hasIntentPassword = false;
            _editingInstalledProfile = false;
        }

        private string CreateTemporarySeedPath()
        {
            DeleteTemporarySeed();
            return Path.Combine(
                ArchipelagoConnectionSettings.GetDirectory(_userDataRoot),
                $"seed.import-{Guid.NewGuid():N}.apladxhd");
        }

        private void ImportFromUri(global::Android.Net.Uri uri)
        {
            _temporarySeedPath = CreateTemporarySeedPath();
            CopyUriToFile(uri, _temporarySeedPath);
            _seed = ArchipelagoSeedManifest.Load(_temporarySeedPath);
            ShowImportDialog();
        }

        private void ShowManualSetupDialog()
        {
            var density = Resources?.DisplayMetrics?.Density ?? 1f;
            var padding = (int)(24 * density);
            var spacing = (int)(8 * density);
            var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            layout.SetPadding(padding, spacing, padding, 0);

            layout.AddView(new TextView(this)
            {
                Text = "Choose a generated .apladxhd file for a new randomizer, or open an installed save profile to change its server, port, or password. The player slot is verified from the seed file.",
                TextSize = 14
            });

            var importButton = new Button(this) { Text = "Choose .apladxhd seed file" };
            layout.AddView(importButton);

            var installed = new LinearLayout(this) { Orientation = Orientation.Vertical };
            installed.SetPadding(0, spacing, 0, 0);
            AlertDialog dialog = null;
            var installedProfiles = ArchipelagoProfileCatalog.LoadInstalled(_userDataRoot);
            foreach (var profile in installedProfiles)
            {
                var selectedSlot = profile.SaveSlot;
                var button = new Button(this)
                {
                    Text = $"Save {profile.SaveSlot + 1}: {profile.SlotName} — {profile.SeedName}"
                };
                button.Click += (_, _) =>
                {
                    dialog?.Dismiss();
                    ConfigureExistingProfile(selectedSlot);
                };
                installed.AddView(button);
            }

            if (installedProfiles.Count > 0)
            {
                installed.AddView(new TextView(this)
                {
                    Text = "Installed profiles:",
                    TextSize = 13
                }, 0);
                layout.AddView(installed);
            }

            dialog = new AlertDialog.Builder(this)
                .SetTitle("Archipelago setup")
                .SetView(layout)
                .SetNegativeButton("Back", (_, _) => Finish())
                .Create();
            importButton.Click += (_, _) =>
            {
                dialog.Dismiss();
                OpenSeedPicker();
            };
            dialog.Show();
        }

        private void OpenSeedPicker()
        {
            var picker = new Intent(Intent.ActionOpenDocument);
            picker.AddCategory(Intent.CategoryOpenable);
            picker.SetType("*/*");
            picker.PutExtra(Intent.ExtraMimeTypes, new[]
            {
                "application/x-apladxhd",
                "application/json",
                "application/octet-stream",
                "text/plain"
            });
            picker.AddFlags(ActivityFlags.GrantReadUriPermission);
        #pragma warning disable CS0618
            StartActivityForResult(picker, OpenSeedRequest);
        #pragma warning restore CS0618
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode != OpenSeedRequest)
                return;

            if (resultCode != Result.Ok || data?.Data == null)
            {
                ShowManualSetupDialog();
                return;
            }

            try
            {
                ClearConnectionHints();
                ImportFromUri(data.Data);
            }
            catch (Exception ex)
            {
                DeleteTemporarySeed();
                ShowError("Could not import randomizer", ex.Message);
            }
        }

        private void ConfigureExistingProfile(int saveSlot)
        {
            try
            {
                ClearConnectionHints();
                _intentSaveSlot = saveSlot;
                _editingInstalledProfile = true;
                var settings = LoadExistingSettings(saveSlot, includeLegacy: false) ??
                               throw new InvalidDataException($"Save {saveSlot + 1} has no Archipelago profile.");
                var installedSeedPath = settings.ResolveProfileSeedPath(_userDataRoot, saveSlot);
                _temporarySeedPath = CreateTemporarySeedPath();
                File.Copy(installedSeedPath, _temporarySeedPath, true);
                _seed = ArchipelagoSeedManifest.Load(_temporarySeedPath);
                ShowImportDialog();
            }
            catch (Exception ex)
            {
                DeleteTemporarySeed();
                ShowError("Could not open installed profile", ex.Message);
            }
        }

        private void ReadConnectionHints(Intent intent)
        {
            _editingInstalledProfile = false;
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

            var magpieTracker = new CheckBox(this)
            {
                Text = $"Enable Magpie autotracker (port {MagpieTrackerProtocol.DefaultPort})"
            };
            layout.AddView(magpieTracker);

            var magpieAllowLan = new CheckBox(this)
            {
                Text = "Allow Magpie connections from the local network"
            };
            magpieTracker.CheckedChange += (_, _) =>
            {
                magpieAllowLan.Enabled = magpieTracker.Checked;
                if (!magpieTracker.Checked)
                    magpieAllowLan.Checked = false;
            };
            layout.AddView(magpieAllowLan);

            var saveSlotLabel = new TextView(this)
            {
                Text = _editingInstalledProfile
                    ? "Installed in-game save slot:"
                    : "New or empty in-game save slot:",
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
            saveSlot.Enabled = !_editingInstalledProfile;

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
                magpieTracker.Checked = selectedSettings?.MagpieTrackerEnabled == true;
                magpieAllowLan.Enabled = magpieTracker.Checked;
                magpieAllowLan.Checked = magpieTracker.Checked &&
                                          selectedSettings?.MagpieTrackerAllowLan == true;
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
                    "Only the selected save profile is replaced, with a backup. Create a new save there for a new seed; other save profiles are unaffected. " +
                    "Allow Magpie LAN access only on a trusted local network.",
                TextSize = 12
            };
            note.SetPadding(0, spacing, 0, 0);
            layout.AddView(note);

            var dialog = new AlertDialog.Builder(this)
                .SetTitle(_editingInstalledProfile
                    ? "Update Archipelago connection"
                    : "Import Archipelago randomizer")
                .SetView(layout)
                .SetNegativeButton("Cancel", (_, _) => Finish())
                .SetPositiveButton(_editingInstalledProfile ? "Save and launch" : "Import and launch",
                    (EventHandler<DialogClickEventArgs>)null)
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
                    if (serverValue.Length > MaximumServerCharacters ||
                        serverValue.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                    {
                        server.Error = "Enter a valid server address and port.";
                        return;
                    }
                    if ((password.Text?.Length ?? 0) > MaximumPasswordCharacters)
                    {
                        password.Error = "The password is unexpectedly long.";
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
                            AutoConnect = true,
                            MagpieTrackerEnabled = magpieTracker.Checked,
                            MagpieTrackerAllowLan = magpieTracker.Checked && magpieAllowLan.Checked
                        });
                        dialog.Dismiss();
                        Toast.MakeText(
                            this,
                            _editingInstalledProfile
                                ? $"Updated {_seed.SlotName} in Save {saveSlot.SelectedItemPosition + 1}."
                                : $"Imported {_seed.SlotName}. Create a new game in Save {saveSlot.SelectedItemPosition + 1}.",
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
            launchIntent.PutExtra(MainActivity.ExtraLaunchSource, _manualSetup ? "manual_setup" : "companion");
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

    internal sealed class AndroidArchipelagoSetupService : IArchipelagoSetupService
    {
        private readonly WeakReference<Activity> _activity;

        public AndroidArchipelagoSetupService(Activity activity)
        {
            _activity = new WeakReference<Activity>(activity);
        }

        public bool IsAvailable => true;

        public void Show()
        {
            if (!_activity.TryGetTarget(out var activity))
                return;

            activity.RunOnUiThread(() =>
            {
                var intent = new Intent(activity, typeof(ArchipelagoImportActivity));
                intent.SetAction(ArchipelagoImportActivity.ActionConfigure);
                activity.StartActivity(intent);
            });
        }
    }
}
