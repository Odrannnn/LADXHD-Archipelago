using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Mono.Options;
using static LADXHD_Patcher.Config;

namespace LADXHD_Patcher
{
    internal class Program
    {
        // Windows-only console attachment for headless mode output.
        [SupportedOSPlatform("windows")]
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [SupportedOSPlatform("windows")]
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [SupportedOSPlatform("windows")]
        private static void TryAttachConsole() => AttachConsole(ATTACH_PARENT_PROCESS);

        [SupportedOSPlatform("windows")]
        private static void TryFreeConsole() => FreeConsole();

        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        public static int Main(string[] args)
        {
            bool showHelp   = false;
            bool headlessMode = false;
            string? platformStr = null;
            string? graphicsStr = null;

            var opts = new OptionSet {
                { "headless|s|silent",   "Run in headless mode (no GUI, for automated installs).", _ => headlessMode = true },
                { "platform=",  "Target platform: windows, android, linux-x86, linux-arm64, macos-x86, macos-arm64. Default: windows.", v => platformStr = v },
                { "graphics=",  "Target graphics API: directx, opengl. Default: directx (windows), opengl (others).", v => graphicsStr = v },
                { "h|?|help",   "Show this help message.", _ => showHelp = true },
            };

            opts.Parse(args); // unrecognized args are silently ignored

            if (showHelp)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    TryAttachConsole();

                Console.WriteLine();
                Console.WriteLine("LADXHD Patcher v" + Config.Version);
                Console.WriteLine("Usage: LADXHD.Patcher.exe [options]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                opts.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
                Console.WriteLine("Exit codes: 0=Success, 1=Exe not found, 2=Patching failed, 3=Invalid arguments");
                Console.WriteLine();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    TryFreeConsole();
                return 0;
            }

            if (headlessMode)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    TryAttachConsole();

                Platform platform;
                GraphicsAPI graphics;

                try
                {
                    platform = platformStr?.ToLowerInvariant() switch {
                        null          => Config.GetNativePlatform(),
                        "windows"     => Platform.Windows,
                        "android"     => Platform.Android,
                        "linux-x86"   => Platform.Linux_x86,
                        "linux-arm64" => Platform.Linux_Arm64,
                        "macos-x86"   => Platform.MacOS_x86,
                        "macos-arm64" => Platform.MacOS_Arm64,
                        _ => throw new ArgumentException($"Invalid --platform value '{platformStr}'. Valid values: windows, android, linux-x86, linux-arm64, macos-x86, macos-arm64")
                    };

                    if (graphicsStr != null)
                    {
                        graphics = graphicsStr.ToLowerInvariant() switch {
                            "directx" => GraphicsAPI.DirectX,
                            "opengl"  => GraphicsAPI.OpenGL,
                            _ => throw new ArgumentException($"Invalid --graphics value '{graphicsStr}'. Valid values: directx, opengl")
                        };
                        if (graphics == GraphicsAPI.DirectX && platform != Platform.Windows)
                            throw new ArgumentException("--graphics directx is only supported on Windows. Use --graphics opengl for other platforms.");
                    }
                    else
                    {
                        graphics = (platform == Platform.Windows) ? GraphicsAPI.DirectX : GraphicsAPI.OpenGL;
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.Error.WriteLine("ERROR: " + ex.Message);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        TryFreeConsole();
                    return 3;
                }

                Config.Initialize();
                Config.SelectedPlatform = platform;
                Config.SelectedGraphics = graphics;

                // Initialize Avalonia platform services (needed for AssetLoader in headless mode)
                // without entering an event loop or creating any window.
                BuildAvaloniaApp().SetupWithoutStarting();

                Functions.HeadlessMode = true;
                Task.Run(() => Functions.StartPatching()).GetAwaiter().GetResult();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    TryFreeConsole();
                return Functions.ExitCode;
            }

            // GUI mode — Avalonia takes over from here.
            // Config.Initialize() is called in App.OnFrameworkInitializationCompleted().
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
