using System;
using System.Globalization;
using System.IO;

namespace ProjectZ
{
    public readonly struct LiveWallpaperAtlasEntry
    {
        public LiveWallpaperAtlasEntry(
            int x, int y, int width, int height, float originX, float originY)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            OriginX = originX;
            OriginY = originY;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public float OriginX { get; }
        public float OriginY { get; }
    }

    public static class LiveWallpaperAtlas
    {
        private const int MaximumCoordinate = 16_384;

        public static bool TryLoad(
            TextReader reader, string spriteId, out LiveWallpaperAtlasEntry entry)
        {
            entry = default;
            if (reader == null || string.IsNullOrWhiteSpace(spriteId) ||
                !int.TryParse(reader.ReadLine(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var version) || version != 1 ||
                !int.TryParse(reader.ReadLine(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var scale) || scale <= 0)
                return false;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var colon = line.IndexOf(':');
                if (colon <= 0 || !string.Equals(line[..colon].Trim(), spriteId,
                        StringComparison.OrdinalIgnoreCase))
                    continue;
                var values = line[(colon + 1)..].Split(',');
                if (values.Length < 4 ||
                    !TryInt(values[0], out var x) || !TryInt(values[1], out var y) ||
                    !TryInt(values[2], out var width) || !TryInt(values[3], out var height) ||
                    x < 0 || y < 0 || width <= 0 || height <= 0 ||
                    x > MaximumCoordinate || y > MaximumCoordinate ||
                    width > MaximumCoordinate || height > MaximumCoordinate)
                    return false;
                var originX = values.Length >= 6 && TryFloat(values[4], out var parsedX)
                    ? parsedX
                    : 0f;
                var originY = values.Length >= 6 && TryFloat(values[5], out var parsedY)
                    ? parsedY
                    : 0f;
                entry = new LiveWallpaperAtlasEntry(
                    x * scale, y * scale, width * scale, height * scale,
                    originX * scale, originY * scale);
                return true;
            }
            return false;
        }

        private static bool TryInt(string value, out int parsed) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

        private static bool TryFloat(string value, out float parsed) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }
}
