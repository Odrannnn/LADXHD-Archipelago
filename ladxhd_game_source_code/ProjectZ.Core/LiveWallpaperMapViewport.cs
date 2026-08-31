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
            float cameraOriginX, float cameraOriginY,
            int screenWidth, int screenHeight, bool centered = false)
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
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            _centered = centered;
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
        public int ScreenWidth { get; }
        public int ScreenHeight { get; }
        private readonly bool _centered;

        public LiveWallpaperMapViewport WithOrigin(
            int originX, int originY, int mapWidth, int mapHeight) =>
            WithCameraOrigin(originX, originY, mapWidth, mapHeight);

        public LiveWallpaperMapViewport WithCameraOrigin(
            float originX, float originY, int mapWidth, int mapHeight)
        {
            ClampCameraTarget(mapWidth, mapHeight, ref originX, ref originY);
            var cameraX = originX;
            var cameraY = originY;
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
                cameraX, cameraY, ScreenWidth, ScreenHeight, _centered);
        }

        public void ClampCameraTarget(int mapWidth, int mapHeight,
            ref float targetX, ref float targetY)
        {
            // Interior draw-cache overscan is not visible camera coverage. Using
            // Columns/Rows here makes the last two map tiles impossible to reveal.
            var visibleColumns = _centered && TileSize > 0 ? ScreenWidth / TileSize : Columns;
            var visibleRows = _centered && TileSize > 0 ? ScreenHeight / TileSize : Rows;
            targetX = Math.Clamp(targetX, 0f, Math.Max(0, mapWidth - visibleColumns));
            targetY = Math.Clamp(targetY, 0f, Math.Max(0, mapHeight - visibleRows));
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

            // Use the same projection as drawing and taps. Centred interiors,
            // parallax and fractional camera movement do not necessarily leave
            // one overscan tile at each screen edge.
            var screenX = Left + (linkPixelX / 16f - OriginX) * TileSize;
            var screenY = Top + (linkPixelY / 16f - OriginY) * TileSize;
            var horizontalEdgeInset = TileSize;
            var verticalEdgeInset = 2f * TileSize;
            if (movementX < -0.1f &&
                screenX <= horizontalEdgeInset)
                targetOriginX -= 10f;
            else if (movementX > 0.1f &&
                     screenX >= ScreenWidth - horizontalEdgeInset)
                targetOriginX += 10f;
            // A clamped horizontal edge must not suppress vertical following
            // when Link moves diagonally along an interior's outer wall.
            if (movementY < -0.1f &&
                     screenY <= verticalEdgeInset)
                targetOriginY -= 8f;
            else if (movementY > 0.1f &&
                     screenY >= ScreenHeight - verticalEdgeInset)
                targetOriginY += 8f;

            ClampCameraTarget(mapWidth, mapHeight, ref targetOriginX, ref targetOriginY);
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

            // Project the room through the same anchors used by drawing/taps.
            // Cache dimensions include overscan and cannot identify screen centre.
            var baseLeft = Left + (CameraOriginX - OriginX) * TileSize;
            var baseTop = Top + (CameraOriginY - OriginY) * TileSize;
            targetOriginX = roomOriginX + fieldColumns / 2f -
                (ScreenWidth / 2f - baseLeft) / TileSize;
            targetOriginY = roomOriginY + fieldRows / 2f -
                (ScreenHeight / 2f - baseTop) / TileSize;
            ClampCameraTarget(mapWidth, mapHeight, ref targetOriginX, ref targetOriginY);
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
                Math.Max(0, sceneX - horizontalSceneMargin), originY, width, height);
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
                centerPixelX / 16f - width / (2f * tileSize),
                0f, Math.Max(0, mapWidth - width / tileSize));
            var cameraY = Math.Clamp(
                centerPixelY / 16f - height / (2f * tileSize),
                0f, Math.Max(0, mapHeight - height / tileSize));
            var originX = (int)MathF.Floor(cameraX);
            var originY = (int)MathF.Floor(cameraY);
            // Keep a stable, map-bounded projection rather than anchoring every
            // redraw to the entrance. Small maps are centred as a whole. Larger
            // maps scroll to their actual edges; launcher parallax must not move
            // those bounds or crop an otherwise visible interior doorway.
            var left = Math.Max(0f, (width - mapWidth * tileSize) / 2f) -
                       (cameraX - originX) * tileSize;
            var top = Math.Max(0f, (height - mapHeight * tileSize) / 2f) -
                      (cameraY - originY) * tileSize;
            viewport = new LiveWallpaperMapViewport(
                originX, originY, columns, rows, tileSize,
                left,
                top,
                height * 0.5f,
                cameraX, cameraY, width, height, centered: true);
            return true;
        }
    }
}
