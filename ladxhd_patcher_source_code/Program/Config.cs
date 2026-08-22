using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using static LADXHD_Patcher.Functions;

namespace LADXHD_Patcher
{
    public class Config
    {
        public static readonly string Version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+')[0];

        public static string BaseFolder;
        public static string TempFolder;
        public static string ZeldaEXE;
        public static string BackupPath;

        public static string ModsPath;
        public static string AnimationMods;
        public static string DungeonMods;
        public static string GraphicsMods;
        public static string MusicMods;
        public static string LanguageMods;
        public static string MapsMods;
        public static string SoundsMods;
        public static string LAHDModPath;

        public static string Launcher;
        public static string WLauncher;
        public static string PortableTxt;

        public static string ApkSign;
        public static string ZipAlign;
        public static string KeyStore;
        public static string JavaExe;
        public static string SevenZip;

        public static bool AndroidMods;

        public static IProgressWindow? ActiveWindow { get; set; }

        public enum Platform { Windows, Android, Linux_x86, Linux_Arm64, MacOS_x86, MacOS_Arm64 }
        public static Platform SelectedPlatform;

        public enum GraphicsAPI { DirectX, OpenGL }
        public static GraphicsAPI SelectedGraphics;

        public static Platform GetNativePlatform()
        {
            bool isArm64 = RuntimeInformation.OSArchitecture == Architecture.Arm64;
            if (OperatingSystem.IsLinux()) return isArm64 ? Platform.Linux_Arm64 : Platform.Linux_x86;
            if (OperatingSystem.IsMacOS()) return isArm64 ? Platform.MacOS_Arm64 : Platform.MacOS_x86;
            return Platform.Windows;
        }

        public static void Initialize()
        {
            BaseFolder = AppContext.BaseDirectory;

#if MACOS
            // When running inside a .app bundle the binary lives at
            // Foo.app/Contents/MacOS/ — navigate up to the folder containing
            // the .app so companion files (game exe, Data/, Mods/, etc.) are reachable.
            var contentsDir = Path.GetFullPath(Path.Combine(BaseFolder, ".."));
            if (Path.GetFileName(contentsDir) == "Contents" &&
                File.Exists(Path.Combine(contentsDir, "Info.plist")))
            {
                BaseFolder = Path.GetFullPath(Path.Combine(contentsDir, "..", ".."));
            }
#endif

#if LINUX
            // When running as an AppImage, AppContext.BaseDirectory points inside
            // the read-only mount at /tmp/.mount_XXXXX/usr/bin/. The AppImage runtime
            // sets $APPIMAGE to the full path of the .AppImage file, so its parent
            // directory is the folder where the user dropped the patcher.
            var appImage = Environment.GetEnvironmentVariable("APPIMAGE");
            if (!string.IsNullOrEmpty(appImage) && File.Exists(appImage))
            {
                BaseFolder = Path.GetDirectoryName(appImage);
            }
#endif
            TempFolder    = Path.Combine(BaseFolder, "_temp");
            ZeldaEXE      = Path.Combine(BaseFolder, "Link's Awakening DX HD.exe");
            BackupPath    = Path.Combine(BaseFolder, "Data", "Backup");

            ModsPath      = Path.Combine(BaseFolder, "Mods");
            AnimationMods = Path.Combine(ModsPath, "Animations");
            DungeonMods   = Path.Combine(ModsPath, "Dungeon");
            GraphicsMods  = Path.Combine(ModsPath, "Graphics");
            MusicMods     = Path.Combine(ModsPath, "Music");
            LanguageMods  = Path.Combine(ModsPath, "Languages");
            MapsMods      = Path.Combine(ModsPath, "Maps");
            SoundsMods    = Path.Combine(ModsPath, "SoundEffects");
            LAHDModPath   = Path.Combine(ModsPath, "LAHDMods");

            Launcher      = Path.Combine(BaseFolder, "Launcher");
            WLauncher     = Path.Combine(BaseFolder, "Launcher.exe");
            PortableTxt   = Path.Combine(BaseFolder, "portable.txt");

            ApkSign       = Path.Combine(TempFolder, "android", "apksigner.jar");
            KeyStore      = Path.Combine(TempFolder, "android", "keystore.jks");

            if (OperatingSystem.IsWindows())
            {
                ZipAlign  = Path.Combine(TempFolder, "android", "zipalign.exe");
                JavaExe   = Path.Combine(TempFolder, "android", "java", "bin", "java.exe");
                SevenZip  = Path.Combine(TempFolder, "7z.exe");
            }
            else
            {
                ZipAlign  = "zipalign";
                JavaExe   = "java";
                SevenZip  = "7z";
            }
            // Adding controller support to the Launcher decided to generate unwanted junk when publishing which
            // just causes more problems and questions and issue reports. Now I need code to get rid of it.
            var fuckingGarbage = Path.Combine(BaseFolder, "Launcher.dll.config");
            fuckingGarbage.RemovePath();
        }
    }
}
