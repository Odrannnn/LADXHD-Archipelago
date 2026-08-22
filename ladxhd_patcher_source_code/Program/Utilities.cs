using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace LADXHD_Patcher
{
    internal class Resources
    {
        public static byte[] GetBytes(string resName)
        {
            var uri = new Uri($"avares://Patcher/Resources/{resName}");
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

        public static void ExtractResourcesZip(string zipName, string destination)
        {
            // All zip files are temporarily written to the temp folder.
            string zipPath = Path.Combine(Config.TempFolder, zipName);
            File.WriteAllBytes(zipPath, Resources.GetBytes(zipName));

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
                if (proc.ExitCode != 0)
                    throw new Exception(startInfo.FileName + ":\nOUTPUT:\n" + output + "\nERRORS:\n" + errors);
            }
        }
    }
}