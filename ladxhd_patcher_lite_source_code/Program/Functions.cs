using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static LADXHD_Patcher_Lite.Config;
using static LADXHD_Patcher_Lite.VCDiff;

namespace LADXHD_Patcher_Lite
{
    public class Functions
    {
        private static int    _fileCount;
        private static int    _totalCount;
        private static int    _filesPatched;

        public  static bool   HeadlessMode { get => _headlessMode; set => _headlessMode = value; }
        public  static int    ExitCode   { get; private set; }

        private static bool   _headlessMode;
        private static bool   _patchFromBackup;
        private static string _executable;

        // Resets the advanced file if the version requires it. 
        private static bool   _resetAdvancedFile = false;

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        FILE MAPPING CODE : NOT ALL FILES AND PATCHES ARE 1:1 FROM ORIGINAL GAME VERSION. NEW FILES NEED A "BASE" TO BE CREATED FROM USING A PATCH
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        // SOME RESOURCES ARE USED TO CREATE MULTIPLE FILES OR PATCHES. EACH ARRAY BELOW HOLDS ALL VERSIONS OF A FILE THAT IS
        // BASED OFF OF ANOTHER FILE. THE "MASTER" FILE THAT CREATES THESE VERSIONS IS LINKED TO THEM IN THE DICTIONARY BELOW

        private static string[] langFiles  = new[] { "chn.lng", "deu.lng", "esp.lng", "fre.lng", "ind.lng", "ita.lng", "por.lng", "pte.lng", "rus.lng", "swe.lng" };
        private static string[] langAchiev = new[] { "achieve_chn.lng", "achieve_deu.lng", "achieve_eng.lng", "achieve_esp.lng", "achieve_fre.lng", "achieve_ind.lng", "achieve_ita.lng", "achieve_por.lng", "achieve_pte.lng", "achieve_rus.lng", "achieve_swe.lng" };
        private static string[] langDialog = new[] { "dialog_chn.lng", "dialog_deu.lng", "dialog_esp.lng", "dialog_fre.lng", "dialog_ind.lng", "dialog_ita.lng", "dialog_por.lng", "dialog_pte.lng", "dialog_rus.lng", "dialog_swe.lng" };
        private static string[] smallFonts = new[] { "smallFont_redux.xnb", "smallFont_vwf.xnb", "smallFont_vwf_redux.xnb", "smallFont_chn.xnb", "smallFont_chn_0.xnb", "smallFont_chn_redux.xnb", "smallFont_chn_redux_0.xnb" };
        private static string[] backGround = new[] { "menuBackgroundB.xnb", "menuBackgroundC.xnb", "sgb_border.xnb" };
        private static string[] lighting   = new[] { "mamuLight.xnb" };
        private static string[] linkImages = new[] { "link1.png", "weapons.png" };
        private static string[] linkAtlas  = new[] { "weapons.atlas" };
        private static string[] npcImages  = new[] { "npcs_redux.png" };
        private static string[] itemImages = new[] { "items_chn.png", "items_deu.png", "items_esp.png", "items_fre.png", "items_ind.png", "items_ita.png", "items_por.png", "items_rus.png", "items_swe.png", "items_redux.png", 
                                                     "items_redux_chn.png", "items_redux_deu.png", "items_redux_esp.png", "items_redux_fre.png", "items_redux_ind.png", "items_redux_ita.png", "items_redux_por.png", "items_redux_rus.png", "items_redux_swe.png" };
        private static string[] introImage = new[] { "intro_chn.png", "intro_deu.png", "intro_esp.png", "intro_fre.png", "intro_ind.png", "intro_ita.png", "intro_por.png", "intro_rus.png", "intro_swe.png" };
        private static string[] introAtlas = new[] { "intro_chn.atlas" };
        private static string[] miniMapImg = new[] { "minimap_chn.png", "minimap_deu.png", "minimap_esp.png", "minimap_fre.png", "minimap_ind.png", "minimap_ita.png", "minimap_por.png", "minimap_rus.png", "minimap_swe.png" };
        private static string[] objectsImg = new[] { "objects_chn.png", "objects_deu.png", "objects_esp.png", "objects_fre.png", "objects_ind.png", "objects_ita.png", "objects_por.png", "objects_rus.png", "objects_swe.png" };
        private static string[] photograph = new[] { "photos_chn.png", "photos_deu.png", "photos_esp.png", "photos_fre.png",  "photos_ind.png", "photos_ita.png", "photos_por.png", "photos_rus.png", "photos_swe.png", "photos_redux.png", 
                                                     "photos_redux_chn.png", "photos_redux_deu.png", "photos_redux_esp.png", "photos_redux_fre.png", "photos_redux_ind.png", "photos_redux_ita.png", "photos_redux_por.png", "photos_redux_rus.png", "photos_redux_swe.png" };
        private static string[] uiImages   = new[] { "ui_chn.png", "ui_deu.png", "ui_esp.png", "ui_fre.png", "ui_ind.png", "ui_ita.png", "ui_por.png", "ui_rus.png", "ui_swe.png" };
        private static string[] musicTile  = new[] { "musicOverworldClassic.data" };
        private static string[] dungeon3M  = new[] { "dungeon3.map" };
        private static string[] dungeon3D  = new[] { "dungeon3.map.data" };
        private static string[] newBridgeM = new[] { "bridge_l_castle.map", "bridge_r_castle.map" };
        private static string[] newBridgeD = new[] { "bridge_l_castle.map.data", "bridge_r_castle.map.data" };
        private static string[] bowwowanim = new[] { "bowwow_water.ani" };
        private static string[] dungeonani = new[] { "mapDungeon.ani", "mapManboPond.ani", "mapTeleporter.ani" };
        private static string[] boomerang  = new[] { "boomerangOrig.ani" };
        private static string[] effectani  = new[] { "bushExplosion.ani", "fireballDeath.ani" };
        private static string[] shaderFile = new[] { "GBCColorCorrection.xnb", "PixelGrid.xnb" };

        // THE "KEY" IS THE MASTER FILE THAT CREATES OTHER FILES FROM IT. THE "VALUE" IS THE STRING ARRAY THAT HOLDS THOSE FILES

        private static readonly Dictionary<string, string[]> fileTargets = new Dictionary<string, string[]>
        {
            { "eng.lng",              langFiles.Concat(langAchiev).ToArray() },
            { "dialog_eng.lng",      langDialog },
            { "smallFont.xnb",       smallFonts },
            { "menuBackground.xnb",  backGround },
            { "ligth room.xnb",        lighting },
            { "link0.png",           linkImages },
            { "link0.atlas",          linkAtlas },
            { "npcs.png",             npcImages },
            { "items.png",           itemImages },
            { "intro.png",           introImage },
            { "intro.atlas",         introAtlas },
            { "minimap.png",         miniMapImg },
            { "objects.png",         objectsImg },
            { "photos.png",          photograph },
            { "ui.png",                uiImages },
            { "musicOverworld.data",  musicTile },
            { "dungeon3_1.map",       dungeon3M },
            { "dungeon3_1.map.data",  dungeon3D },
            { "bridge.map",          newBridgeM },
            { "bridge.map.data",     newBridgeD },
            { "BowWow.ani",          bowwowanim },
            { "mapPlayer.ani",       dungeonani },
            { "boomerang.ani",        boomerang },
            { "explosion0.ani",       effectani },
            { "ShockEffect.xnb",     shaderFile }
        };

        private static readonly HashSet<string> _targetList = fileTargets.Values.SelectMany(v => v).ToHashSet(StringComparer.OrdinalIgnoreCase);

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        PROGRESS CODE : TRACK AND UPDATE THE PROGRESS BAR BY KEEPING TRACK OF THE FILE COUNTS.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private async static Task ShowWarning(string title, string message, bool altSound = false)
        {
            if (_headlessMode)
                Console.WriteLine("WARNING: " + message);
            else
                await OkayWindow.ShowAsync(title, message, timeoutSeconds: 10, altSound: altSound);
        }

        public interface IProgressWindow
        {
            void UpdateProgressBar(int value);
            void ShowSuccessLabel();
            void HideSuccessLabel();
        }

        public interface EProgressWindow
        {
            void UpdateProgressBar(int value);
        }

        private static void ResetProgress()
        {
            // Reset the variables.
            _fileCount = 0;
            _totalCount = 0;
            Config.ActiveWindow?.UpdateProgressBar(0);
            Config.ActiveWindow?.HideSuccessLabel();
        }

        private static void UpdateProgress()
        {
            // Update the file count.
            _fileCount++;

            // Update the progress bar and process UI events (GUI mode only).
            if (!_headlessMode)
            {
                int progress = _totalCount > 0 ? (int)(_fileCount * 100.0 / _totalCount) : 0;
                Config.ActiveWindow?.UpdateProgressBar(progress);
            }
        }

        private static void ShowPatchingSuccessLabel()
        {
            if (!_headlessMode)
                Config.ActiveWindow?.ShowSuccessLabel();
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        POST PATCHING CODE : GENERATE ANDROID APK
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool IsToolAvailable(string tool)
        {
            if (OperatingSystem.IsWindows()) return true; // Bundled tools are extracted on Windows
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    Arguments = tool,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit();
                return proc?.ExitCode == 0;
            }
            catch { return false; }
        }

        private static bool ZipAlignSupportsPFlag()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Config.ZipAlign,
                    Arguments = "",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var proc = Process.Start(psi);
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                return (stdout + stderr).Contains("-P ");
            }
            catch { return false; }
        }

        private static async Task<bool> VerifyAndroidTools()
        {
            if (OperatingSystem.IsWindows()) return true;

            var missingTools = new List<string>();
            if (!IsToolAvailable(Config.JavaExe)) missingTools.Add("java");
            if (!IsToolAvailable(Config.ZipAlign)) missingTools.Add("zipalign");
            if (!IsToolAvailable(Config.SevenZip))
            {
                if (IsToolAvailable("7za")) Config.SevenZip = "7za";
                else if (IsToolAvailable("7zz")) Config.SevenZip = "7zz";
                else missingTools.Add("7z (or 7za,7zz)");
            }

            // apksigner.jar is bundled in android_tools.zip and extracted to the temp folder.
            // We should verify that we can actually run java, which is checked above.
            // On non-Windows, we rely on the system 'java' to run the bundled .jar.
            if (missingTools.Count > 0)
            {
                string message = "The following tools are required for APK generation but were not found in your PATH:\n\n" +
                                 string.Join(", ", missingTools);
                throw new Exception(message);
            }

            if (!ZipAlignSupportsPFlag())
                throw new Exception("Your version of zipalign is too old. Please update to a version of zipalign supporting -P.");

            return true;
        }

        private static async Task GenerateAPKFile()
        {
            // Verify tools are available on Linux/macOS, will throw if missing.
            await VerifyAndroidTools();
                // throw new Exception("Android tools (java, zipalign, 7z) are missing or not in PATH.");

            // Paths to the temporary and final APK files.
            string androidPath = Path.Combine(Config.TempFolder, "android").CreatePath();
            string stageRoot   = Path.Combine(androidPath, "com.zelda.ladxhd");
            string apkUnsigned = Path.Combine(androidPath, "unsigned.apk");
            string apkAligned  = Path.Combine(androidPath, "aligned.apk");
            string apkSigned   = Path.Combine(androidPath, "signed.apk");
            string apkFinalize = Path.Combine(Config.BaseFolder, "ladxhd-archipelago.apk");

            // Clean up any previous APK files.
            apkUnsigned.RemovePath();
            apkAligned.RemovePath();
            apkSigned.RemovePath();
            apkFinalize.RemovePath();

            // Extract tools and the stripped base APK.
            await Utilities.DownloadAndExtractFile("android_tools.zip", androidPath);

            // If on Windows, download 7-zip.
            if (OperatingSystem.IsWindows())
            {
                await Utilities.DownloadAndExtractFile("7zip.zip", Config.TempFolder);
            }
            // Write the base APK from resources.
            await Utilities.DownloadAndExtractFile("android_base.apk", apkUnsigned);



            // Mods can be written into the APK so we need the command to 7-zip to be a bit more dynamic.
            // 7-Zip internal paths should use the internal separator (usually \ is fine, but we'll use Path.Combine style logic)
            string assetsContent = Path.Combine("assets", "Content", "*");
            string assetsData    = Path.Combine("assets", "Data", "*");
            var sevenZipArgs = new List<string> { "a", "-tzip", apkUnsigned, assetsContent, assetsData };

            // If the user wants to pack mods then add it to the command.
            if (Config.AndroidMods)
                sevenZipArgs.Add(Path.Combine("assets", "Mods", "*"));

            // Add the remaining arguments to the 7-Zip command.
            sevenZipArgs.AddRange(new[] { "-r", "-mx=9", "-mm=Deflate" });

            // Inject only Content/Data into the base APK, then align/sign/verify.
            Utilities.RunProcess(Config.SevenZip, stageRoot, sevenZipArgs);
            Utilities.RunProcess(Config.ZipAlign, stageRoot, new List<string> { "-P", "16", "-f", "-v", "4", apkUnsigned, apkAligned });

            // Use apksigner.jar with java on all platforms.
            Utilities.RunProcess(Config.JavaExe, stageRoot, new List<string> { "-jar", Config.ApkSign, "sign", "--ks", Config.KeyStore, "--ks-key-alias", "zelda-la", "--ks-pass", "pass:zeldala", "--out", apkSigned, apkAligned });
            Utilities.RunProcess(Config.JavaExe, stageRoot, new List<string> { "-jar", Config.ApkSign, "verify", "-v", apkSigned });

            // Verify the signed APK exists before moving it.
            if (apkSigned.TestPath())
            {
                // Remove the temporary APK files we no longer need.
                apkUnsigned.RemovePath();
                apkAligned.RemovePath();

                // Move the final APK to the root folder.
                apkSigned.MovePath(apkFinalize, true);
            }
            else
            {
                throw new Exception("The signed APK was not generated.");
            }
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        POST PATCHING CODE : LINUX / MACOS FINALIZATION SCRIPTS
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        // Sets the executable bit (chmod +x) on a file.
        private static void SetExecutableBit(string path)
        {
            if (!File.Exists(path)) return;
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                           UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                           UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }

        // Ad-hoc codesign an executable, dylib, or app bundle.
        private static void CodeSign(string path)
        {
            if (!OperatingSystem.IsMacOS() || (!File.Exists(path) && !Directory.Exists(path))) return;
            var psi = new ProcessStartInfo
            {
                FileName = "codesign",
                Arguments = $"--sign - --force --deep \"{path}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi)?.WaitForExit();
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, string[]? excludeFolders = null)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    if (excludeFolders != null && excludeFolders.Contains(subDir.Name))
                        continue;

                    // Preserve symlinks rather than copying their contents.
                    if (subDir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        string? linkTarget = subDir.LinkTarget;
                        if (linkTarget != null)
                            Directory.CreateSymbolicLink(Path.Combine(destinationDir, subDir.Name), linkTarget);
                        continue;
                    }

                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true, excludeFolders);
                }
            }
        }

        private static void RunLinuxFinalization()
        {
            string executableName = Path.GetFileNameWithoutExtension(Config.ZeldaEXE);
            string exePath = Path.Combine(Config.BaseFolder, executableName);
            SetExecutableBit(exePath);

            string launcherPath = Path.Combine(Config.BaseFolder, "Launcher");
            if (File.Exists(launcherPath))
                SetExecutableBit(launcherPath);
        }

        // Finalizing for macOS requires:
        //   * setting the executable bit on game and launcher binaries
        //   * signing all binaries (executable and libraries)
        //   * bundling as app bundles (game, game + launcher), and signing the bundles
        private static void RunMacOSFinalization()
        {
            string name = Path.GetFileNameWithoutExtension(Config.ZeldaEXE);
            string baseFolder = Config.BaseFolder;
            string tempFolder = Config.TempFolder;
            
            string bundle = Path.Combine(baseFolder, $"{name}.app");
            string bundleTmp = Path.Combine(tempFolder, $"{name}.app");
            string launcherBundle = Path.Combine(baseFolder, $"{name} Launcher.app");
            string launcherBundleTmp = Path.Combine(tempFolder, $"{name} Launcher.app");

            // Clean slate
            if (Directory.Exists(bundleTmp))
                Directory.Delete(bundleTmp, true);
            if (Directory.Exists(launcherBundleTmp))
                Directory.Delete(launcherBundleTmp, true);

            // Set executable bit and codesign binaries
            string mainExe = Path.Combine(baseFolder, name);
            SetExecutableBit(mainExe);
            CodeSign(mainExe);
            
            string launcherExe = Path.Combine(baseFolder, "Launcher");
            SetExecutableBit(launcherExe);
            CodeSign(launcherExe);

            // Codesign dylibs
            string[] dylibs = { "libopenal.dylib", "libSDL2-2.0.0.dylib", "libAvaloniaNative.dylib", "libHarfBuzzSharp.dylib", "libSkiaSharp.dylib"};
            foreach (var dylib in dylibs)
            {
                string dylibPath = Path.Combine(baseFolder, dylib);
                if (File.Exists(dylibPath))
                    CodeSign(dylibPath);
            }

            // Create game bundle structure
            string contentsPath = Path.Combine(bundleTmp, "Contents");
            string macOSPath = Path.Combine(contentsPath, "MacOS");
            string resourcesPath = Path.Combine(contentsPath, "Resources");
            
            Directory.CreateDirectory(macOSPath);
            Directory.CreateDirectory(resourcesPath);

            // Copy binaries and dylibs
            File.Copy(mainExe, Path.Combine(macOSPath, name), true);
            SetExecutableBit(Path.Combine(macOSPath, name));
            File.Copy(launcherExe, Path.Combine(macOSPath, "Launcher"), true);
            SetExecutableBit(Path.Combine(macOSPath, "Launcher"));
            foreach (var dylib in dylibs)
            {
                string dylibPath = Path.Combine(baseFolder, dylib);
                if (File.Exists(dylibPath))
                    File.Copy(dylibPath, Path.Combine(macOSPath, dylib), true);
            }

            // Copy Data, Content, and Mods
            string dataSrc = Path.Combine(baseFolder, "Data");
            if (Directory.Exists(dataSrc))
                CopyDirectory(dataSrc, Path.Combine(macOSPath, "Data"), true, null);

            string contentSrc = Path.Combine(baseFolder, "Content");
            if (Directory.Exists(contentSrc))
                CopyDirectory(contentSrc, Path.Combine(macOSPath, "Content"), true, null);

            string modsSrc = Path.Combine(baseFolder, "Mods");
            if (Directory.Exists(modsSrc))
                CopyDirectory(modsSrc, Path.Combine(macOSPath, "Mods"), true, null);

            // Write Resources
            File.WriteAllBytes(Path.Combine(resourcesPath, "Icon.icns"), Resources.GetBytes("Icon.icns"));

            string template = System.Text.Encoding.UTF8.GetString(Resources.GetBytes("Info.plist.template"));
            string plist = template
                .Replace("{EXECUTABLE}", name)
                .Replace("{VERSION}", Config.Version);
            File.WriteAllText(Path.Combine(contentsPath, "Info.plist"), plist, new System.Text.UTF8Encoding(false));

            // Create Content symlink
            File.CreateSymbolicLink(Path.Combine(resourcesPath, "Content"), "../MacOS/Content");

            // Create launcher bundle by copying game bundle and changing executable pointer
            CopyDirectory(bundleTmp, launcherBundleTmp, true, null);

            string launcherPlistPath = Path.Combine(launcherBundleTmp, "Contents", "Info.plist");
            string launcherPlist = File.ReadAllText(launcherPlistPath)
                .Replace($"<string>{name}</string>", "<string>Launcher</string>")
                .Replace("com.zelda.ladxhd", "com.zelda.ladxhd.launcher");
            File.WriteAllText(launcherPlistPath, launcherPlist, new System.Text.UTF8Encoding(false));

            // Codesign the app bundles
            CodeSign(bundleTmp);
            CodeSign(launcherBundleTmp);

            // Move completed bundles
            if (Directory.Exists(bundle))
                Directory.Delete(bundle, true);
            Directory.Move(bundleTmp, bundle);

            if (Directory.Exists(launcherBundle))
                Directory.Delete(launcherBundle, true);
            Directory.Move(launcherBundleTmp, launcherBundle);
        }

        private async static Task HostFinalizationFunctions()
        {
            bool isLinux = Config.SelectedPlatform == Platform.Linux_x86 || Config.SelectedPlatform == Platform.Linux_Arm64;
            bool isMacOS = Config.SelectedPlatform == Platform.MacOS_x86 || Config.SelectedPlatform == Platform.MacOS_Arm64;

            if (isLinux && OperatingSystem.IsLinux())
            {
                try { RunLinuxFinalization(); }
                catch {
                    await ShowWarning(
                        "Patching partially complete",
                        "The game was patched, but the Linux finalization steps failed to apply.\n" +
                        "Please check https://gitlab.com/bighead.0/ladxhd_updated/-/blob/main/LINUX.md."
                    );
                }
            }
            else if (isMacOS && OperatingSystem.IsMacOS())
            {
                try { RunMacOSFinalization(); }
                catch {
                    await ShowWarning(
                        "Patching partially complete",
                        "The game was patched, but the macOS finalization steps failed to apply.\n" +
                        "Please check https://gitlab.com/bighead.0/ladxhd_updated/-/blob/main/MACOS.md."
                    );
                }
            }
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        POST PATCHING CODE : ADDITIONAL FILE AND FOLDER HANDLING

-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private async static Task CopyNewFiles()
        {
            string dataPath;

            // Set the path to "Data" based on platform selected.
            if (Config.SelectedPlatform == Platform.Android)
                dataPath = Path.Combine(Config.TempFolder, "android", "com.zelda.ladxhd", "assets", "Data");
            else
                dataPath = Path.Combine(Config.BaseFolder, "Data");

            // Set up the path to the Icon.
            string iconPath = Path.Combine(dataPath, "Icon").CreatePath();

            // Write the icon to the "Data\Icon" folder.
            string iconIcoFile = Path.Combine(iconPath, "Icon.ico");
            File.WriteAllBytes(iconIcoFile, Resources.GetBytes("Icon.ico"));

            // Write the bitmap icon to the "Data\Icon" folder.
            string iconBmpFile = Path.Combine(iconPath, "Icon.bmp");
            File.WriteAllBytes(iconBmpFile, Resources.GetBytes("Icon.bmp"));

            // Write the png icon to the the "Data\Icon" folder.
            string iconPngFile = Path.Combine(iconPath, "Icon.png");
            File.WriteAllBytes(iconPngFile, Resources.GetBytes("Icon.png"));

            // Write the svg icon to the the "Data\Icon" folder.
            string iconSvgFile = Path.Combine(iconPath, "Icon.svg");
            File.WriteAllBytes(iconSvgFile, Resources.GetBytes("Icon.svg"));

            // If it's the Windows OpenGL build then it needs SDL2.dll.
            if (Config.SelectedPlatform == Platform.Windows && Config.SelectedGraphics == GraphicsAPI.OpenGL)
            {
                string SdlPath = Path.Combine(Config.BaseFolder, "SDL2.dll");
                File.WriteAllBytes(SdlPath, Resources.GetBytes("SDL2.dll"));
            }
            // Extract the achievement images to the "Data\Achievements" folder.
            string achievementPath = Path.Combine(dataPath, "Achievements").CreatePath();
            await Utilities.DownloadAndExtractFile("achievements.zip", achievementPath);
        }

        private static void RemoveBadBackupFiles()
        {
            // Because old versions of the patchers saved "new" files, we need to remove them or they will cause problems.
            string[][] list = { langFiles, langAchiev, langDialog, smallFonts, backGround, lighting, linkImages, linkAtlas, npcImages, itemImages, introImage, introAtlas, miniMapImg, 
                                objectsImg, photograph, uiImages, musicTile, dungeon3M, dungeon3D, newBridgeM, newBridgeD, bowwowanim, dungeonani, boomerang, shaderFile };

            string[] remove = list.SelectMany(x => x).ToArray();

            // Loop through the files in the backup folder.
            foreach (string file in Config.BackupPath.GetFiles("*", true))
            {
                // Get the file as a file item which gives us some cool properties to reference.
                FileItem fileItem = new FileItem(file);

                // If the current array file exists then remove it.
                if (remove.Contains(fileItem.Name))
                    fileItem.FullName.RemovePath();
            }
        }

        private static void CreateModFolders()
        {
            // The path to where Mods used to be located.
            string previousModPath = Path.Combine(Config.BaseFolder, "Data", "Mods");

            // Create the new mods folders.
            Config.AnimationMods.CreatePath(true);
            Config.DungeonMods.CreatePath(true);
            Config.GraphicsMods.CreatePath(true);
            Config.MusicMods.CreatePath(true);
            Config.LanguageMods.CreatePath(true);
            Config.MapsMods.CreatePath(true);
            Config.SoundsMods.CreatePath(true);
            Config.LAHDModPath.CreatePath(true);

            // Find the old "Mods" path for lahdmods and exit if it doesn't exist.
            if (!Directory.Exists(previousModPath))
                return;

            // Move any lahdmods in the old "Mods" folder to the new location.
            foreach (string file in Directory.GetFiles(previousModPath, "*", SearchOption.AllDirectories))
            {
                FileItem fileItem = new FileItem(file);
                string newModLoc = Path.Combine(Config.LAHDModPath, fileItem.Name);
                fileItem.FullName.MovePath(newModLoc, true);
            }
        }

        private static void MoveFirstVersionSaveGames()
        {
            // Don't move saves unless it's actually v1.0.0 and a "portable.txt" doesn't exist. 
            if (_patchFromBackup || Config.PortableTxt.TestPath() || Config.SelectedPlatform == Platform.Android)
                return;

            // Throw together some paths.
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string oldSavePath = Path.Combine(Config.BaseFolder, "SaveFiles");
            string oldSettings = Path.Combine(Config.BaseFolder, "settings");
            string newSavePath = Path.Combine(appDataPath, "Zelda_LA", "SaveFiles");
            string newSettings = Path.Combine(appDataPath, "Zelda_LA", "settings");

            // Create the folder in the location.
            string zeldaSavePath = Path.Combine(appDataPath, "Zelda_LA");
            zeldaSavePath.CreatePath();

            // Move the save folder and settings to the global save path.
            oldSavePath.MovePath(newSavePath);
            oldSettings.MovePath(newSettings);
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        POST PATCHING CODE : MASTER FUNCTION : STUFF THAT IS DONE AFTER PATCHING HAS FINISHED.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private async static Task PostPatchingFunctions()
        {
            // We need the new FNT files for Chinese font, the new editor fonts, and a bitmap icon for OpenGL.
            await CopyNewFiles();

            // If the user is patching v1.0.0 try to keep their save games.
            MoveFirstVersionSaveGames();

            // They will probably be there again so remove them one more time.
            RemoveBadBackupFiles();

            // After migration, some map files are not needed.
            CleanUp.RemoveJunkMapFiles();

            // Create the mod folders if they don't exist.
            CreateModFolders();

            // Android needs the controller buttons and to be made into an APK.
            if (Config.SelectedPlatform == Platform.Android)
            {
                // Extract the android buttons to the game directory in "Data/Buttons".
                string stageRoot   = Path.Combine(Config.TempFolder, "android", "com.zelda.ladxhd");
                string buttonsPath = Path.Combine(stageRoot, "assets", "Data", "Buttons").CreatePath();
                await Utilities.DownloadAndExtractFile("android_buttons.zip", buttonsPath);

                // If the user wants to install Mods directly into the APK.
                if (Config.AndroidMods)
                {
                    string modsStagePath = Path.Combine(stageRoot, "assets", "Mods");
                    Config.ModsPath.CopyPath(modsStagePath, true);
                }
                // Generate the APK file.
                await GenerateAPKFile();
            }
            // MacOS has additional library files that are required.
            else if (Config.SelectedPlatform == Platform.MacOS_x86 || Config.SelectedPlatform == Platform.MacOS_Arm64)
            {
                // The files are different depending on MacOS CPU.
                string zipName = Config.SelectedPlatform == Platform.MacOS_x86
                    ? "macos_x86_files.zip" 
                    : "macos_arm64_files.zip";

                // Extract the zip containing the MacOS files.
                await Utilities.DownloadAndExtractFile(zipName, Config.BaseFolder);
            }
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        PATCHING CODE : PATCH FILES USING VCDIFF PATCHES TO UPDATE TO THE LATEST VERSION.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static Dictionary<string, string> _gameFileLookup;

        private static void BuildGameFileLookup()
        {
            // Create a dictionary to store both file name (key) and the path to it (value).
            _gameFileLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Loop through all files in the base folder.
            foreach (string file in Directory.EnumerateFiles(Config.BaseFolder, "*", SearchOption.AllDirectories))
            {
                // Get a file item of the file.
                var fileItem = new FileItem(file);

                // Always skip files in the mods, backup, and temp folders.
                if (fileItem.IsInFolder("Mods") || fileItem.IsInFolder("Backup") || fileItem.IsInFolder("_temp"))
                    continue;

                // Get the name of the file for the key.
                string name = Path.GetFileName(file);

                // Store the key if it doesn't exist and set the value to the full path.
                if (!_gameFileLookup.ContainsKey(name))
                    _gameFileLookup[name] = file;
                // If there is a duplicate key, add a hashtag that will be trimmed later.
                else
                    _gameFileLookup[name + "#"] = file;
            }
        }

        private static void HandleMultiFilePatches(string filePath, string overridePath = "")
        {
            // Use the input path to get a file item.
            FileItem fileItem = new FileItem(filePath);

            // Use the file name to get the files that it creates.
            if (!fileTargets.TryGetValue(fileItem.Name, out var targets))
                return;

            // Loop through the target file names.
            foreach (string newFile in targets)
            {
                // Set up the path to the patch.
                string vcdiffFile = Path.Combine(Config.TempFolder, "patches", newFile + ".vcdiff");

                // Make sure a patch exists.
                if (!vcdiffFile.TestPath())
                    continue;

                // Where the patched file will be temporarily held.
                string patchedFile = Path.Combine(Config.TempFolder, "patchedFiles", newFile);

                // The path to move the file. 
                string newFilePath = Path.Combine(fileItem.DirectoryName, newFile);

                // If an override is provided overwrite the path with it.
                if (overridePath != "")
                    newFilePath = Path.Combine(overridePath, newFile);

                // Apply the patch to the file.
                VCDiff.Execute(Operation.Apply, fileItem.FullName, vcdiffFile, patchedFile, newFilePath);
            }
        }

        private static void RestoreOriginalDungeon3Map()
        {
            // Get all possible locations to find the dungeon 3 map and data files.
            string mapsPath  = Path.Combine(Config.BaseFolder, "Data", "Maps");
            string[] d3Files = { "dungeon3.map", "dungeon3_1.map", "dungeon3.map.data", "dungeon3_1.map.data" };

            // Delete all versions of these files.
            foreach (string file in d3Files)
            {
                Path.Combine(mapsPath, file).RemovePath();
                Path.Combine(Config.BackupPath, file).RemovePath();
            }
            // Write the v1.0.0 "dungeon3_1.map" and "dungeon3_1.map.data" files in the "Data\Maps" folder.
            File.WriteAllBytes(Path.Combine(mapsPath, "dungeon3_1.map"), Resources.GetBytes("d3map"));
            File.WriteAllBytes(Path.Combine(mapsPath, "dungeon3_1.map.data"), Resources.GetBytes("d3mapdata"));
        }

        private static void VerifyDungeon3Map()
        {
            // We're only left with an APK so there's nothing to verify.
            if (Config.SelectedPlatform == Platform.Android)
                return;

            // The path to where the patched "dungeon3.map" file should be.
            string dungeon3Map = Path.Combine(Config.BaseFolder, "Data", "Maps", "dungeon3.map");

            // If it doesn't exist then report it to the user who should report it to me.
            if (!dungeon3Map.TestPath())
            {
                throw new PathOperationException(
                    "The map file \"dungeon3.map\" was not present after patching. Try running the " +
                    "patcher again and please report you seen this error on the Github repo!");
            }
        }

        private static void PrepareZeldaLAExecutable(bool isAndroid, bool isLinux, bool isMacOS)
        {
            // If we're patching for Android, we don't need to touch the executable.
            if (isAndroid)
                return;

            // Set the path to both potential executables.
            string winEXE = Config.ZeldaEXE;
            string altEXE = Config.ZeldaEXE.Substring(0, Config.ZeldaEXE.Length - 4);

            // Set the path to what the executable should be.
            string exePath = (isLinux || isMacOS) ? altEXE : winEXE;

            // If patching from backup remove all versions of the executable.
            if (_patchFromBackup)
            {
                winEXE.RemovePath();
                altEXE.RemovePath();
            }
            // If coming from backup or patching v1.0.0 to Linux/MacOS it will need renamed.
            if (_executable != exePath)
                _executable.MovePath(exePath, true);

            // Remove any Linux / MacOS specific files.
            string[] libFiles = new string[]{ "System.IO.Compression.Native.a", "System.Native.a", "System.Net.Http.Native.a", 
                "System.Net.Security.Native.a", "System.Security.Cryptography.Native.OpenSsl.a", "libopenal.dylib", "libSDL2-2.0.0.dylib",
                "libAvaloniaNative.dylib", "libHarfBuzzSharp.dylib", "libopenal.dylib", "libSDL2-2.0.0.dylib", "libSkiaSharp.dylib"};

            foreach (string file in libFiles) 
            {
                string filePath = Path.Combine(Config.BaseFolder, file);
                filePath.RemovePath();
            }
        }

        private async static Task PatchGameFiles()
        {
            await Task.Run(async () =>
            {
                // Check to see which platform we are patching for.
                bool isAndroid = Config.SelectedPlatform == Platform.Android;
                bool isWindows = Config.SelectedPlatform == Platform.Windows;
                bool isLinux   = Config.SelectedPlatform == Platform.Linux_x86 || Config.SelectedPlatform == Platform.Linux_Arm64;
                bool isMacOS   = Config.SelectedPlatform == Platform.MacOS_x86 || Config.SelectedPlatform == Platform.MacOS_Arm64;

                // Create the backup path if it doesn't exist.
                Config.BackupPath.CreatePath();

                // The v1.0.0 executable must be in the base path.
                PrepareZeldaLAExecutable(isAndroid, isLinux, isMacOS);

                // Remove any garbage files that will just mess up the patcher.
                RemoveBadBackupFiles();
                _filesPatched = 0;

                // Restore v1.0.0 version of "dungeon3_1.map" and "dungeon3_1.map.data".
                RestoreOriginalDungeon3Map();

                // Build fast lookup of game files (name -> full path)
                BuildGameFileLookup();

                // Get a count of how many files there are.
                _totalCount = _gameFileLookup.Count;

                // Loop through all files in the collection.
                foreach (var kvp in _gameFileLookup)
                {
                    // We don't need a perfect representation of progress just a rough idea of the time left.
                    UpdateProgress();

                    string fileName = kvp.Key.TrimEnd('#');
                    string fullPath = kvp.Value;

                    // If it's a file that relies on a file with a different name, skip it.
                    if (_targetList.Contains(fileName))
                        continue;

                    // Get the file as a file item which gives us some cool properties to reference.
                    FileItem fileItem = new FileItem(fullPath);

                    // On Android we only want files in Content or Data folders.
                    if (isAndroid && (!fileItem.IsInFolder("Content") && !fileItem.IsInFolder("Data")))
                        continue;

                    // Get the backup path to test for existing backups and create new ones to it.
                    string backupPath = Path.Combine(Config.BackupPath, fileName);
                    string vcDiffFile = Path.Combine(Config.TempFolder, "patches", fileName + ".vcdiff");
                    bool patchExists = vcDiffFile.TestPath();

                    // Default both input and output to the file item path.
                    string inputFile = fullPath;
                    string outputFile = fullPath;
                    string overridePath = "";

                    // Android handles backups and multi-files a bit differently.
                    if (isAndroid)
                    {
                        // If a patch and a backup exist set the input to the backup file.
                        if (patchExists && backupPath.TestPath())
                            inputFile = backupPath;

                        // Set the destination path to the Android folder.
                        string relative = fileItem.DirectoryName.Replace(Config.BaseFolder, "").TrimStart(Path.DirectorySeparatorChar);
                        string destPath = Path.Combine(Config.TempFolder, "android", "com.zelda.ladxhd", "assets", relative).CreatePath();

                        // Update the output file using the destination path and set the override for MultiFilePatches.
                        outputFile = Path.Combine(destPath, fileName);
                        overridePath = destPath;

                        // If the patch doesn't exist, we can just copy the file to the Android folder.
                        if (!patchExists)
                            fullPath.CopyPath(outputFile, true);
                    }
                    // Windows is a bit simpler.
                    else if (isWindows || isLinux || isMacOS)
                    {
                        // If a patch file exists.
                        if (patchExists)
                        {
                            // If a backup doesn't exist, create one. If one does exist, overwrite the current file with it.
                            if (!backupPath.TestPath())
                                fullPath.CopyPath(backupPath, true);
                            else
                                backupPath.CopyPath(fullPath, true);
                        }
                    }
                    // If this file creates other files do so now.
                    if (fileTargets.TryGetValue(fileName, out _))
                        HandleMultiFilePatches(inputFile, overridePath);

                    // If this file is not patched directly then move on to the next.
                    if (!patchExists)
                        continue;

                    // Patch the file.
                    string patchedFile = Path.Combine(Config.TempFolder, "patchedFiles", fileName);
                    VCDiff.Execute(Operation.Apply, inputFile, vcDiffFile, patchedFile, outputFile);
                    _filesPatched++;
                }
                // Verify that the dungeon3.map file was succesfully created.
                VerifyDungeon3Map();

                // There's stuff to do after patching. This function just gathers it all into one method.
                await PostPatchingFunctions();
            });
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        PATCH / LAUNCHER EXTRACTION FUNCTIONS : EXTRACTS PATCHES OR LAUNCHER BASED ON SELECTED PLATFORM AND CREATES THE PATCH FOLDERS.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static async Task ExtractPatches()
        {
            // Default to Windows Direct-X patches.
            string zipName = "patches_win_dx.zip";

            // Get the name of the zip file depending on the selected platform.
            switch (Config.SelectedPlatform)
            {
                case Platform.Android:     { zipName = "patches_android.zip";     break; }
                case Platform.Linux_x86:   { zipName = "patches_linux_x86.zip";   break; }
                case Platform.Linux_Arm64: { zipName = "patches_linux_arm64.zip"; break; }
                case Platform.MacOS_x86:   { zipName = "patches_macos_x86.zip";   break; }
                case Platform.MacOS_Arm64: { zipName = "patches_macos_arm64.zip"; break; }
                case Platform.Windows:     { if (Config.SelectedGraphics == GraphicsAPI.OpenGL) { zipName = "patches_win_gl.zip"; } break;  }
            };
            // Create the path to extract patches to and extract them.
            string patchesPath = Path.Combine(Config.TempFolder, "patches").CreatePath();
            await Utilities.DownloadAndExtractFile(zipName, patchesPath);

            // Create the path to where patched files will go.
            Path.Combine(Config.TempFolder, "patchedFiles").CreatePath(true);
        }

        private static async Task ExtractLauncher()
        {
            // Platform determines which launcher to extract.
            string zipName = "";
            bool isMacOS = false;

            switch (Config.SelectedPlatform)
            {
                // Just exit if it's Android. No launcher for it.
                case Platform.Android:      { return; }

                // Each has its own type of launcher.
                case Platform.Linux_x86:    { zipName = "launcher_linux_x86.zip"; break; }
                case Platform.Linux_Arm64:  { zipName = "launcher_linux_arm64.zip"; break; }
                case Platform.MacOS_x86:    { zipName = "launcher_macos_x86.zip"; isMacOS = true; break; }
                case Platform.MacOS_Arm64:  { zipName = "launcher_macos_arm64.zip"; isMacOS = true; break; }

                // Default to Windows.
                default:                    { zipName = "launcher_windows.zip"; break; }
            }
            // Remove the launcher if it exists.
            Config.Launcher.RemovePath();
            Config.WLauncher.RemovePath();

            // MacOS may have additional files included with the launcher.
            if (isMacOS)
            {
                Path.Combine(Config.BaseFolder, "libAvaloniaNative.dylib").RemovePath();
                Path.Combine(Config.BaseFolder, "libHarfBuzzSharp.dylib").RemovePath();
                Path.Combine(Config.BaseFolder, "libSkiaSharp.dylib").RemovePath();
            }
            // Write the zipfile, extract it, then delete it.
            await Utilities.DownloadAndExtractFile(zipName, Config.BaseFolder);
        }
/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        SETUP / VALIDATION CODE : SET UP WHETHER PATCHING FROM v1.0.0 OR PATCHING FROM BACKUP FILES AND VERIFY IF PATCHING SHOULD TAKE PLACE.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool CompareHashes(string hash1, string hash2, string exePath, bool isBackup = false)
        {
            // If the hashes match then we have what we are looking for.
            if (exePath.TestPath() && hash1 == hash2)
            {
                _executable = exePath;
                _patchFromBackup = isBackup;
                return true;
            }
            // If they don't match blank out the executable path.
            _executable = "";
            return false;
        }

        private async static Task<bool> SetSourceFile()
        {
            // Start with the hash of the file found in the main folder.
            string exeCheck = Config.ZeldaEXE;
            string goodHash = "F4ADFBA864B852908705EA6A18A48F18";

            // Checks the file path, hashes, and sets the variables.
            bool CheckFile(string path, bool isBackup) { return CompareHashes(goodHash, path.CalculateHash("MD5"), path, isBackup); }

            // First check the executable found in the main folder.
            if (CheckFile(exeCheck, false)) { return true; }

            // Try the same executable without the extension.
            exeCheck = exeCheck.Substring(0, exeCheck.Length - 4);
            if (CheckFile(exeCheck, false)) { return true; }

            // If it's already been patched try to find the executable in the backup path.
            exeCheck = Path.Combine(Config.BackupPath, "Link's Awakening DX HD.exe");
            if (CheckFile(exeCheck, true)) {return true; }

            // If it doesn't exist with the extension check without it.
            exeCheck = exeCheck.Substring(0, exeCheck.Length - 4);
            if (CheckFile(exeCheck, true)) { return true; }

            // If we still don't have the good hash then we're screwed.
            var message = "Could not find the original \"Link's Awakening DX HD.exe\" to patch. It is suggested to start over with the original release of v1.0.0 and run it from there.";
            await ShowWarning("Original Executable Not Found", message);

            return false;
        }

        private async static Task<bool> ValidateStart()
        {
            // The verification message changes if Android is selected to patch.
            string title = Config.SelectedPlatform == Platform.Android
                ? "Create v" + Config.Version + " APK"
                : "Patch to v" + Config.Version;
            string message = Config.SelectedPlatform == Platform.Android
                ? "Create an APK using game files patching to v" + Config.Version + "?"
                : "Are you sure you wish to patch the game to v" + Config.Version + "?";
            return await YesNoWindow.ShowAsync(title, message);
        }

        private async static Task ReportFinished()
        {
            string message;

            if (_headlessMode)
            {
                Console.WriteLine("============================================");
                Console.WriteLine("SUCCESS: Game patched to v" + Config.Version);
                Console.WriteLine("");
                Console.WriteLine("Files patched: " + _filesPatched);
            }
            else
            {
                if (Config.SelectedPlatform == Platform.Android)
                {
                    message = _patchFromBackup
                        ? "Creating an APK from v1.0.0 backup files was successful. The game version is set to v"+ Config.Version + "." 
                        : "Creating an APK from original v1.0.0 files was successful. The game version is set to v"+ Config.Version + ".";
                    await ShowWarning("APK Created", message, true);
                    ShowPatchingSuccessLabel();
                }
                else
                {
                    message = _patchFromBackup
                        ? "Patching the game from v1.0.0 backup files was successful. The game was updated to v"+ Config.Version + "." 
                        : "Patching Link's Awakening DX HD v1.0.0 was successful. The game was updated to v"+ Config.Version + ".";
                    await ShowWarning("Patching Complete", message, true);
                    ShowPatchingSuccessLabel();
                }
            }
        }

        private async static Task TryRemoveTempPath()
        {
            // Try to remove the temp path.
            try { Config.TempFolder.RemovePath(); }

            // If it fails, show an error message.
            catch { await ShowWarning("Patching Complete", "The game was patched successfully, but the patcher failed to delete the \"temp\" folder. Please report this as an issue on the Github repo!"); }
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        MAIN PATCHING FUNCTION: THIS IS WHERE IT ALL BEGINS WHETHER IT'S PATCHING OR GENERATING AN APK.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private async static Task ResetAdvancedFile()
        {
            // Make sure the user is okay with this change.
            string message = $"This version has many changes that require a reset of the \"Mods\" settings. It is highly suggested to press \"Yes\" to reset settings or enemies will have the wrong HP values!";
            
            // It may be necessary to reset the advanced file on some versions.
            if (await YesNoWindow.ShowAsync("Reset Mods Settings", message))
            {
                // Get the location of the "advanced" file.
                string advancedPath = Utilities.GetAdvancedPath(AppContext.BaseDirectory);

                // Remove it completely. A new one will be written.
                advancedPath.RemovePath();
            }
        }

        public async static Task StartPatching()
        {
            // Reset exit code and progress bar.
            ExitCode = 2;
            ResetProgress();

            if (_headlessMode)
            {
                Console.WriteLine("LADXHD Patcher-Lite v" + Config.Version + " - Headless Mode");
                Console.WriteLine("============================================");
            }

            // Locate the v1.0.0 executable and set _executable / _patchFromBackup.
            // In headless mode suppress the dialog; ShowWarning will print to console instead.
            if (!await SetSourceFile())
            {
                if (_headlessMode) ExitCode = 1;
                return;
            }

            if (_headlessMode)
            {
                Console.WriteLine("Found game executable. Starting patch process...");
            }
            else
            {
                // GUI-only: confirm the user wants to patch.
                if (!await ValidateStart()) return;
                App.MainWindowInstance.EnableComponents(false);
            }

            try
            {
                // Remove temp path and recreate it.
                Config.TempFolder.RemovePath();
                Config.TempFolder.CreatePath(true);

                if (_headlessMode) Console.WriteLine("Extracting patches...");
                await ExtractPatches();

                if (_headlessMode) Console.WriteLine("Patching game files...");
                await PatchGameFiles();

                if (_headlessMode) Console.WriteLine("Extracting launcher...");
                await ExtractLauncher();

                if (_headlessMode) Console.WriteLine("Performing platform-specific finalization...");
                await HostFinalizationFunctions();

                // Reset the advanced file.
                if (!_headlessMode && _resetAdvancedFile)
                    await ResetAdvancedFile();

                // All steps completed successfully.
                ExitCode = 0;
                await ReportFinished();
            }
            catch (PathOperationException pex)
            {
                await ShowWarning("File Operation Failed", pex.Message);
            }
            catch (Exception ex)
            {
                await ShowWarning("Patching Failed", ex.Message);
            }
            finally
            {
                if (_headlessMode) Console.WriteLine("Cleaning up...");
                await TryRemoveTempPath();
                if (!_headlessMode)
                    App.MainWindowInstance.EnableComponents(true);
            }
        }
    }
}
