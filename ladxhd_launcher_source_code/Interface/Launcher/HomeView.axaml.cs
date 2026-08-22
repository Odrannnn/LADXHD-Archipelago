using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace LADXHD_Launcher
{
    public partial class HomeView : UserControl, IControllerPage
    {
        private MainWindow? _parent;

        // Resets the advanced file if the version requires it. 
        bool _resetAdvancedFile = false;

        public HomeView()
        {
            InitializeComponent();
        }

        public HomeView(MainWindow parent)
        {
            InitializeComponent();
            SoundToggle_SetImage();
            _parent = parent;

            Config.UpdateAvailable = () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    UpdateButton.IsVisible = true;
                });
            };

            // Check to see if an update is pending after a launcher restart.
            if (File.Exists(Config.UpdateMarker))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    // Get the version we are updating to from the marker file.
                    string targetVersion;
                    try
                    {
                        targetVersion = File.ReadAllText(Config.UpdateMarker).Trim();
                        File.Delete(Config.UpdateMarker);
                    }
                    catch { return; }

                    // If the current version is still equal to the marker, something went wrong somewhere.
                    if (!Config.CurrentVersion.Equals(targetVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        await OkayWindow.ShowAsync("Update Warning", "The launcher was not updated successfully. Please update manually.");
                        return;
                    }
                    Config.GitlabVersion = targetVersion;

                    // Finish updating the game.
                    await UpdateGame();
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
        }
        // Forcus the play button on init.
        public void FocusInitial() => PlayButton.Focus(Avalonia.Input.NavigationMethod.Directional);

        // Already at home window. Move to "Exit".
        public void OnCancel() => ExitButton.Focus(Avalonia.Input.NavigationMethod.Directional);

        // No back button on the home page; nothing to jump to.
        public void FocusBack() { }

        private string GetGameDirectory()
        {
            return AppContext.BaseDirectory;
        }

        private void SoundToggle_SetImage()
        {
            SoundButtonImage.Source = SoundPlayer.Enabled
                ? new Avalonia.Media.Imaging.Bitmap(
                    AssetLoader.Open(new Uri("avares://Launcher/Resources/sound_on.png")))
                : new Avalonia.Media.Imaging.Bitmap(
                    AssetLoader.Open(new Uri("avares://Launcher/Resources/sound_off.png")));
        }

        public void ForceUpdate()
        {
            // Forces an update by pressing Shift + F1.
            UpdateButton.IsVisible = true;
            UpdateButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private async Task ResetAdvancedFile()
        {
            // Make sure the user is okay with this change.
            string message = $"This version has many changes that require a reset of the \"Mods\" settings. It is highly suggested to press \"Yes\" to reset settings or enemies will have the wrong HP values!";
            
            // It may be necessary to reset the advanced file on some versions.
            if (await YesNoWindow.ShowAsync("Reset Mods Settings", message))
            {
                // Get the location of the "advanced" file.
                string advancedPath = AdvancedSettings.GetPath(AppContext.BaseDirectory);

                // Remove it completely. A new one will be written.
                advancedPath.RemovePath();
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // Ask the user if they want to update the game.
            bool confirmed = await YesNoWindow.ShowAsync("Update Game", $"Update the game to {Config.GitlabVersion}?");
            if (!confirmed) return;

            // If the launcher itself is outdated, update it first and restart.
            var latest  = Version.Parse(Config.GitlabVersion.TrimStart('v'));
            var current = Version.Parse(Config.CurrentVersion.TrimStart('v'));

            // If the launcher needs updated do that, otherwise just update the game.
            if (latest > current)
                await UpdateLauncherAndRestart();
            else
                await UpdateGame();
        }

        private async Task UpdateLauncherAndRestart()
        {
            // Show the progress window.
            var progress = await ProgressWindow.ShowAsync("Updating Launcher", "Downloading launcher...");
            try
            {
                // Create a temporary path to grab the new launcher.
                Config.TempPath = Path.Combine(Config.RootPath, "_temp").CreatePath();

                // Download the new launcher zip to the temp folder.
                string launcherZip  = Gitlab.GetLauncherZipName();
                string launcherPath = Path.Combine(Config.TempPath, launcherZip);
                var downloadProg    = new Progress<int>(v => progress.UpdateProgressBar(v));
                await Gitlab.DownloadFileAsync(launcherZip, launcherPath, downloadProg);

                // Install the new launcher in place of the old.
                progress.UpdateStatus("Installing launcher...");
                Patcher_Functions.ReplaceLauncher(launcherPath);

                // Launcher is installed so the temp folder is no longer needed.
                if (Directory.Exists(Config.TempPath)) Directory.Delete(Config.TempPath, true);

                // Tells the new launcher to continue the game update on startup.
                File.WriteAllText(Config.UpdateMarker, Config.GitlabVersion);

                // Tell the user they need to restart the launcher.
                string message = $"To finish patching the game, please restart the Launcher after it closes!";
                await OkayWindow.ShowAsync("Launcher Updated", message, 20, true);

                // Close the launcher.
                progress.CloseWindow();
                App.MainWindowInstance.Close();
            }
            catch (Exception ex)
            {
                progress.CloseWindow();
                try { if (Directory.Exists(Config.TempPath)) Directory.Delete(Config.TempPath, true); } catch { }
                await OkayWindow.ShowAsync("Update Failed", "An unknown issue has happened. Please update manually via the LADXHD Patcher!");
            }
        }

        private async Task UpdateGame()
        {
            // Show the progress window.
            var progress = await ProgressWindow.ShowAsync("Updating Game", "Downloading patches...");
            var downloadProg = new Progress<int>(v => progress.UpdateProgressBar(v));

            try
            {
                // Set up the temporary folders.
                Config.TempPath = Path.Combine(Config.RootPath, "_temp").CreatePath();
                Config.PatchesPath = Path.Combine(Config.TempPath, "patches").CreatePath();
                Config.PatchedPath = Path.Combine(Config.TempPath, "patchedFiles").CreatePath();

                // Download the patches to the temp folder.
                progress.UpdateStatus("Downloading patches...");
                progress.UpdateProgressBar(0);
                string patchZip = Gitlab.GetPatchesZipName();
                string zipPath = Path.Combine(Config.TempPath, patchZip);
                await Gitlab.DownloadFileAsync(patchZip, zipPath, downloadProg);

                // Extract the patches to temp patches folder.
                progress.UpdateStatus("Extracting patches...");
                progress.UpdateProgressBar(0);
                ZipFile.ExtractToDirectory(zipPath, Config.PatchesPath);

                // Apply the patches.
                progress.UpdateStatus("Applying patches...");
                progress.UpdateProgressBar(0);
                Config.ActiveWindow = progress;
                await Patcher_Functions.StartPatching();

                // Run finalization on new launcher.
                progress.UpdateStatus("Finalizing...");
                await Patcher_Functions.HostFinalizationFunctions();

                // Sometimes the advanced file needs reset.
                if (_resetAdvancedFile)
                    await ResetAdvancedFile();

                // Finish up and close out the window.
                progress.Finish();
                progress.CloseWindow();

                // Check if the Achievement images are missing and install if missing.
                var achievementPath = Path.Combine(Config.DataPath, "Achievements");
                if (!achievementPath.TestPath() || achievementPath.GetFiles("*", true).Count < Config.AchievementCount)
                    Config.InstallAchievementImages(false);

                // Cleanup the temporary folder.
                if (Directory.Exists(Config.TempPath))
                    Directory.Delete(Config.TempPath, true);

                // Show the confirmation window.
                string message = $"The game has been updated to {Config.GitlabVersion}.";
                await OkayWindow.ShowAsync("Update Complete", message, 20, true);
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Config.RootPath, "update_error.log"), 
                    ex.Message + "\n" + ex.StackTrace);

                // If an error happened close the progress window.
                progress.CloseWindow();

                // Attempt to remove the temp folder.
                try 
                {
                    if (Directory.Exists(Config.TempPath)) Directory.Delete(Config.TempPath, true); 
                } 
                catch { }

                // Show the reason for failure to the user.
                await OkayWindow.ShowAsync("Update Failed", ex.Message);
            }
        }

        private void SoundToggle_Click(object sender, RoutedEventArgs e)
        {
            SoundPlayer.Enabled = !SoundPlayer.Enabled;
            Config.SaveLauncherConfig();
            SoundToggle_SetImage();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Config.ZeldaEXE))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = Config.ZeldaEXE,
                WorkingDirectory = Config.RootPath,
                UseShellExecute = true
            });
            _parent?.Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            int maxGameScale = AdvancedSettings.LoadMaxGameScale(GetGameDirectory());
            GameSettings.Load(GetGameDirectory());
            _parent?.SettingsView.LoadValues(maxGameScale);
            _parent?.NavigateTo(_parent.SettingsView);
            SoundPlayer.PlayXnbSound(SoundPlayer.SoundOpen);
        }

        private async void ModsButton_Click(object sender, RoutedEventArgs e)
        {
            _parent?.HideNotifications();
            _parent?.ShowLoadingMessage();

            await System.Threading.Tasks.Task.Run(() =>
            {
                AdvancedSettings.Load(AppContext.BaseDirectory);
            });

            _parent?.ModsView.LoadValues();
            _parent?.HideLoadingMessage();
            _parent?.NavigateTo(_parent.ModsView);

            // Wait for the UI to actually render before playing sound
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
                () => SoundPlayer.PlayXnbSound(SoundPlayer.SoundOpen),
                Avalonia.Threading.DispatcherPriority.Background);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _parent?.Close();
        }
    }
}
