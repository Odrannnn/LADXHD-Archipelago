using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;

#if WINDOWS
using System.Media;
#endif

namespace LADXHD_Patcher
{
    internal class SoundPlayer
    {
        private static string? Which(string name)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo("which", name)
                {
                    CreateNoWindow        = true,
                    RedirectStandardOutput = true
                });
                string? result = p?.StandardOutput.ReadLine();
                p?.WaitForExit();
                return string.IsNullOrWhiteSpace(result) ? null : result;
            }
            catch { return null; }
        }

        public static void Play(string resourcePath)
        {
            try
            {
                // The parameter "resourcePath" takes an avalonia resource.
                // Example: "avares://Patcher/Resources/success.wav"
                var uri = new Uri(resourcePath);
                using var stream = AssetLoader.Open(uri);

            #if WINDOWS
                using var soundPlayer = new System.Media.SoundPlayer(stream);
                soundPlayer.Play();

            #elif LINUX || MACOS
                // Extract the file to a temp file.
                string tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
                try
                {
                    using (var fs = File.Create(tmp))
                        stream.CopyTo(fs);

                #if LINUX
                    string[] players = { "paplay", "aplay", "ffplay" };
                    foreach (var player in players)
                    {
                        if (Which(player) == null) continue;
                        string args = player == "ffplay" 
                            ? $"-nodisp -autoexit \"{tmp}\"" 
                            : $"\"{tmp}\"";
                        Process.Start(new ProcessStartInfo(player, args) { CreateNoWindow = true });
                        break;
                    }
                #elif MACOS
                    Process.Start(new ProcessStartInfo("afplay", $"\"{tmp}\"") { CreateNoWindow = true });
                #endif
                }
                finally
                {
                    // Wait for a moment before deleting the sound effect.
                    Task.Delay(3000).ContinueWith(_ => { try { File.Delete(tmp); } catch { } });
                }
            #endif
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
        }
    }
}
