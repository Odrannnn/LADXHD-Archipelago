using System;
using System.IO;
using LADXHD_Launcher;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

#if WINDOWS
using System.Media;
#endif

public static class SoundPlayer
{
    public static string SoundSave;
    public static string SoundXSave;
    public static string SoundOpen;
    public static string SoundClose;
    public static string SoundClick;
    public static string SoundSelect;
    public static string SoundReset;

    public static bool Enabled { get; set; } = true;
    public static bool SuppressSound { get; set; } = false;

    private static readonly Dictionary<string, string> _wavCache = new();

    public static byte[] LoadWavFromXnb(string xnbPath)
    {
        byte[] data = File.ReadAllBytes(xnbPath);
        using var reader = new BinaryReader(new MemoryStream(data));

        reader.BaseStream.Seek(10, SeekOrigin.Begin);
        Read7BitInt(reader);
        int nameLen = reader.ReadByte();
        reader.BaseStream.Seek(nameLen, SeekOrigin.Current);
        reader.ReadInt32();
        Read7BitInt(reader);
        Read7BitInt(reader);

        int   formatSize     = reader.ReadInt32();
        short formatTag      = reader.ReadInt16();
        short channels       = reader.ReadInt16();
        int   sampleRate     = reader.ReadInt32();
        int   avgBytesPerSec = reader.ReadInt32();
        short blockAlign     = reader.ReadInt16();
        short bitsPerSample  = reader.ReadInt16();

        if (formatSize > 16)
            reader.BaseStream.Seek(formatSize - 16, SeekOrigin.Current);

        int    dataSize = reader.ReadInt32();
        byte[] pcmData  = reader.ReadBytes(dataSize);

        using var ms = new MemoryStream();
        using var w  = new BinaryWriter(ms);

        w.Write("RIFF"u8); w.Write(36 + pcmData.Length);
        w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1);
        w.Write(channels); w.Write(sampleRate);
        w.Write(sampleRate * channels * (bitsPerSample / 8));
        w.Write(blockAlign); w.Write(bitsPerSample);
        w.Write("data"u8); w.Write(pcmData.Length);
        w.Write(pcmData);
        w.Flush();

        return ms.ToArray();
    }

    private static int Read7BitInt(BinaryReader reader)
    {
        int result = 0, shift = 0;
        byte b;
        do
        {
            b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return result;
    }

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

    private static void PlayTempWav(string tmpPath, bool deletAfter = false)
    {
        try
        {
        #if WINDOWS
            using var sp = new System.Media.SoundPlayer(tmpPath);
            sp.Play();
        #elif LINUX
            string[] players = { "paplay", "aplay", "ffplay" };
            foreach (var player in players)
            {
                if (Which(player) == null) continue;
                string args = player == "ffplay"
                    ? $"-nodisp -autoexit \"{tmpPath}\""
                    : $"\"{tmpPath}\"";
                Process.Start(new ProcessStartInfo(player, args) { CreateNoWindow = true });
                break;
            }
        #elif MACOS
            Process.Start(new ProcessStartInfo("afplay", $"\"{tmpPath}\"") { CreateNoWindow = true });
        #endif
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); }
        finally
        {
            if (deletAfter)
                Task.Delay(3000).ContinueWith(_ => { try { File.Delete(tmpPath); } catch { } });
        }
    }

    public static void PlayXnbSound(string xnbPath)
    {
        if (!Enabled || SuppressSound || !File.Exists(xnbPath)) return;
        if (!_wavCache.TryGetValue(xnbPath, out string tmpPath)) return;
        PlayTempWav(tmpPath);
    }

    public static void PlayWAVSound(string wavPath)
    {
        if (!Enabled || SuppressSound) return;
        try
        {
            var uri = new Uri(wavPath);
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            string tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
            using (var fs = File.Create(tmp))
                stream.CopyTo(fs);
            PlayTempWav(tmp, deletAfter: true);
        }
        catch (Exception ex) { Debug.WriteLine(ex.Message); }
    }

    public static void Initialize()
    {
        SoundSave   = Path.Combine(Config.RootPath, "Content", "SoundEffects", "D360-01-01.xnb");
        SoundXSave  = Path.Combine(Config.RootPath, "Content", "SoundEffects", "D360-35-23.xnb");
        SoundOpen   = Path.Combine(Config.RootPath, "Content", "SoundEffects", "D360-17-11.xnb");
        SoundClose  = Path.Combine(Config.RootPath, "Content", "SoundEffects", "D360-18-12.xnb");
        SoundClick  = Path.Combine(Config.RootPath, "Content", "SoundEffects", "D360-10-0A.xnb");
        SoundSelect = Path.Combine(Config.RootPath, "Content", "SoundEffects", "D360-19-13.xnb");
        SoundReset  = Path.Combine(Config.RootPath, "Content", "SoundEffects", "D360-27-1B.xnb");

        Task.Run(() =>
        {
            foreach (var path in new[] { SoundSave, SoundXSave, SoundOpen, SoundClose, SoundClick, SoundSelect, SoundReset })
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
                    File.WriteAllBytes(tmpPath, LoadWavFromXnb(path));
                    _wavCache[path] = tmpPath;
                }
                catch (Exception ex) { Debug.WriteLine(ex.Message); }
            }
        });
    }
}