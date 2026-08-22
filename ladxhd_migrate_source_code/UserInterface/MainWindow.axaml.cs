using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using static LADXHD_Migrater.Config;

namespace LADXHD_Migrater
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ComboBox_Platform.SelectedIndex = Config.GetNativePlatform() switch
            {
                Config.Platform.Windows     => 0,
                Config.Platform.Android     => 1,
                Config.Platform.Linux_x64   => 2,
                Config.Platform.Linux_Arm64 => 3,
                Config.Platform.MacOS_x64   => 4,
                Config.Platform.MacOS_Arm64 => 5,
                _ => 0
            };
        }

        public void EnableComponents(bool toggle)
        {
            ComboBox_Platform.IsEnabled = toggle;
            ComboBox_API.IsEnabled      = toggle;
            Button_Migrate.IsEnabled    = toggle;
            Button_Patches.IsEnabled    = toggle;
            Button_Exit.IsEnabled       = toggle;
            Button_Clean.IsEnabled      = toggle;
            Button_Build.IsEnabled      = toggle;
        }

        private void ComboBox_Platform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The combobox will be null on initialization.
            if (ComboBox_API == null) return;

            // Get the combobox that was triggered.
            ComboBox comboBox = (ComboBox)sender;

            if (comboBox.SelectedIndex == 0)
            {
                Config.SelectedPlatform = Platform.Windows;

                ComboBox_API.Items.Clear();
                ComboBox_API.Items.Add("DirectX11");
                ComboBox_API.Items.Add("DirectX12");
                ComboBox_API.Items.Add("OpenGL");
                ComboBox_API.Items.Add("Vulkan");
            }
            else if (comboBox.SelectedIndex == 1)
            {
                Config.SelectedPlatform = Platform.Android;

                ComboBox_API.Items.Clear();
                ComboBox_API.Items.Add("OpenGL");
            }
            else if (comboBox.SelectedIndex == 2)
            {
                Config.SelectedPlatform = Platform.Linux_x64;

                ComboBox_API.Items.Clear();
                ComboBox_API.Items.Add("OpenGL");
                ComboBox_API.Items.Add("Vulkan");
            }
            else if (comboBox.SelectedIndex == 3)
            {
                Config.SelectedPlatform = Platform.Linux_Arm64;

                ComboBox_API.Items.Clear();
                ComboBox_API.Items.Add("OpenGL");
                ComboBox_API.Items.Add("Vulkan");
            }
            else if (comboBox.SelectedIndex == 4)
            {
                Config.SelectedPlatform = Platform.MacOS_x64;

                ComboBox_API.Items.Clear();
                ComboBox_API.Items.Add("OpenGL");
                ComboBox_API.Items.Add("Vulkan");
            }
            else if (comboBox.SelectedIndex == 5)
            {
                Config.SelectedPlatform = Platform.MacOS_Arm64;

                ComboBox_API.Items.Clear();
                ComboBox_API.Items.Add("OpenGL");
                ComboBox_API.Items.Add("Vulkan");
            }
            ComboBox_API.SelectedIndex = 0;
        }

        private void ComboBox_API_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the combobox that was triggered.
            ComboBox comboBox = (ComboBox)sender;

            // Nothing selected yet.
            if (comboBox.SelectedItem is not string api)
                return;

            // Set the Graphics API that was selected.
            Config.SelectedGraphics = api switch
            {
                "DirectX11" => GraphicsAPI.DirectX11,
                "DirectX12" => GraphicsAPI.DirectX12,
                "OpenGL"    => GraphicsAPI.OpenGL,
                "Vulkan"    => GraphicsAPI.Vulkan,
                _           => Config.SelectedGraphics
            };
        }

        public async static Task<bool> VerifyMigrate()
        {
            string message;
            if (!Config.Orig_Content.TestPath() || !Config.Orig_Data.TestPath())
            {
                message = "Either the original \"Content\" folder, \"Data\" folder, or both are missing from the \"assets_original\" folder.";
                await OkayWindow.ShowAsync("Error: Assets Missing", message, 10);
                return false;
            }
            message = "Are you sure you wish to migrate assets? This will apply current patches and overwrite your assets!";
            return await YesNoWindow.ShowAsync("Confirm Migration", message);
        }

        private async static Task<bool> VerifyCreatePatch()
        {
            string message;
            // If the original "Content" or "Data" folder is missing.
            if (!Config.Orig_Content.TestPath() || !Config.Orig_Data.TestPath())
            {
                message = "Either the original \"Content\" folder, \"Data\" folder, or both are missing from the \"assets_original\" folder.";
                await OkayWindow.ShowAsync("Error: Assets Missing", message, 10);
                return false;
            }
            // If the updated "Content" or "Data" folder is missing.
            if (!Config.Update_Content.TestPath() || !Config.Update_Data.TestPath())
            {
                message = "Either the \"Content\" folder, \"Data\" folder, or both are missing from \"ladxhd_game_source_code\".";
                await OkayWindow.ShowAsync("Error: Assets Missing", message, 10);
                return false;
            }
            // Ask the user if they want to create patches.
            message = "Are you sure you wish to create patches? This will overwrite all current patches with recent changes!";
            return await YesNoWindow.ShowAsync("Confirm Create Patches", message);
        }

        private async static Task<bool> VerifyCleanFiles()
        {
            string message = "Are you sure you wish to clean build files? This will remove all instances of \'obj\', \'bin\', \'Publish\', and \'zelda_ladxhd_build\' folders if they currently exist.";
            return await YesNoWindow.ShowAsync("Clean Build Files", message);
        }

        private async void Button_Migrate_Click(object sender, RoutedEventArgs e)
        {
            // Check to see if files should be migrated.
            if (!await VerifyMigrate()) return;

            // Disable the dialog's controls.
            EnableComponents(false);

            // Migrate to the latest versions of files.
            Functions.MigrateFiles();

            // Let the user know that files were migrated.
            string message = "Updated Content/Data files to latest versions.";
            await OkayWindow.ShowAsync("Finished Migration", message, 10, true);

            // Enable the dialog's controls.
            EnableComponents(true);
        }

        private async void Button_Patches_Click(object sender, RoutedEventArgs e)
        {
            // Check to see if patches should be created.
            if (!await VerifyCreatePatch()) return;

            // Disable the dialog's controls.
            EnableComponents(false);

            // Create the patches.
            Functions.CreatePatches();

            // Let the user know that the patches were created.
            string message = "Finished creating vcdiff patches from modified files. If any files were intentionally modifed, these can be shared as a new PR for the GitHub repository.";
            await OkayWindow.ShowAsync("Patches Created", message, 10, true);

            // Enable the dialog's controls.
            EnableComponents(true);
        }

        private void Button_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Button_Clean_Click(object sender, RoutedEventArgs e)
        {
            // Verify cleaning out junk files.
            if (!await VerifyCleanFiles()) return;

            // Disable the dialog's controls.
            EnableComponents(false);

            // Clean out the junk files.
            await Task.Run(() => Functions.CleanBuildFiles());

            // Let the user know that it finished.
            string message = "Finished cleaning build files (obj/bin/Publish folders).";
            await OkayWindow.ShowAsync("Finished Build File Purge", message, 10, true);

            // Enable the dialog's controls.
            EnableComponents(true);
        }

        private async void Button_Build_Click(object sender, RoutedEventArgs e)
        {
            // Disable the dialog's controls.
            EnableComponents(false);
            
            // Try to create a new build.
            await Task.Run(() => Functions.CreateBuild());

            // Let the user know it's finished.
            string message = "Finished build process. If the build was successful, it can be found in the \"_Publish\" folder.";
            await OkayWindow.ShowAsync("Finished Build", message, 10, true);

            // Enable the dialog's controls.
            EnableComponents(true);
        }
    }
}