using System;
using System.IO;
using Avalonia.Platform;

namespace LADXHD_Launcher
{
    internal class Resources
    {
        public static byte[] GetBytes(string resName)
        {
            var uri = new Uri($"avares://Launcher/Resources/{resName}");
            using var stream = AssetLoader.Open(uri);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}