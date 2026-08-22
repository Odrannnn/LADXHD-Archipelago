using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LADXHD_Launcher
{
    internal class Gitlab
    {
        private static readonly HttpClient _http = new();

        private const string versionUrl = "https://gitlab.com/api/v4/projects/bighead.0%2Fladxhd_updated/releases/permalink/latest";
        private const string patchesUrl = "https://gitlab.com/bighead.0/ladxhd_updated/-/raw/main/ladxhd_patcher_source_code/Resources/";

        public static async Task<string?> GetLatestTagAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, versionUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("LADXHD-Launcher", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("tag_name").GetString();
        }

        public static string GetPatchesZipName()
        {
            #if WINDOWS
                bool isOpenGL = File.Exists(Path.Combine(Config.RootPath, "SDL2.dll"));
                return isOpenGL ? "patches_win_gl.zip" : "patches_win_dx.zip";
            #elif LINUX
                bool isArm64 = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
                return isArm64 ? "patches_linux_arm64.zip" : "patches_linux_x86.zip";
            #elif MACOS
                bool isArm64 = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
                return isArm64 ? "patches_macos_arm64.zip" : "patches_macos_x86.zip";
            #else
                return "";
            #endif
        }

        public static string GetLauncherZipName()
        {
            #if WINDOWS
                return "launcher_windows.zip";
            #elif LINUX
                bool isArm64 = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
                return isArm64 ? "launcher_linux_arm64.zip" : "launcher_linux_x86.zip";
            #elif MACOS
                bool isArm64 = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
                return isArm64 ? "launcher_macos_arm64.zip" : "launcher_macos_x86.zip";
            #else
                return "";
            #endif
        }

        public static async Task DownloadFileAsync(string fileName, string destinationPath, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            string url = patchesUrl + fileName;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("LADXHD-Launcher", "1.0"));

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream  = File.Create(destinationPath);

            var buffer = new byte[81920];
            long totalRead = 0;
            int  bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes.HasValue)
                {
                    int percent = (int)(totalRead * 100L / totalBytes.Value);
                    progress.Report(percent);
                }
            }
        }
    }
}