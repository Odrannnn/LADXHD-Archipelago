using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace LADXHD_Patcher_Lite
{
    internal class Resources
    {
        public static byte[] GetBytes(string resName)
        {
            var uri = new Uri($"avares://Patcher-Lite/Resources/{resName}");
            using var stream = AssetLoader.Open(uri);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }

    public class Utilities
    {
        public static string GetAdvancedPath(string gameDirectory)
        {
            // If a "portable.txt" exists try to load from the game directory.
            string portable = Path.Combine(gameDirectory, "portable.txt");
            if (File.Exists(portable))
                return Path.Combine(gameDirectory, "advanced");

            // Check the "AppData\Local\Zelda_LA" path where save files are located.
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "Zelda_LA", "advanced");
        }

        public static async Task DownloadAndExtractFile(string zipName, string destination)
        {
            var progress = await ProgressWindow.ShowAsync("Downloading", "Downloading \"" + zipName + "\"...");
            try
            {
                // Set up the paths and download the file.
                string tempZipFile = Path.Combine(Config.TempFolder, zipName);
                var downloadProg = new Progress<int>(v => progress.UpdateProgressBar(v));
                await Gitlab.DownloadFileAsync(zipName, tempZipFile, downloadProg);

                // When it's a zip file extract it to the destination.
                if (zipName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    ExtractZipFile(tempZipFile, destination);

                // When it's a normal file, move it to the destination.
                else
                    tempZipFile.MovePath(destination);
            }
            finally
            {
                progress.CloseWindow();
            }
        }

        public static void ExtractZipFile(string zipName, string destination)
        {
            // All zip files are temporarily written to the temp folder.
            string zipPath = Path.Combine(Config.TempFolder, zipName);

            // Because .NET Framework 4.8 can not ovewrite files with ExtractToDirectory we do it manually.
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                // Loop through the entires in the archive.
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Set the path to the extracted file.
                    string entryPath = Path.Combine(destination, entry.FullName);

                    // It's a directory entry.
                    if (string.IsNullOrEmpty(entry.Name))
                        Directory.CreateDirectory(entryPath);

                    // Ensure the directory exists and extract, overwriting the file if present.
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(entryPath));
                        entry.ExtractToFile(entryPath, overwrite: true);
                    }
                }
            }
            // Remove the zip file after we are done.
            zipPath.RemovePath();
        }

        public static void RunProcess(string fileName, string workingDir, List<string> args)
        {
            string escapedArgs = string.Join(" ", System.Linq.Enumerable.Select(args, arg =>
                "\"" + arg.Replace("\"", "\\\"") + "\""));

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = escapedArgs,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var proc = new Process { StartInfo = startInfo })
            {
                proc.Start();
                string output = null, errors = null;
                var outTask = Task.Run(() => output = proc.StandardOutput.ReadToEnd());
                var errTask = Task.Run(() => errors = proc.StandardError.ReadToEnd());
                proc.WaitForExit();
                Task.WaitAll(outTask, errTask);

                // Always log this call, so it survives even if the failure dialog times out unread.
                string logPath = Path.Combine(Config.BaseFolder, "process_log.txt");
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:HH:mm:ss}] {fileName} {escapedArgs}\n" +
                    $"EXIT CODE: {proc.ExitCode}\nOUTPUT:\n{output}\nERRORS:\n{errors}\n\n");

                if (proc.ExitCode != 0)
                    throw new Exception(startInfo.FileName + ":\nOUTPUT:\n" + output + "\nERRORS:\n" + errors);
            }
        }
    }
}