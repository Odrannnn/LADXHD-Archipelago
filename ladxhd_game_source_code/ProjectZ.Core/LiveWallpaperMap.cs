using System;
using System.Collections.Generic;
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
        private const int MaximumObjectTemplates = 4_096;
        private const int MaximumObjects = 100_000;
        private const int TileSize = 16;
        private readonly int[,,] _tiles;
        private readonly List<CollisionRectangle>[,] _collisionGrid;

        private LiveWallpaperMap(
            string tilesetPath, int width, int height, int depth, int[,,] tiles,
            List<CollisionRectangle>[,] collisionGrid,
            int collisionCount,
            int hazardCount)
        {
            TilesetPath = tilesetPath;
            Width = width;
            Height = height;
            Depth = depth;
            _tiles = tiles;
            _collisionGrid = collisionGrid;
            CollisionCount = collisionCount;
            HazardCount = hazardCount;
        }

        public string TilesetPath { get; }
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int DrawableDepth => Math.Max(1, Depth - 1);
        public int CollisionCount { get; }
        public int HazardCount { get; }

        public int GetTile(int x, int y, int layer)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height ||
                layer < 0 || layer >= Depth)
                return -1;
            return _tiles[x, y, layer];
        }

        /// <summary>
        /// Tests a pixel-space body rectangle against the collision and hole objects stored
        /// after the visual tile layers in the original map file.
        /// </summary>
        public bool IntersectsCollision(
            float x, float y, float width, float height, bool includeHoles)
        {
            if (width <= 0 || height <= 0)
                return false;
            if (x < 0 || y < 0 || x + width > Width * TileSize ||
                y + height > Height * TileSize)
                return true;
            if (_collisionGrid == null)
                return false;

            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            {
                for (var tileX = startX; tileX <= endX; tileX++)
                {
                    var entries = _collisionGrid[tileX, tileY];
                    if (entries == null)
                        continue;
                    foreach (var entry in entries)
                    {
                        if ((!entry.IsHole || includeHoles) && entry.Intersects(
                                x, y, width, height))
                            return true;
                    }
                }
            }
            return false;
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

            if (!TryLoadCollisionObjects(reader, width, height,
                    out var collisionGrid, out var collisionCount, out var hazardCount))
                return false;

            map = new LiveWallpaperMap(
                tilesetPath, width, height, depth, tiles,
                collisionGrid, collisionCount, hazardCount);
            return true;
        }

        private static bool TryLoadCollisionObjects(
            TextReader reader,
            int width,
            int height,
            out List<CollisionRectangle>[,] collisionGrid,
            out int collisionCount,
            out int hazardCount)
        {
            collisionGrid = null;
            collisionCount = 0;
            hazardCount = 0;
            var templateCountLine = reader.ReadLine();
            if (templateCountLine == null)
                return true;
            if (!int.TryParse(templateCountLine, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var templateCount) ||
                templateCount is < 0 or > MaximumObjectTemplates)
                return false;

            var templates = new string[templateCount];
            for (var index = 0; index < templateCount; index++)
            {
                templates[index] = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(templates[index]) ||
                    templates[index].Length > 256)
                    return false;
            }
            if (!TryReadInt(reader, out var objectCount) ||
                objectCount is < 0 or > MaximumObjects)
                return false;

            collisionGrid = new List<CollisionRectangle>[width, height];
            for (var index = 0; index < objectCount; index++)
            {
                var line = reader.ReadLine();
                var parts = line?.Split(';');
                if (parts == null || parts.Length < 3 ||
                    !int.TryParse(parts[0], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var templateIndex) ||
                    templateIndex < 0 || templateIndex >= templates.Length ||
                    !TryParseInt(parts[1], out var positionX) ||
                    !TryParseInt(parts[2], out var positionY))
                    return false;

                AddObjectCollision(
                    collisionGrid, width, height, templates[templateIndex], parts,
                    positionX, positionY, ref collisionCount, ref hazardCount);
            }
            return true;
        }

        private static void AddObjectCollision(
            List<CollisionRectangle>[,] grid,
            int mapWidth,
            int mapHeight,
            string template,
            string[] parts,
            int positionX,
            int positionY,
            ref int collisionCount,
            ref int hazardCount)
        {
            if (template is "hole" or "fullHole")
            {
                var fullTile = template == "fullHole";
                var width = GetOptionalPositiveInt(parts, 3, fullTile ? 16 : 14);
                var height = GetOptionalPositiveInt(parts, 4, fullTile ? 16 : 14);
                var offsetX = GetOptionalInt(parts, 6, fullTile ? 0 : 1);
                var offsetY = GetOptionalInt(parts, 7, fullTile ? 0 : 1);
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(positionX + offsetX, positionY + offsetY,
                        width, height, true), ref collisionCount, ref hazardCount);
                return;
            }

            LocalRectangle[] rectangles = template switch
            {
                "c1" or "lowCollider16" or "lowerLevelCollider" or "c1PushIgnore" =>
                    [new LocalRectangle(0, 0, 16, 16)],
                "c2" or "lowCollider0" or "lowerLevelCollider1" =>
                    [new LocalRectangle(0, 8, 16, 8)],
                "c5" or "lowCollider1" =>
                    [new LocalRectangle(0, 0, 16, 8)],
                "c3" or "lowCollider2" or "lowerLevelCollider2" =>
                    [new LocalRectangle(0, 0, 8, 16)],
                "c4" or "lowCollider3" =>
                    [new LocalRectangle(8, 0, 8, 16)],
                "c13" => [new LocalRectangle(0, 0, 8, 8)],
                "c6" => [new LocalRectangle(8, 0, 8, 8)],
                "c7" => [new LocalRectangle(0, 8, 8, 8)],
                "c8" => [new LocalRectangle(8, 8, 8, 8)],
                "colliderL0" or "c9" =>
                    [new LocalRectangle(0, 8, 8, 8), new LocalRectangle(0, 0, 16, 8)],
                "colliderL1" or "c10" =>
                    [new LocalRectangle(8, 8, 8, 8), new LocalRectangle(0, 0, 16, 8)],
                "colliderL2" or "c11" =>
                    [new LocalRectangle(0, 0, 8, 8), new LocalRectangle(0, 8, 16, 8)],
                "colliderL3" or "c12" =>
                    [new LocalRectangle(8, 0, 8, 8), new LocalRectangle(0, 8, 16, 8)],
                "blockDoor_Seg1" => [new LocalRectangle(0, 8, 7, 8)],
                "blockDoor_Seg2" => [new LocalRectangle(7, 9, 1, 7)],
                "blockDoor_Seg3" => [new LocalRectangle(8, 10, 8, 6)],
                "oneWayBridge2" => [new LocalRectangle(15, 0, 1, 16)],
                "oneWayBridge0" => [new LocalRectangle(0, 0, 1, 16)],
                "oneWayFlatTop" => [new LocalRectangle(0, 0, 16, 1)],
                "oneWayFlatTop-14" => [new LocalRectangle(1, 0, 14, 1)],
                _ => null
            };
            if (rectangles == null)
                return;
            foreach (var rectangle in rectangles)
            {
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(
                        positionX + rectangle.X, positionY + rectangle.Y,
                        rectangle.Width, rectangle.Height, false),
                    ref collisionCount, ref hazardCount);
            }
        }

        private static void AddCollision(
            List<CollisionRectangle>[,] grid,
            int mapWidth,
            int mapHeight,
            CollisionRectangle rectangle,
            ref int collisionCount,
            ref int hazardCount)
        {
            if (rectangle.Width <= 0 || rectangle.Height <= 0 ||
                rectangle.X + rectangle.Width <= 0 || rectangle.Y + rectangle.Height <= 0 ||
                rectangle.X >= mapWidth * TileSize || rectangle.Y >= mapHeight * TileSize)
                return;
            var startX = Math.Clamp(rectangle.X / TileSize, 0, mapWidth - 1);
            var startY = Math.Clamp(rectangle.Y / TileSize, 0, mapHeight - 1);
            var endX = Math.Clamp((rectangle.X + rectangle.Width - 1) / TileSize,
                0, mapWidth - 1);
            var endY = Math.Clamp((rectangle.Y + rectangle.Height - 1) / TileSize,
                0, mapHeight - 1);
            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    grid[x, y] ??= [];
                    grid[x, y].Add(rectangle);
                }
            }
            if (rectangle.IsHole)
                hazardCount++;
            else
                collisionCount++;
        }

        private static int GetOptionalPositiveInt(
            string[] parts, int index, int fallback)
        {
            var value = GetOptionalInt(parts, index, fallback);
            return value is > 0 and <= 512 ? value : fallback;
        }

        private static int GetOptionalInt(string[] parts, int index, int fallback) =>
            index < parts.Length && TryParseInt(parts[index], out var value)
                ? value
                : fallback;

        private static bool TryParseInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out result);

        private readonly struct LocalRectangle
        {
            public LocalRectangle(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
        }

        private readonly struct CollisionRectangle
        {
            public CollisionRectangle(int x, int y, int width, int height, bool isHole)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                IsHole = isHole;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
            public bool IsHole { get; }

            public bool Intersects(float x, float y, float width, float height) =>
                x < X + Width && x + width > X &&
                y < Y + Height && y + height > Y;
        }

        private static bool TryReadInt(TextReader reader, out int value) =>
            int.TryParse(reader.ReadLine(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
    }
}
