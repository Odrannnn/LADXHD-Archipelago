using ProjectZ;

internal static class WallpaperCameraResizeTests
{
    public static void Run()
    {
        const int mapWidth = 160, mapHeight = 128;
        foreach (var (width, height) in new[] { (1840, 2944), (1600, 2560), (1200, 2608), (1080, 2400) })
        {
            LiveWallpaperMapViewport.TryCreateCentered(width, height, mapWidth, mapHeight,
                1200, 900, 0.5f, out var portrait);
            LiveWallpaperMapViewport.TryCreateCentered(height, width, mapWidth, mapHeight,
                1200, 900, 0.5f, out var landscape);
            Check(landscape.Columns > portrait.Columns && landscape.Rows < portrait.Rows,
                "Fixture must change both camera bounds on rotation.");
            CheckRotation(portrait, landscape, horizontal: true);
            CheckRotation(landscape, portrait, horizontal: false);

            float x = 20.25f, y = 30.5f;
            landscape.ClampCameraTarget(mapWidth, mapHeight, ref x, ref y);
            Check(x == 20.25f && y == 30.5f,
                "In-bounds scroll targets must retain their fractional position.");
            landscape.ClampCameraTarget(10, 8, ref x, ref y);
            Check(x == 0 && y == 0, "Small interiors must retain the existing zero-bound behavior.");

            CheckScreenEdges(width, height);
            CheckScreenEdges(height, width);
        }

        static void CheckRotation(LiveWallpaperMapViewport before,
            LiveWallpaperMapViewport after, bool horizontal)
        {
            var x = horizontal ? mapWidth - before.Columns : 30f;
            var y = horizontal ? 30f : mapHeight - before.Rows;
            var resized = after.WithCameraOrigin(x, y, mapWidth, mapHeight);
            Check(resized.CameraOriginX != x || resized.CameraOriginY != y,
                "Old target must be unreachable after resizing (the previous stuck-camera case).");
            resized.ClampCameraTarget(mapWidth, mapHeight, ref x, ref y);
            Check(resized.CameraOriginX == x && resized.CameraOriginY == y,
                "Constrained targets must let the camera finish its old scroll.");
            var screenX = horizontal ? resized.ScreenWidth / 2f :
                resized.ScreenWidth - resized.TileSize * .5f;
            var screenY = horizontal ? resized.ScreenHeight - resized.TileSize :
                resized.ScreenHeight / 2f;
            var linkX = (resized.OriginX + (screenX - resized.Left) / resized.TileSize) * 16f;
            var linkY = (resized.OriginY + (screenY - resized.Top) / resized.TileSize) * 16f;
            Check(resized.TryGetEdgeScrollTarget(linkX, linkY, horizontal ? 0 : 1,
                    horizontal ? 1 : 0, mapWidth, mapHeight, out var nextX, out var nextY) &&
                (nextX != x || nextY != y),
                "Finishing the resized target must allow a new edge scroll on the other axis.");
            after.ClampCameraTarget(mapWidth, mapHeight, ref nextX, ref nextY);
            var reached = after.WithCameraOrigin(nextX, nextY, mapWidth, mapHeight);
            Check(reached.CameraOriginX == nextX && reached.CameraOriginY == nextY,
                "The next target must remain reachable with the resized viewport.");
        }
    }

    private static void CheckScreenEdges(int width, int height)
    {
        foreach (var parallax in new[] { 0f, .5f, 1f })
        {
            // A cave entered near the map's left/top edge has a centred drawing
            // anchor, not the overworld's -one-tile anchor. Its visible right or
            // bottom edge used to be reached before the camera's threshold.
            LiveWallpaperMapViewport.TryCreateCentered(width, height, 160, 128,
                16, 16, parallax, out var centred);
            LiveWallpaperMapViewport.TryCreate(width, height, 128, 1, parallax,
                out var overworld);
            foreach (var original in new[] { centred, overworld })
            {
                var viewport = original.WithCameraOrigin(30.25f, 40.5f, 160, 128);
                Check(viewport.ScreenWidth == width && viewport.ScreenHeight == height,
                    "Scrolling must preserve the actual screen dimensions.");
                foreach (var direction in new[] { 0, 1, 2, 3 })
                {
                    var dx = direction == 0 ? -1 : direction == 2 ? 1 : 0;
                    var dy = direction == 1 ? -1 : direction == 3 ? 1 : 0;
                    var screenX = dx < 0 ? viewport.TileSize * .5f :
                        dx > 0 ? width - viewport.TileSize * .5f : width * .5f;
                    var screenY = dy < 0 ? viewport.TileSize :
                        dy > 0 ? height - viewport.TileSize : height * .5f;
                    var linkX = (viewport.OriginX +
                        (screenX - viewport.Left) / viewport.TileSize) * 16f;
                    var linkY = (viewport.OriginY +
                        (screenY - viewport.Top) / viewport.TileSize) * 16f;
                    Check(viewport.TryGetEdgeScrollTarget(linkX, linkY, dx, dy,
                            160, 128, out var x, out var y) &&
                          x == viewport.CameraOriginX + dx * 10 &&
                          y == viewport.CameraOriginY + dy * 8,
                        "Link approaching a visible screen edge must scroll in that direction.");
                    Check(!viewport.TryGetEdgeScrollTarget(linkX, linkY, -dx, -dy,
                            160, 128, out _, out _),
                        "Walking away from the edge must not scroll back immediately.");
                }
                var middleX = (viewport.OriginX + (width * .5f - viewport.Left) / viewport.TileSize) * 16f;
                var middleY = (viewport.OriginY + (height * .5f - viewport.Top) / viewport.TileSize) * 16f;
                Check(!viewport.TryGetEdgeScrollTarget(middleX, middleY, 1, 1,
                        160, 128, out _, out _),
                    "An offset map origin must not scroll while Link is at screen centre.");
            }
        }
    }

    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }
}
