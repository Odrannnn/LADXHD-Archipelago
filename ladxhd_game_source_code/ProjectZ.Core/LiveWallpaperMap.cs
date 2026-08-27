using System;
using System.Globalization;
using System.IO;

namespace ProjectZ
{
    public sealed class LiveWallpaperMap
    {
        private const int MaximumWidth = 512;
        private const int MaximumHeight = 512;
        private const int MaximumDepth = 8;
        private const int MaximumTileIndex = 1_000_000;
        private readonly int[,,] _tiles;

        private LiveWallpaperMap(string tilesetPath, int width, int height, int depth, int[,,] tiles)
        {
            TilesetPath = tilesetPath;
            Width = width;
            Height = height;
            Depth = depth;
            _tiles = tiles;
        }

        public string TilesetPath { get; }
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int DrawableDepth => Math.Max(1, Depth - 1);

        public int GetTile(int x, int y, int layer)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height ||
                layer < 0 || layer >= Depth)
                return -1;
            return _tiles[x, y, layer];
        }

        public static bool TryLoad(TextReader reader, out LiveWallpaperMap map)
        {
            map = null;
            if (reader == null || !TryReadInt(reader, out var version) || version is < 1 or > 3)
                return false;

            if (version > 2 && (!TryReadInt(reader, out _) || !TryReadInt(reader, out _)))
                return false;

            var tilesetPath = reader.ReadLine()?.Trim();
            if (!LiveWallpaperAnimation.TryNormalizeRelativePath(tilesetPath, out tilesetPath) ||
                !tilesetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!TryReadInt(reader, out var width) || width is <= 0 or > MaximumWidth ||
                !TryReadInt(reader, out var height) || height is <= 0 or > MaximumHeight ||
                !TryReadInt(reader, out var depth) || depth is <= 0 or > MaximumDepth)
                return false;

            var tiles = new int[width, height, depth];
            for (var layer = 0; layer < depth; layer++)
            {
                for (var y = 0; y < height; y++)
                {
                    var values = reader.ReadLine()?.Split(',');
                    if (values == null || values.Length < width)
                        return false;
                    for (var x = 0; x < width; x++)
                    {
                        if (string.IsNullOrWhiteSpace(values[x]))
                        {
                            tiles[x, y, layer] = -1;
                            continue;
                        }
                        if (!int.TryParse(values[x], NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out var tile) ||
                            tile is < 0 or > MaximumTileIndex)
                            return false;
                        tiles[x, y, layer] = tile;
                    }
                }
            }

            map = new LiveWallpaperMap(tilesetPath, width, height, depth, tiles);
            return true;
        }

        private static bool TryReadInt(TextReader reader, out int value) =>
            int.TryParse(reader.ReadLine(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
    }
}
