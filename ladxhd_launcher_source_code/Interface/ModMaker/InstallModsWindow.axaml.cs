using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using static LADXHD_Launcher.ModMaker_Functions;

namespace LADXHD_Launcher
{
    public partial class InstallModsWindow : Window, IProgressWindow
    {
        public void UpdateProgressBar(int value)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => progressBar.Value = value);
        }

        public InstallModsWindow()
        {
            InitializeComponent();

            // When the window is closed delete the temp folder.
            this.Closed += (_, _) =>
            {
                if (Directory.Exists(Config.TempPath))
                    Directory.Delete(Config.TempPath, true);
            };

            // If there is no INI file there is no mod to install.
            string IniPath = Path.Combine(Config.TempPath, "LAHDMOD.ini");
            if (!IniPath.TestPath())
                this.Close();

            // Load the INI file.
            LADXHD_IniFile.Initialize(IniPath);
            LADXHD_IniFile.LoadINIValues();

            // Load the values from the INI file.
            Title = Config.ModName;
            ModNameLabel.Text = Config.ModName;
            ModDescBox.Text = Config.Description.Replace("\\n", "\n");

            // Test for an image to display.
            string[] validExt = { ".png", ".jpg", ".bmp" };
            string imagePath = "";

            // If the extensions is in the list then set the image path.
            foreach (string ext in validExt) 
            {
                string testImage = Path.Combine(Config.TempPath, "Image" + ext);

                if (File.Exists(testImage))
                {
                    imagePath = testImage;
                    break;
                }
            }
            // Load in the image if it exists.
            if (imagePath.TestPath())
                ImageBox.Source = new Bitmap(imagePath);
        }

        private void EnableComponents(bool toggle)
        {
            ModDescBox.IsEnabled       = toggle;
            InstallModButton.IsEnabled = toggle;
            ExitButton.IsEnabled       = toggle;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        BUTTONS

-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private async void InstallModButton_Click(object sender, RoutedEventArgs e)
        {
            string message;

            // The game executable was not found.
            string GameExePath = Config.GetGameExecutable(Config.RootPath);
            if (!Config.RootPath.TestPath(true) || !GameExePath.TestPath())
            {
                message = "The Game Path must point to a valid installation that contains the file \"Link's Awakening DX HD.exe\".";
                await OkayWindow.ShowAsync("Game Path Invalid", message, 10);
                return;
            }
            // Disable form, apply patches, enable form.
            EnableComponents(false);
            Config.ActiveWindow = this;
            try
            {
                await Task.Run(() => ModMaker_Functions.ApplyModPatches());
            }
            finally
            {
                Config.ActiveWindow = null;
            }
            EnableComponents(true);

            // Display that it's done.
            message = "Finished applying patches. Any \"Graphics\" mods or \"LAHDMods\" were generated in the \"Mods\" folder.";
            await OkayWindow.ShowAsync("Mod Installed", message, 10, true);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}