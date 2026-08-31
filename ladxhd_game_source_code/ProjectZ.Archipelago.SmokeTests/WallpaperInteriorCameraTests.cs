using ProjectZ;

internal static class WallpaperInteriorCameraTests
{
    public static void Run()
    {
        foreach (var (width, height) in new[] { (1080, 2400), (1200, 2608), (1840, 2944), (2944, 1840) })
        {
            SmallRoomFits(width, height);
            ScrollReachesBothEnds(width, height);
            DungeonRoomStaysCentered(width, height);
            ClampedCornerStillScrolls(width, height);
        }
        // A map larger than the screen but smaller than the overscanned tile
        // cache must still pan far enough to expose its last column.
        var narrow = Create(1080, 2400, 13, 8, 8, 120);
        Check(narrow.Columns > 13, "Fixture must fit inside cache overscan, not the visible screen.");
        var end = narrow.WithCameraOrigin(100, 0, 13, 8);
        Check(end.CameraOriginX > 0 && Near(ProjectX(end, 13 * 16), 1080),
            "Draw-cache overscan must not clip the far edge of a narrow interior.");
    }

    private static void SmallRoomFits(int width, int height)
    {
        foreach (var spawn in new[] { (8f, 8f), (80f, 120f), (152f, 120f) })
        {
            var view = Create(width, height, 10, 8, spawn.Item1, spawn.Item2);
            Check(Near(ProjectX(view, 80), width / 2f) && Near(ProjectY(view, 64), height / 2f),
                "Small interiors must centre their whole map, regardless of the entrance used.");
            Check(ProjectX(view, 0) >= 0 && ProjectX(view, 160) <= width &&
                  ProjectY(view, 0) >= 0 && ProjectY(view, 128) <= height,
                "The roof, side walls and exit must remain visible in a single-room interior.");
        }
    }

    private static void ScrollReachesBothEnds(int width, int height)
    {
        foreach (var (columns, rows) in new[] { (20, 8), (10, 32), (62, 58) })
        foreach (var spawn in new[] { (8f, 8f), (columns * 16f - 8, rows * 16f - 8) })
        {
            var initial = Create(width, height, columns, rows, spawn.Item1, spawn.Item2);
            var end = initial.WithCameraOrigin(1000, 1000, columns, rows);
            Check(ProjectX(end, columns * 16) <= width + .01f &&
                  ProjectY(end, rows * 16) <= height + .01f,
                "The right/bottom map edges must be revealable even when entered from the opposite end.");
            var start = end.WithCameraOrigin(0, 0, columns, rows);
            Check(ProjectX(start, 0) >= -.01f && ProjectY(start, 0) >= -.01f,
                "Returning the camera must reveal the left/top map edges regardless of entrance.");

            // Android rebuilds the base viewport from the entry each draw and
            // applies the followed origin. Fractional origins and parallax
            // changes must not accumulate a second entrance-based offset.
            for (var frame = 1; frame <= 30; frame++)
            {
                var x = end.CameraOriginX * frame / 30f;
                var y = end.CameraOriginY * frame / 30f;
                var continuous = start.WithCameraOrigin(x, y, columns, rows);
                LiveWallpaperMapViewport.TryCreateCentered(width, height, columns, rows,
                    spawn.Item1, spawn.Item2, frame % 2, out var redrawn);
                redrawn = redrawn.WithCameraOrigin(x, y, columns, rows);
                Check(Near(ProjectX(continuous, 80), ProjectX(redrawn, 80)) &&
                      Near(ProjectY(continuous, 64), ProjectY(redrawn, 64)),
                    "Camera redraws must retain one stable projection during a smooth scroll.");
            }
        }
    }

    private static void DungeonRoomStaysCentered(int width, int height)
    {
        const int columns = 62, rows = 58, offset = 1;
        const float roomX = 736, roomY = 592; // Offset-aware room (41,33), centre (46,37).
        var view = Create(width, height, columns, rows, 680, 600);
        view.TryGetRoomScrollTarget(roomX, roomY, offset, offset, columns, rows, out var x, out var y);
        var centered = view.WithCameraOrigin(x, y, columns, rows);
        Check(Near(ProjectX(centered, roomX), width / 2f) && Near(ProjectY(centered, roomY), height / 2f),
            "A dungeon entered away from room centre must scroll to actual screen centre, not cache centre.");
        Check(!centered.TryGetRoomScrollTarget(roomX - 48, roomY + 32, offset, offset,
                columns, rows, out _, out _),
            "Walking within the same dungeon room must not keep adjusting the camera.");
        Check(centered.TryGetRoomScrollTarget(roomX - 160, roomY, offset, offset,
                columns, rows, out x, out y), "Entering the adjacent dungeon room must scroll.");
        var adjacent = centered.WithCameraOrigin(x, y, columns, rows);
        Check(Near(ProjectX(adjacent, roomX - 160), width / 2f) &&
              Near(ProjectY(adjacent, roomY), height / 2f),
            "An adjacent dungeon room must land at screen centre after the transition.");
    }

    private static void ClampedCornerStillScrolls(int width, int height)
    {
        var view = Create(width, height, 62, 58, 600, 600).WithCameraOrigin(1000, 10, 62, 58);
        var linkX = (view.OriginX + (width - view.TileSize / 2 - view.Left) / view.TileSize) * 16;
        var linkY = (view.OriginY + (height - view.TileSize - view.Top) / view.TileSize) * 16;
        Check(view.TryGetEdgeScrollTarget(linkX, linkY, 1, 1, 62, 58, out var x, out var y) &&
              Near(x, view.CameraOriginX) && y > view.CameraOriginY,
            "The right map limit must not block a downward camera scroll during diagonal movement.");
    }

    private static LiveWallpaperMapViewport Create(int width, int height, int columns, int rows, float x, float y)
    {
        Check(LiveWallpaperMapViewport.TryCreateCentered(width, height, columns, rows, x, y, .5f, out var view),
            "Interior camera fixture must be valid.");
        return view;
    }

    private static float ProjectX(LiveWallpaperMapViewport view, float x) =>
        view.Left + (x / 16f - view.OriginX) * view.TileSize;
    private static float ProjectY(LiveWallpaperMapViewport view, float y) =>
        view.Top + (y / 16f - view.OriginY) * view.TileSize;
    private static bool Near(float a, float b) => Math.Abs(a - b) < .01f;
    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }
}
