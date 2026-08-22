using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static LADXHD_Migrater.Config;

namespace LADXHD_Migrater
{
    internal class DotNet
    {
        public async static Task<bool> BuildGame()
        {
            if (!Config.Game_Source.TestPath()) return false;

            if ((OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MGFXC_WINE_PATH")))
            {
                string url = OperatingSystem.IsMacOS()
                    ? "https://tinyurl.com/mgfxc-macos"
                    : "https://tinyurl.com/mgfxc-linux";
                await Functions.Notify("Wine Not Configured", $"MGFXC_WINE_PATH environment variable is not set. Shader compilation will fail.\n\nSee {url}");
                return false;
            }

            try
            {
                (string? buildFolder, string? csproj, string? rid, string? profile, bool wintarget, string? exe) = (Config.SelectedPlatform, Config.SelectedGraphics) switch
                {
                    (Platform.Windows, GraphicsAPI.DirectX11)  => ("Windows-DX11", "ProjectZ.WindowsDX11/ProjectZ.WindowsDX11.csproj", "win-x64",     "FolderProfile_Windows-DX11", true,  "Link's Awakening DX HD.exe"),
                    (Platform.Windows, GraphicsAPI.DirectX12)  => ("Windows-DX12", "ProjectZ.WindowsDX11/ProjectZ.WindowsDX12.csproj", "win-x64",     "FolderProfile_Windows-DX12", true,  "Link's Awakening DX HD.exe"),
                    (Platform.Windows, GraphicsAPI.OpenGL)     => ("Windows-GL",   "ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj",     "win-x64",     "FolderProfile_Windows-GL",   false, "Link's Awakening DX HD.exe"),
                    (Platform.Windows, GraphicsAPI.Vulkan)     => ("Windows-VK",   "ProjectZ.DesktopVK/ProjectZ.DesktopVK.csproj",     "win-x64",     "FolderProfile_Windows-VK",   false, "Link's Awakening DX HD.exe"),
                    (Platform.Android, _)                      => ("Android",      "ProjectZ.Android/ProjectZ.Android.csproj",         null,          "FolderProfile_Android",      false, "com.zelda.ladxhd.archipelago-Signed.apk"),
                    (Platform.Linux_x64, GraphicsAPI.OpenGL)   => ("Linux-x86_64", "ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj",     "linux-x64",   "FolderProfile_Linux-x86_64", false, "Link's Awakening DX HD"),
                    (Platform.Linux_x64, GraphicsAPI.Vulkan)   => ("Linux-x86_64", "ProjectZ.DesktopVK/ProjectZ.DesktopVK.csproj",     "linux-x64",   "FolderProfile_Linux-x86_64", false, "Link's Awakening DX HD"),
                    (Platform.Linux_Arm64, GraphicsAPI.OpenGL) => ("Linux-Arm64",  "ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj",     "linux-arm64", "FolderProfile_Linux-Arm64",  false, "Link's Awakening DX HD"),
                    (Platform.Linux_Arm64, GraphicsAPI.Vulkan) => ("Linux-Arm64",  "ProjectZ.DesktopVK/ProjectZ.DesktopVK.csproj",     "linux-arm64", "FolderProfile_Linux-Arm64",  false, "Link's Awakening DX HD"),
                    (Platform.MacOS_Arm64, GraphicsAPI.OpenGL) => ("MacOS-Arm64",  "ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj",     "osx-arm64",   "FolderProfile_MacOS-Arm64",  false, "Link's Awakening DX HD"),
                    (Platform.MacOS_Arm64, GraphicsAPI.Vulkan) => ("MacOS-Arm64",  "ProjectZ.DesktopVK/ProjectZ.DesktopVK.csproj",     "osx-arm64",   "FolderProfile_MacOS-Arm64",  false, "Link's Awakening DX HD"),
                    (Platform.MacOS_x64, GraphicsAPI.OpenGL)   => ("MacOS-x86_64", "ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj",     "osx-x64",     "FolderProfile_MacOS-x86_64", false, "Link's Awakening DX HD"),
                    (Platform.MacOS_x64, GraphicsAPI.Vulkan)   => ("MacOS-x86_64", "ProjectZ.DesktopVK/ProjectZ.DesktopVK.csproj",     "osx-x64",     "FolderProfile_MacOS-x86_64", false, "Link's Awakening DX HD"),
                    _                                          => (null, null, null, null, false, null)
                };

                if (buildFolder == null) return false;

                Config.Build_Path = Path.Combine(Config.Publish_Path, buildFolder);

                string args = $"publish {csproj} -c Release";
                if (!string.IsNullOrEmpty(rid)) args += $" -r {rid}";
                if (wintarget) args += $" -p:EnableWindowsTargeting=true";
                args += $" -p:PublishProfile={profile} --disable-build-servers -p:UsedAvaloniaProducts=";

                if (!await RunProcess("dotnet", args, "Build Error")) return false;

                string exePath = Path.Combine(Config.Build_Path, exe!);
                return exePath.TestPath();
            }
            catch (Exception ex)
            {
                await Functions.Notify("Exception Caught", "Exception: " + ex.Message, 10);
                return false;
            }
        }

        public async static Task<bool> BuildLauncher()
        {
            if (!Config.Launcher_Source.TestPath()) return false;

            (string rid,string profile) = Config.SelectedPlatform switch
            {
                Platform.Windows     => ("win-x64",     "Windows"),
                Platform.Linux_x64   => ("linux-x64",   "Linux-x64"),
                Platform.Linux_Arm64 => ("linux-arm64", "Linux-arm64"),
                Platform.MacOS_x64   => ("osx-x64",     "macOS-x64"),
                Platform.MacOS_Arm64 => ("osx-arm64",   "macOS-arm64"),
                _                    => ("", "")
            };

            if (string.IsNullOrEmpty(rid) || string.IsNullOrEmpty(profile))
                return true;

            string args = $"publish LADXHD_Launcher.csproj -r {rid} -p:PublishProfile={profile} --disable-build-servers -p:UsedAvaloniaProducts=";
            return await RunProcess("dotnet", args, "Launcher Build Error", Config.Launcher_Source);
        }

        private async static Task<bool> RunProcess(string executable, string arguments, string errorTitle, string workingDirectory = "")
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Config.Game_Source : workingDirectory,
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                if (Functions.HeadlessMode)
                {
                    process.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                    process.ErrorDataReceived  += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
                }

                process.Start();

                if (Functions.HeadlessMode)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        return false;
                }
                else
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        string message = string.IsNullOrWhiteSpace(error) ? output : error;
                        if (message.Length > 500)
                            message = message.Substring(message.Length - 500);
                        await Functions.Notify(errorTitle, message, 10);
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
