using System;

namespace ProjectZ
{
    public readonly struct LiveWallpaperMapViewport
    {
        private LiveWallpaperMapViewport(
            int originX, int originY, int columns, int rows,
            float tileSize, float left, float top, float groundY)
        {
            OriginX = originX;
            OriginY = originY;
            Columns = columns;
            Rows = rows;
            TileSize = tileSize;
            Left = left;
            Top = top;
            GroundY = groundY;
        }

        public int OriginX { get; }
        public int OriginY { get; }
        public int Columns { get; }
        public int Rows { get; }
        public float TileSize { get; }
        public float Left { get; }
        public float Top { get; }
        public float GroundY { get; }

        public static bool TryCreate(
            int width, int height, int mapHeight, int scene, float xOffset,
            out LiveWallpaperMapViewport viewport)
        {
            viewport = default;
            if (width <= 0 || height <= 0 || mapHeight <= 0 ||
                !LiveWallpaperSceneSelection.TryGetTileOrigin(scene, out var sceneX, out var sceneY))
                return false;

            const int visibleColumns = 10;
            const int horizontalOverscan = 2;
            var tileSize = MathF.Ceiling(width / (float)visibleColumns);
            var columns = visibleColumns + horizontalOverscan;
            var rows = (int)MathF.Ceiling(height / tileSize) + 2;
            var top = (height - rows * tileSize) * 0.5f;
            var layout = LiveWallpaperSceneLayouts.Resolve(scene);
            var desiredGroundY = height * 0.72f;
            var groundMapY = sceneY + layout.GroundTileRow;
            var originY = (int)MathF.Round(
                groundMapY - (desiredGroundY - top) / tileSize);
            originY = Math.Clamp(originY, 0, Math.Max(0, mapHeight - rows));
            var left = -tileSize + (0.5f - Math.Clamp(xOffset, 0f, 1f)) * tileSize;
            var groundY = top + (groundMapY - originY) * tileSize;

            viewport = new LiveWallpaperMapViewport(
                Math.Max(0, sceneX - 1), originY, columns, rows,
                tileSize, left, top, groundY);
            return true;
        }
    }
}
