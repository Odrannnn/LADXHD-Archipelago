using System;

namespace ProjectZ
{
    public readonly struct LiveWallpaperMapViewport
    {
        // Show twelve rather than ten tiles across the short display dimension.
        // Both world and interior cameras share this orientation-stable zoom.
        public const int ReferenceVisibleTiles = 12;
        private LiveWallpaperMapViewport(
            int originX, int originY, int columns, int rows,
            float tileSize, float left, float top, float groundY,
            float cameraOriginX, float cameraOriginY)
        {
            OriginX = originX;
            OriginY = originY;
            Columns = columns;
            Rows = rows;
            TileSize = tileSize;
            Left = left;
            Top = top;
            GroundY = groundY;
            CameraOriginX = cameraOriginX;
            CameraOriginY = cameraOriginY;
        }

        public int OriginX { get; }
        public int OriginY { get; }
        public int Columns { get; }
        public int Rows { get; }
        public float TileSize { get; }
        public float Left { get; }
        public float Top { get; }
        public float GroundY { get; }
        public float CameraOriginX { get; }
        public float CameraOriginY { get; }

        public LiveWallpaperMapViewport WithOrigin(
            int originX, int originY, int mapWidth, int mapHeight) =>
            WithCameraOrigin(originX, originY, mapWidth, mapHeight);

        public LiveWallpaperMapViewport WithCameraOrigin(
            float originX, float originY, int mapWidth, int mapHeight)
        {
            var cameraX = Math.Clamp(
                originX, 0f, Math.Max(0, mapWidth - Columns));
            var cameraY = Math.Clamp(
                originY, 0f, Math.Max(0, mapHeight - Rows));
            var tileOriginX = (int)MathF.Floor(cameraX);
            var tileOriginY = (int)MathF.Floor(cameraY);
            // Recover the unshifted drawing anchors so repeated fractional updates
            // do not accumulate rounding error.
            var baseLeft = Left + (CameraOriginX - OriginX) * TileSize;
            var baseTop = Top + (CameraOriginY - OriginY) * TileSize;
            var baseGroundY = GroundY + (CameraOriginY - OriginY) * TileSize;
            return new LiveWallpaperMapViewport(
                tileOriginX, tileOriginY, Columns, Rows, TileSize,
                baseLeft - (cameraX - tileOriginX) * TileSize,
                baseTop - (cameraY - tileOriginY) * TileSize,
                baseGroundY - (cameraY - tileOriginY) * TileSize,
                cameraX, cameraY);
        }

        public bool TryMoveToAdjacentField(
            int horizontalDirection,
            int verticalDirection,
            int mapWidth,
            int mapHeight,
            out LiveWallpaperMapViewport viewport)
        {
            viewport = this;
            if (Columns <= 0 || Rows <= 0 || mapWidth <= 0 || mapHeight <= 0 ||
                horizontalDirection != 0 && verticalDirection != 0)
                return false;

            // Values.FieldWidth and Values.FieldHeight are 160x128: the original
            // overworld loading-zone field is exactly 10x8 map tiles.
            var originX = Math.Clamp(
                OriginX + Math.Clamp(horizontalDirection, -1, 1) * 10,
                0, Math.Max(0, mapWidth - Columns));
            var originY = Math.Clamp(
                OriginY + Math.Clamp(verticalDirection, -1, 1) * 8,
                0, Math.Max(0, mapHeight - Rows));
            if (originX == OriginX && originY == OriginY)
                return false;

            viewport = WithOrigin(originX, originY, mapWidth, mapHeight);
            return true;
        }

        public bool TryFollowLinkThroughExit(
            float linkPixelX,
            float linkPixelY,
            int mapWidth,
            int mapHeight,
            out LiveWallpaperMapViewport viewport)
        {
            viewport = this;
            if (Columns <= 0 || Rows <= 0 || mapWidth <= 0 || mapHeight <= 0)
                return false;

            // A phone wallpaper is much taller than the original 160x128 field.
            // Recenter the new crop on Link so following a field transition visibly
            // moves the whole wallpaper instead of retaining most of the old crop.
            var centeredOriginX = (int)MathF.Floor(
                linkPixelX / 16f - Columns / 2f);
            var centeredOriginY = (int)MathF.Floor(
                linkPixelY / 16f - Rows / 2f);
            viewport = WithOrigin(
                centeredOriginX, centeredOriginY, mapWidth, mapHeight);
            return viewport.OriginX != OriginX || viewport.OriginY != OriginY;
        }

        public bool TryGetEdgeScrollTarget(
            float linkPixelX,
            float linkPixelY,
            float movementX,
            float movementY,
            int mapWidth,
            int mapHeight,
            out float targetOriginX,
            out float targetOriginY)
        {
            targetOriginX = CameraOriginX;
            targetOriginY = CameraOriginY;
            if (Columns <= 0 || Rows <= 0 || mapWidth <= 0 || mapHeight <= 0)
                return false;

            var linkTileX = linkPixelX / 16f;
            var linkTileY = linkPixelY / 16f;
            // Columns/Rows include one overscan tile beyond each phone edge.
            // Keep one visible tile of horizontal notice. Vertically, begin two
            // visible tiles early so Link remains reachable above the navigation
            // bar and below the status bar.
            const float horizontalEdgeInset = 2f;
            const float verticalEdgeInset = 3f;
            if (movementX < -0.1f &&
                linkTileX <= CameraOriginX + horizontalEdgeInset)
                targetOriginX -= 10f;
            else if (movementX > 0.1f &&
                     linkTileX >= CameraOriginX + Columns - horizontalEdgeInset)
                targetOriginX += 10f;
            else if (movementY < -0.1f &&
                     linkTileY <= CameraOriginY + verticalEdgeInset)
                targetOriginY -= 8f;
            else if (movementY > 0.1f &&
                     linkTileY >= CameraOriginY + Rows - verticalEdgeInset)
                targetOriginY += 8f;

            targetOriginX = Math.Clamp(
                targetOriginX, 0f, Math.Max(0, mapWidth - Columns));
            targetOriginY = Math.Clamp(
                targetOriginY, 0f, Math.Max(0, mapHeight - Rows));
            return MathF.Abs(targetOriginX - CameraOriginX) > 0.001f ||
                   MathF.Abs(targetOriginY - CameraOriginY) > 0.001f;
        }

        public bool TryGetRoomScrollTarget(
            float linkPixelX,
            float linkPixelY,
            int mapOffsetX,
            int mapOffsetY,
            int mapWidth,
            int mapHeight,
            out float targetOriginX,
            out float targetOriginY)
        {
            targetOriginX = CameraOriginX;
            targetOriginY = CameraOriginY;
            if (Columns <= 0 || Rows <= 0 || mapWidth <= 0 || mapHeight <= 0)
                return false;

            const int fieldColumns = 10;
            const int fieldRows = 8;
            var linkTileX = linkPixelX / 16f;
            var linkTileY = linkPixelY / 16f;
            var roomOriginX = MathF.Floor(
                (linkTileX - mapOffsetX) / fieldColumns) * fieldColumns +
                mapOffsetX;
            var roomOriginY = MathF.Floor(
                (linkTileY - mapOffsetY) / fieldRows) * fieldRows +
                mapOffsetY;

            // Dungeon cameras are room-based in the game. Centre the phone crop
            // on that room, while retaining the larger portrait view around it.
            targetOriginX = Math.Clamp(
                roomOriginX + fieldColumns / 2f - Columns / 2f,
                0f, Math.Max(0, mapWidth - Columns));
            targetOriginY = Math.Clamp(
                roomOriginY + fieldRows / 2f - Rows / 2f,
                0f, Math.Max(0, mapHeight - Rows));
            return MathF.Abs(targetOriginX - CameraOriginX) > 0.001f ||
                   MathF.Abs(targetOriginY - CameraOriginY) > 0.001f;
        }

        public static bool TryCreate(
            int width, int height, int mapHeight, int scene, float xOffset,
            out LiveWallpaperMapViewport viewport)
        {
            viewport = default;
            if (width <= 0 || height <= 0 || mapHeight <= 0 ||
                !LiveWallpaperSceneSelection.TryGetTileOrigin(scene, out var sceneX, out var sceneY))
                return false;

            const int referenceVisibleTiles = ReferenceVisibleTiles;
            const int horizontalOverscan = 2;
            // Keep the same physical map scale when the device rotates. Using
            // width here made landscape tiles more than twice as large; the
            // shorter display dimension is stable across portrait/landscape.
            var tileSize = MathF.Ceiling(
                Math.Min(width, height) / (float)referenceVisibleTiles);
            var visibleColumns = (int)MathF.Ceiling(width / tileSize);
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

            var horizontalSceneMargin = Math.Max(1, (columns - 10) / 2);
            viewport = new LiveWallpaperMapViewport(
                Math.Max(0, sceneX - horizontalSceneMargin), originY, columns, rows,
                tileSize, left, top, groundY,
                Math.Max(0, sceneX - horizontalSceneMargin), originY);
            return true;
        }

        public static bool TryCreateCentered(
            int width, int height, int mapWidth, int mapHeight,
            float centerPixelX, float centerPixelY, float xOffset,
            out LiveWallpaperMapViewport viewport)
        {
            viewport = default;
            if (width <= 0 || height <= 0 || mapWidth <= 0 || mapHeight <= 0)
                return false;

            const int referenceVisibleTiles = ReferenceVisibleTiles;
            const int horizontalOverscan = 2;
            var tileSize = MathF.Ceiling(
                Math.Min(width, height) / (float)referenceVisibleTiles);
            var visibleColumns = (int)MathF.Ceiling(width / tileSize);
            var columns = visibleColumns + horizontalOverscan;
            var rows = (int)MathF.Ceiling(height / tileSize) + 2;
            var cameraX = Math.Clamp(
                centerPixelX / 16f - columns / 2f,
                0f, Math.Max(0, mapWidth - columns));
            var cameraY = Math.Clamp(
                centerPixelY / 16f - rows / 2f,
                0f, Math.Max(0, mapHeight - rows));
            var originX = (int)MathF.Floor(cameraX);
            var originY = (int)MathF.Floor(cameraY);
            // Interior maps can be smaller than the tall wallpaper viewport. A
            // clamped camera origin alone would leave their real entry door near
            // an edge. Anchor the requested map position at screen centre, then
            // retain only the wallpaper's small horizontal parallax adjustment.
            var left = width * 0.5f -
                       (centerPixelX / 16f - originX) * tileSize +
                       (0.5f - Math.Clamp(xOffset, 0f, 1f)) * tileSize;
            var top = height * 0.5f -
                      (centerPixelY / 16f - originY) * tileSize;
            viewport = new LiveWallpaperMapViewport(
                originX, originY, columns, rows, tileSize,
                left,
                top,
                height * 0.5f,
                cameraX, cameraY);
            return true;
        }
    }
}
