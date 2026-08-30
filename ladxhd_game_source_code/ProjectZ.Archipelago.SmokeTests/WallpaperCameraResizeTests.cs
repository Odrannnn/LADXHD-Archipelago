using ProjectZ;

internal static class WallpaperCameraResizeTests
{
    public static void Run()
    {
        const int mapWidth = 160, mapHeight = 128;
        foreach (var (width, height) in new[] { (1600, 2560), (1200, 2608), (1080, 2400) })
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
            var linkX = horizontal ? (resized.CameraOriginX + resized.Columns / 2f) * 16f :
                (resized.CameraOriginX + resized.Columns - 2f) * 16f;
            var linkY = horizontal ? (resized.CameraOriginY + resized.Rows - 3f) * 16f :
                (resized.CameraOriginY + resized.Rows / 2f) * 16f;
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

    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }
}
