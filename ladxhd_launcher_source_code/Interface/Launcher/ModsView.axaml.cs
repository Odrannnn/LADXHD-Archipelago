using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using NativeFileDialogSharp;
using static LADXHD_Launcher.AdvancedSettings;

namespace LADXHD_Launcher
{
    public partial class ModsView : UserControl, IControllerPage
    {
        private MainWindow? _parent;

        public ModsView() { InitializeComponent(); }

        public ModsView(MainWindow parent)
        {
            InitializeComponent();
            _parent = parent;
            InstallLAHDPak.AddHandler(DragDrop.DragOverEvent, ModsInstall_DragOver);
            InstallLAHDPak.AddHandler(DragDrop.DropEvent, ModsInstall_Drop);
            CreateLAHDPak.AddHandler(DragDrop.DragOverEvent, ModsCreate_DragOver);
            CreateLAHDPak.AddHandler(DragDrop.DropEvent, ModsCreate_Drop);
        }

        public void FocusInitial() => InstallLAHDPak.Focus(NavigationMethod.Directional);
        public void FocusBack() => BackButton.Focus(NavigationMethod.Directional);

        public void OnCancel()
        {
            _parent?.NavigateTo(_parent.HomeView);
            SoundPlayer.PlayXnbSound(SoundPlayer.SoundClose);
        }

        private bool IsLAHDPakFile(string pakPath)
        {
            // Get the file as an item to test the extension.
            FileItem pakItem = new FileItem(pakPath);

            // Only LAHDPak files should be allowed.
            if (pakItem.Extension == ".lahdpak")
                return true;

            // Whatever it is, it's not a lahdpak.
            return false;
        }

        //-------------------------------------------------------------------------------------------------------
        //
        //  INSTALL MODPACK BUTTON
        //
        //-------------------------------------------------------------------------------------------------------

        private void ModsInstall_DragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains(DataFormats.FileNames) ? DragDropEffects.Copy : DragDropEffects.None;
        }
        private void ModsInstall_Drop(object sender, DragEventArgs e)
        {
            // If there is no data then exit early.
            if (!e.Data.Contains(DataFormats.FileNames))
                return;

            // Get the path that was dropped.
            string pakPath = e.Data.GetFileNames()?.FirstOrDefault();
            FinishShowModInstallWindow(pakPath);
        }

        private void InstallModPack(object sender, RoutedEventArgs e)
        {
            // Open a dialog to select a lahdpak file.
            DialogResult? selected = Dialog.FileOpen("lahdpak");
            if (!selected.IsOk)
                return;

            // Get the file path that was selected.
            string pakPath = selected.Path;
            FinishShowModInstallWindow(pakPath);
        }

        private async void FinishShowModInstallWindow(string pakPath)
        {
            // Make sure it's an LAHDPak file.
            if (!IsLAHDPakFile(pakPath))
                return;

            // Extract the .lahdpak to a temp folder and point Config at it.
            Config.TempPath = Path.Combine(Path.GetTempPath(), "LADXHD_ModInstall");
            if (Directory.Exists(Config.TempPath))
                Directory.Delete(Config.TempPath, true);
            ZipFile.ExtractToDirectory(pakPath, Config.TempPath);

            // Show the mods window to the user.
            var installModWindow = new InstallModsWindow();
            await installModWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        //-------------------------------------------------------------------------------------------------------
        //
        //  CREATE MODPACK BUTTON
        //
        //-------------------------------------------------------------------------------------------------------

        private void ModsCreate_DragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains(DataFormats.FileNames) ? DragDropEffects.Copy : DragDropEffects.None;
        }
        private async void ModsCreate_Drop(object sender, DragEventArgs e)
        {
            // If there is no data then exit early.
            if (!e.Data.Contains(DataFormats.FileNames))
                return;

            // Get the path that was dropped.
            string pakPath = e.Data.GetFileNames()?.FirstOrDefault();
            
            // Make sure it's an LAHDPak file.
            if (!IsLAHDPakFile(pakPath))
                return;

            // Show the mods window to the user and load the dropped pack.
            var createModWindow = new CreateModsWindow(pakPath);
            await createModWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private async void CreateModPack(object sender, RoutedEventArgs e)
        {
            // Show the mods window to the user.
            var createModWindow = new CreateModsWindow();
            await createModWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        //-------------------------------------------------------------------------------------------------------
        //
        //  REMOVE ALL MODS BUTTON
        //
        //-------------------------------------------------------------------------------------------------------

        private async void RemoveAllMods(object sender, RoutedEventArgs e)
        {
            // Show a Yes/No window to the user.
            string message = "Are you sure you wish to delete all mod files?";

            // If the user clicked "Yes" clear all the mods.
            if (await YesNoWindow.ShowAsync("Delete All Mods?", message))
            {
                Config.AnimationMods.ClearPath();
                Config.DungeonMods.ClearPath();
                Config.GraphicsMods.ClearPath();
                Config.MusicMods.ClearPath();
                Config.LanguageMods.ClearPath();
                Config.MapsMods.ClearPath();
                Config.SoundsMods.ClearPath();
                Config.LAHDModPath.ClearPath();
                Config.ZScripts.RemovePath();

                message = "All currently installed mod files have been successfully deleted.";
                await OkayWindow.ShowAsync("Mods Deleted", message, 10, true);
            }
        }

        //-------------------------------------------------------------------------------------------------------

        private static decimal? GetOverride(Dictionary<string, decimal> overrides, string key)
        {
            // Exact match first
            if (overrides.TryGetValue(key, out decimal exact))
                return exact;

            // Partial suffix match
            foreach (var entry in overrides)
            {
                if (entry.Key.StartsWith("*") && key.EndsWith(entry.Key[1..]))
                    return entry.Value;
            }

            return null;
        }

        public void LoadValues()
        {
            // Suppress the sound effects so the checkbox sound doesn't fire a bunch of times.
            SoundPlayer.SuppressSound = true;

            // Remove only auto-generated groups.
            while (ModsPanel.Children.Count > 1)
                ModsPanel.Children.RemoveAt(1);

            foreach (var section in AdvancedSettings.Sections)
            {
                // Count total options
                int optionCount = 0;
                foreach (var g in section.Groups) optionCount += g.Options.Count;

                // Detect "lives-style" section: many options, single group, no sub-tooltips.
                bool twoColumn = false;

                // Section header: create a new combobox.
                var header = new TextBlock
                {
                    Text       = section.Header,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold,
                    Margin     = new Thickness(2, 0, 0, 0)
                };
                // If a comment was set then set the tooltip.
                if (!string.IsNullOrEmpty(section.HeaderTooltip))
                    ToolTip.SetTip(header, section.HeaderTooltip);

                int rowHeight = 36;
                int rows = twoColumn
                    ? (int)Math.Ceiling(optionCount / 2.0)
                    : optionCount;
                int canvasH  = rows * rowHeight;
                var canvas   = new Canvas { Height = canvasH };
                var rgbNuds  = new Dictionary<string, NumericUpDown?[]>();
                var rgbRows  = new Dictionary<string, List<int>>();
                var rgbBoxes = new Dictionary<string, Border>();

                void UpdateRgbBox(string prefix)
                {
                    if (!rgbBoxes.TryGetValue(prefix, out var box)) return;
                    if (!rgbNuds.TryGetValue(prefix, out var arr)) return;
                    box.Background = new SolidColorBrush(
                        Color.FromRgb(ToByte(arr[0]?.Value), ToByte(arr[1]?.Value), ToByte(arr[2]?.Value)));
                }
                int col = 0;
                int row = 0;

                foreach (var group in section.Groups)
                {
                    foreach (var option in group.Options)
                    {
                        double x = twoColumn && col == 1 ? 232 : 0;
                        double y = row * rowHeight;
                        string tooltip = option.Tooltip;

                        // Checkbox: Option is boolean so present with a checkbox.
                        if (option.IsBool)
                        {
                            // Create a new checkbox.
                            var cb = new CheckBox
                            {
                                Content    = OptionLabels.Get(option.Key),
                                Foreground = Brushes.White,
                                IsChecked  = option.BoolValue
                            };
                            // Set a tooltip if comments were found.
                            if (!string.IsNullOrEmpty(tooltip))
                                ToolTip.SetTip(cb, tooltip);

                            string sHeader = section.Header;
                            string key     = option.Key;
                            cb.IsCheckedChanged += (s, e) =>
                                AdvancedSettings.UpdateValue(sHeader, key,
                                    (cb.IsChecked == true).ToString().ToLower());

                            Canvas.SetLeft(cb, x);
                            Canvas.SetTop(cb, y);
                            canvas.Children.Add(cb);
                        }
                        // Numeric Up/Down: Option is a number so present with a numeric up/down.
                        else
                        {
                            // Width and offset of numeric up/downs.
                            const double nudWidth  = 140;
                            const double lblOffset = nudWidth + 6;

                            decimal? minOverride = GetOverride(AdvancedSettings.MinOverrides, option.Key);
                            decimal? maxOverride = GetOverride(AdvancedSettings.MaxOverrides, option.Key);

                            decimal minVal = minOverride ?? (group.AllowNegative ? decimal.MinValue : 0);
                            decimal maxVal = maxOverride ?? decimal.MaxValue;

                            // Apply the values to the numeric up/downs.
                            var nud = new NumericUpDown
                            {
                                Width        = nudWidth,
                                Minimum      = minVal,
                                Maximum      = maxVal,
                                Increment    = option.Increment,
                                Value        = (decimal)(option.IsFloat ? option.FloatValue : option.IntValue),
                                FormatString = option.FormatString
                            };
                            var lbl = new TextBlock
                            {
                                Text       = OptionLabels.Get(option.Key),
                                Foreground = Brushes.White
                            };
                            if (!string.IsNullOrEmpty(tooltip))
                            {
                                ToolTip.SetTip(nud, tooltip);
                                ToolTip.SetTip(lbl, tooltip);
                            }

                            string sHeader = section.Header;
                            string key     = option.Key;

                            // If this is an R/G/B channel, register it under its prefix.
                            bool isRgb = TryParseRgbKey(key, out string rgbPrefix, out int rgbChannel);
                            if (isRgb)
                            {
                                if (!rgbNuds.TryGetValue(rgbPrefix, out var arr))
                                {
                                    arr = new NumericUpDown?[3];
                                    rgbNuds[rgbPrefix] = arr;
                                    rgbRows[rgbPrefix] = new List<int>();
                                }
                                arr[rgbChannel] = nud;
                                rgbRows[rgbPrefix].Add(row);
                            }

                            nud.ValueChanged += (s, e) =>
                            {
                                string val = option.IsFloat
                                    ? ((float)(nud.Value ?? 0)).ToString("F" + option.DecimalPlaces,
                                        System.Globalization.CultureInfo.InvariantCulture)
                                    : ((int)(nud.Value ?? 0)).ToString();
                                AdvancedSettings.UpdateValue(sHeader, key, val);
                                if (isRgb) UpdateRgbBox(rgbPrefix);
                            };
                            Canvas.SetLeft(nud, x);
                            Canvas.SetTop(nud, y);
                            Canvas.SetLeft(lbl, x + lblOffset);
                            Canvas.SetTop(lbl, y + 7);
                            canvas.Children.Add(nud);
                            canvas.Children.Add(lbl);
                        }

                        if (twoColumn)
                        {
                            col++;
                            if (col > 1) { col = 0; row++; }
                        }
                        else
                        {
                            row++;
                        }
                    }
                }

                // Place a color-preview swatch for every complete R/G/B triplet,
                // vertically centered across its three rows.
                const double boxSize = 50;
                const double boxX = 380;
                foreach (var kvp in rgbNuds)
                {
                    var arr = kvp.Value;
                    if (arr[0] == null || arr[1] == null || arr[2] == null)
                        continue; // need all three channels before drawing a box

                    var r = rgbRows[kvp.Key];
                    double centerY = (r.Min() + r.Max()) / 2.0 * rowHeight + rowHeight / 2.0;

                    var box = new Border
                    {
                        Width           = boxSize,
                        Height          = boxSize,
                        BorderBrush     = Brushes.White,
                        BorderThickness = new Thickness(2),
                        CornerRadius    = new CornerRadius(4)
                    };
                    Canvas.SetLeft(box, boxX);
                    Canvas.SetTop(box, centerY - boxSize / 2);
                    canvas.Children.Add(box);
                    rgbBoxes[kvp.Key] = box;
                    UpdateRgbBox(kvp.Key); // paint initial color
                }
                var border = new Border
                {
                    BorderBrush     = new SolidColorBrush(Color.Parse("#88FFFFFF")),
                    CornerRadius    = new CornerRadius(6),
                    Padding         = new Thickness(10),
                    Child           = canvas
                };
                var sectionPanel = new StackPanel { Spacing = 4 };
                sectionPanel.Children.Add(header);
                sectionPanel.Children.Add(border);
                ModsPanel.Children.Add(sectionPanel);
            }
            // Ok it's fine now.
            SoundPlayer.SuppressSound = false;
            UiNavigator.InvalidateCandidates();
        }

        private static bool TryParseRgbKey(string key, out string prefix, out int channel)
        {
            prefix = ""; channel = -1;
            if (key.Length < 3 || key[^2] != '_') return false;
            channel = char.ToLowerInvariant(key[^1]) switch { 'r' => 0, 'g' => 1, 'b' => 2, _ => -1 };
            if (channel < 0) return false;
            prefix = key[..^2];
            return true;
        }

        private static byte ToByte(decimal? v)
        {
            if (v is null) return 0;
            decimal d = v.Value;
            return (byte)(d < 0 ? 0 : d > 255 ? 255 : d);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _parent?.NavigateTo(_parent.HomeView);
            SoundPlayer.PlayXnbSound(SoundPlayer.SoundClose);
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Show a dialog to confirm reset settings.
            var message = "Reset all settings to their default values?";
            if (!await YesNoWindow.ShowAsync("Reset Settings?", message))
                return;

            // Set the "advanced" file path based on presence of "portable.txt" in root folder.
            string targetDir = File.Exists(Path.Combine(Config.RootPath, "portable.txt"))
                ? Config.RootPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zelda_LA");
            string advancedPath = Path.Combine(targetDir, "advanced");

            // Delete the existing file
            if (File.Exists(advancedPath))
                File.Delete(advancedPath);

            // Re-extract from resources
            File.WriteAllBytes(advancedPath, LADXHD_Launcher.Resources.GetBytes("advanced"));

            // Reload on background thread
            await System.Threading.Tasks.Task.Run(() => { AdvancedSettings.Load(AppContext.BaseDirectory); });

            // Back on UI thread — rebuild controls then show notification and play sound
            LoadValues();

            // Show the notification that settings were reset and play a sound.
            await System.Threading.Tasks.Task.Delay(250);
            _parent?.ShowNotification(MainWindow.NotificationType.Reset);
            SoundPlayer.PlayXnbSound(SoundPlayer.SoundReset);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            AdvancedSettings.Save(AppContext.BaseDirectory);
            _parent?.ShowNotification(MainWindow.NotificationType.Save);
            _parent?.NavigateTo(_parent.HomeView);
            SoundPlayer.PlayXnbSound(SoundPlayer.SoundXSave);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            _parent?.Close();
        }
    }
}