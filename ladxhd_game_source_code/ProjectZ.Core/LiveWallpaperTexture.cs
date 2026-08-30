using System;
using System.IO;

namespace ProjectZ
{
    public static class LiveWallpaperTexture
    {
        // The installed Android light textures are uncompressed XNB Color/RGBA.
        // Read those exact pixels without creating a MonoGame graphics device.
        // Other encodings are rejected, not replaced by a procedural gradient.
        public static bool TryReadXnb(Stream stream, out int width, out int height,
            out int[] argb)
        {
            width = height = 0;
            argb = null;
            try
            {
                var start = stream.CanSeek ? stream.Position : 0;
                using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
                if (reader.ReadByte() != 'X' || reader.ReadByte() != 'N' || reader.ReadByte() != 'B')
                    return false;
                reader.ReadByte(); // platform
                if (reader.ReadByte() != 5 || (reader.ReadByte() & 0xC0) != 0)
                    return false;
                var length = reader.ReadInt32();
                if (length < 10 || length > 16 * 1024 * 1024 ||
                    stream.CanSeek && length > stream.Length - start ||
                    reader.Read7BitEncodedInt() != 1 ||
                    !reader.ReadString().StartsWith("Microsoft.Xna.Framework.Content.Texture2DReader", StringComparison.Ordinal))
                    return false;
                reader.ReadInt32(); // type reader version
                if (reader.Read7BitEncodedInt() != 0 || reader.Read7BitEncodedInt() != 1 ||
                    reader.ReadInt32() != 0) // SurfaceFormat.Color
                    return false;
                var w = reader.ReadInt32();
                var h = reader.ReadInt32();
                var levels = reader.ReadInt32();
                if (w <= 0 || h <= 0 || w > 2048 || h > 2048 || levels < 1 || levels > 12 ||
                    reader.ReadInt32() != checked(w * h * 4))
                    return false;
                var result = new int[w * h];
                for (var i = 0; i < result.Length; i++)
                {
                    var r = reader.ReadByte();
                    var g = reader.ReadByte();
                    var b = reader.ReadByte();
                    var a = reader.ReadByte();
                    // XNB pixels are premultiplied; Bitmap.SetPixels expects
                    // straight ARGB and premultiplies them on upload.
                    int Straight(int channel) => a == 0 ? 0 : Math.Min(255, channel * 255 / a);
                    result[i] = a << 24 | Straight(r) << 16 | Straight(g) << 8 | Straight(b);
                }
                width = w;
                height = h;
                argb = result;
                return true;
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or OverflowException or FormatException)
            {
                return false;
            }
        }
    }
}
