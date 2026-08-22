using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using static LADXHD_Launcher.ModMaker_Functions;

namespace LADXHD_Launcher
{
    public interface IProgressWindow
    {
        void UpdateProgressBar(int value);
    }

    internal class Config
    {
        public static string CurrentVersion = "v" + Assembly.GetExecutingAssembly().
            GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+')[0];

        public static int AchievementCount = 111;
        public static string GitlabVersion = "";

        public static string AppPath = "";
        public static string AppName = "";
        public static string RootPath = "";
        public static string ZeldaEXE = "";

        public static string ModName = "";
        public static string Description = "";

        public static string DataPath = "";
        public static string BackupPath = "";

        public static string ImagePath = "";
        public static string TempPath = "";
        public static string PatchesPath = "";
        public static string PatchedPath = "";
        public static string OutputPath = "";

        public static string AnimationMods = "";
        public static string DungeonMods = "";
        public static string GraphicsMods = "";
        public static string MusicMods = "";
        public static string LanguageMods = "";
        public static string MapsMods = "";
        public static string SoundsMods = "";
        public static string LAHDModPath = "";
        public static string ZScripts = "";
        public static string MapOverlay = "";

        public static string OutAnimationMods = "";
        public static string OutDungeonMods = "";
        public static string OutGraphicsMods = "";
        public static string OutMusicMods = "";
        public static string OutLanguageMods = "";
        public static string OutMapsMods = "";
        public static string OutSoundsMods = "";
        public static string OutLAHDModPath = "";
        public static string OutZScripts = "";
        public static string OutMapOverlay = "";

        public static Action? UpdateAvailable { get; set; }

        public static IProgressWindow? ActiveWindow { get; set; }

        private static bool IsRunningInAppImage => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPIMAGE"));

        private static string ModsRoot => IsRunningInAppImage
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zelda_LA", "Mods")
            : Path.Combine(RootPath, "Mods");

        public static string LauncherConfig => Path.Combine(
            File.Exists(Path.Combine(RootPath, "portable.txt"))
                ? RootPath
                : Path.Combine(Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData), "Zelda_LA"), "launcher.cfg");

        public static string UpdateMarker =>
            Path.Combine(Path.GetDirectoryName(LauncherConfig)!, "update.pending");

        public static void Initialize()
        {
            RootPath   = AppContext.BaseDirectory;
            DataPath   = Path.Combine(RootPath, "Data");
            BackupPath = Path.Combine(DataPath, "Backup");

            #if WINDOWS
                AppPath  = Path.Combine(RootPath, "Launcher.exe");
                ZeldaEXE = Path.Combine(RootPath, "Link's Awakening DX HD.exe");
            #elif LINUX
                AppPath  = Path.Combine(RootPath, "Launcher");
                ZeldaEXE = Path.Combine(RootPath, "Link's Awakening DX HD");
            #elif MACOS
                AppPath  = Path.Combine(RootPath, "Launcher");
                ZeldaEXE = Path.Combine(RootPath, "Link's Awakening DX HD");
            #endif

            CleanupOldLauncher();
            SetModCreatePaths();
            CreateDefaultFiles();
        }

        private static void CleanupOldLauncher()
        {
        #if WINDOWS
            string contentFolder = Path.Combine(RootPath, "Content");
            string oldPath = Path.Combine(contentFolder, Path.GetFileNameWithoutExtension(AppPath) + ".old");
        #else
            string oldPath = AppPath + ".old";
        #endif
            if (File.Exists(oldPath))
                File.Delete(oldPath);

            // Adding controller support decided to generate unwanted junk when publishing which just
            // causes more problems and questions and issue reports. Now I need code to get rid of it.
            var fuckingGarbage = Path.Combine(RootPath, "Launcher.dll.config");
            fuckingGarbage.RemovePath();
        }

        public static async Task CheckForUpdateAsync()
        {
            try
            {
                // Get the latest version from the Github repo.
                string? ver = await Gitlab.GetLatestTagAsync();
                if (string.IsNullOrEmpty(ver)) return;

                // Set the latest version.
                GitlabVersion = ver;

                // Parse them as integers to compare them.
                var latest  = Version.Parse(GitlabVersion.TrimStart('v'));
                var current = Version.Parse(CurrentVersion.TrimStart('v'));

                // If the latest version is newer than the current version show the button.
                if (latest > current)
                    UpdateAvailable?.Invoke();
            }
            catch { }
        }

        public static async Task InstallAchievementImages(bool skipPrompts)
        {
            var message = "Achievement images are missing! This is a common issue if updating via the Launcher. Press \"Yes\" to install them.";
            if (skipPrompts || await YesNoWindow.ShowAsync("Install Achievment Images", message))
            {
                // Extract the achievement images to the "Data\Achievements" folder.
                string achievementPath = Path.Combine(Config.DataPath, "Achievements").CreatePath();

                // Check to see if achievement images already exist in the path.
                if (!achievementPath.IsPathEmpty())
                    achievementPath.ClearPath();

                // Create the zip file and extract the achievement images.
                string zipAchievementsPath = Path.Combine(achievementPath, "achievements.zip");
                File.WriteAllBytes(zipAchievementsPath, Resources.GetBytes("achievements.zip"));
                ZipFile.ExtractToDirectory(zipAchievementsPath, achievementPath);
                zipAchievementsPath.RemovePath();

                // Show a confirmation window after installing them.
                if (!skipPrompts)
                {
                    message = "Achievement images have been installed.";
                    await OkayWindow.ShowAsync("Achievement Images Installed", message, 10, true);
                }
            }
        }

        public static string GetGameExecutable(string path)
        {
            var exe = Path.Combine(path, "Link's Awakening DX HD.exe");
            if (File.Exists(exe))
                return exe;
            exe = Path.Combine(path, "Link's Awakening DX HD");
            if (File.Exists(exe))
                return exe;
            return "";
        }

        private static Dictionary<string, string> LoadRawValues(string path)
        {
            // Preserve the user's stored values in a dictionary.
            var values = new Dictionary<string, string>();
            string currentSection = null;

            // Read through each line of the "advanced" file.
            foreach (string raw in File.ReadAllLines(path))
            {
                // Trim whitespace and get the current section.
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                    currentSection = line[1..^1];

                // Look for a line with a key-value pair that is not a comment and is part of a section.
                if (line.Contains('=') && !line.StartsWith("//") && currentSection != null)
                {
                    // Crop out the key and value.
                    int eq     = line.IndexOf('=');
                    string key = line[..eq].Trim();
                    string val = line[(eq + 1)..].Trim();

                    // Store the current key-value pair.
                    if (!string.IsNullOrEmpty(key))
                        values[$"{currentSection}|{key}"] = val;
                }
            }
            // Return the dictionary.
            return values;
        }

        private static void MergeValuesIntoFile(string path, Dictionary<string, string> oldValues)
        {
            // Read the new file and create a list to merge old and new.
            var lines  = File.ReadAllLines(path);
            var output = new List<string>();
            string currentSection = null;

            // Loop through each line in the new file.
            foreach (string raw in lines)
            {
                // Trim white space and find current section.
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                    currentSection = line[1..^1];

                // Look for a line with a key-value pair that is not a comment and is part of a section.
                if (line.Contains('=') && !line.StartsWith("//") && currentSection != null)
                {
                    // Crop out the key and value.
                    int eq      = line.IndexOf('=');
                    string key  = line[..eq].Trim();
                    string newVal = line[(eq + 1)..].Trim();
                    string vk   = $"{currentSection}|{key}";

                    // Get the old value from the key in the current section.
                    if (oldValues.TryGetValue(vk, out string oldVal))
                    {
                        // Variable names may remain the same but value types might change (ex: int to float).
                        bool oldIsBool  = oldVal.Equals("true", StringComparison.OrdinalIgnoreCase) || oldVal.Equals("false", StringComparison.OrdinalIgnoreCase);
                        bool newIsBool  = newVal.Equals("true", StringComparison.OrdinalIgnoreCase) || newVal.Equals("false", StringComparison.OrdinalIgnoreCase);
                        bool oldIsFloat = !oldIsBool && oldVal.Contains('.');
                        bool newIsFloat = !newIsBool && newVal.Contains('.');

                        // Only restore the old value if types match.
                        if (oldIsBool == newIsBool && oldIsFloat == newIsFloat)
                        {
                            int rawEq  = raw.IndexOf('=');
                            string lhs = raw[..rawEq];
                            output.Add($"{lhs}= {oldVal}");
                            continue;
                        }
                    }
                }
                // Add the line the output.
                output.Add(raw);
            }
            // Write the merged file to the output.
            File.WriteAllLines(path, output);
        }

        private static void CreateDefaultFiles()
        {
            // Get the target path. User may store saves locally with portable.txt file.
            string portable  = Path.Combine(RootPath, "portable.txt");
            string targetDir = File.Exists(portable)
                ? RootPath
                : Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "Zelda_LA");

            // Make sure the directory exists.
            Directory.CreateDirectory(targetDir);

            // Set the paths to the two save files.
            string settingsPath = Path.Combine(targetDir, "settings");
            string advancedPath = Path.Combine(targetDir, "advanced");

            // Create "settings" file if it is missing.
            if (!File.Exists(settingsPath))
                File.WriteAllBytes(settingsPath, Resources.GetBytes("settings"));

            // It doesn't exist so simply create a new one.
            if (!File.Exists(advancedPath))
                File.WriteAllBytes(advancedPath, Resources.GetBytes("advanced"));

            // It does exist so it's about to get complicated...
            else
            {
                // Load the user's stored values and merge them into the new file.
                var oldValues = LoadRawValues(advancedPath);
                File.WriteAllBytes(advancedPath, Resources.GetBytes("advanced"));
                MergeValuesIntoFile(advancedPath, oldValues);
            }
        }

        public static void SaveLauncherConfig()
        {
            // Saves the launcher's values to the config file.
            File.WriteAllLines(LauncherConfig, new[]
            {
                $"SoundEnabled={SoundPlayer.Enabled}",
                $"WindowHeight={App.MainWindowInstance?.Height ?? 768}"
            });
            // On Linux/Mac, ensure the file is not marked as executable
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(LauncherConfig,
                    UnixFileMode.UserRead  | 
                    UnixFileMode.UserWrite |
                    UnixFileMode.GroupRead |
                    UnixFileMode.OtherRead);
            }
        }

        public static void LoadLauncherConfig()
        {
            // Migrate old "launcher" file to "launcher.cfg" if it exists.
            string oldConfig = LauncherConfig.Replace(".cfg", "");
            if (File.Exists(oldConfig))
            {
                // Copy the old path temporarily so we can read it.
                string oldPath = oldConfig;

                // Read old values first.
                foreach (string line in File.ReadAllLines(oldPath))
                {
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key   = line[..eq].Trim();
                    string value = line[(eq + 1)..].Trim();

                    switch (key)
                    {
                        case "SoundEnabled":
                            SoundPlayer.Enabled = value.Equals("True", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "WindowHeight":
                            if (double.TryParse(value, out double h))
                                App.SavedWindowHeight = h;
                            break;
                    }
                }
                // Delete old file and save as new format.
                File.Delete(oldPath);
                SaveLauncherConfig();
                return;
            }

            // Normal load from new .cfg file.
            if (!File.Exists(LauncherConfig)) return;

            foreach (string line in File.ReadAllLines(LauncherConfig))
            {
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key   = line[..eq].Trim();
                string value = line[(eq + 1)..].Trim();

                switch (key)
                {
                    case "SoundEnabled":
                        SoundPlayer.Enabled = value.Equals("True", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "WindowHeight":
                        if (double.TryParse(value, out double h))
                            App.SavedWindowHeight = h;
                        break;
                }
            }
        }

        public static void SetModCreatePaths()
        {
            DataPath      = Path.Combine(RootPath, "Data");
            BackupPath    = Path.Combine(DataPath, "Backup");
            AnimationMods = Path.Combine(ModsRoot, "Animations");
            DungeonMods   = Path.Combine(ModsRoot, "Dungeon");
            GraphicsMods  = Path.Combine(ModsRoot, "Graphics");
            MusicMods     = Path.Combine(ModsRoot, "Music");
            LanguageMods  = Path.Combine(ModsRoot, "Languages");
            MapsMods      = Path.Combine(ModsRoot, "Maps");
            SoundsMods    = Path.Combine(ModsRoot, "SoundEffects");
            LAHDModPath   = Path.Combine(ModsRoot, "LAHDMods");
            ZScripts      = Path.Combine(ModsRoot, "scripts.zScript");
            MapOverlay    = Path.Combine(ModsRoot, "MapOverlay.data");
        }

        public static void UpdateOutputPaths(string output)
        {
            // The paths when creating patches.
            OutputPath       = Path.Combine(output, "_ModOutput");
            OutAnimationMods = Path.Combine(OutputPath, "Mods", "Animations");
            OutDungeonMods   = Path.Combine(OutputPath, "Mods", "Dungeon");
            OutGraphicsMods  = Path.Combine(OutputPath, "Mods", "Graphics");
            OutMusicMods     = Path.Combine(OutputPath, "Mods", "Music");
            OutLanguageMods  = Path.Combine(OutputPath, "Mods", "Languages");
            OutMapsMods      = Path.Combine(OutputPath, "Mods", "Maps");
            OutSoundsMods    = Path.Combine(OutputPath, "Mods", "SoundEffects");
            OutLAHDModPath   = Path.Combine(OutputPath, "Mods", "LAHDMods");
            OutZScripts      = Path.Combine(OutputPath, "Mods", "scripts.zScript");
            OutMapOverlay    = Path.Combine(OutputPath, "Mods", "MapOverlay.data");
        }

        public static void UpdateOutputPaths_ApplyPatches()
        {
            // The paths when applying patches.
            OutputPath       = Path.Combine(RootPath, "Mods", "Graphics");
            OutAnimationMods = Path.Combine(TempPath, "Mods", "Animations");
            OutDungeonMods   = Path.Combine(TempPath, "Mods", "Dungeon");
            OutGraphicsMods  = Path.Combine(TempPath, "Mods", "Graphics");
            OutMusicMods     = Path.Combine(TempPath, "Mods", "Music");
            OutLanguageMods  = Path.Combine(TempPath, "Mods", "Languages");
            OutMapsMods      = Path.Combine(TempPath, "Mods", "Maps");
            OutSoundsMods    = Path.Combine(TempPath, "Mods", "SoundEffects");
            OutLAHDModPath   = Path.Combine(TempPath, "Mods", "LAHDMods");
            OutZScripts      = Path.Combine(TempPath, "Mods", "scripts.zScript");
            OutMapOverlay    = Path.Combine(TempPath, "Mods", "MapOverlay.data");
        }
    }
}
