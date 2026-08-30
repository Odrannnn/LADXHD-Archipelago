using System;
using System.IO;
using System.Linq;
using System.Text;
using ProjectZ;

internal static class WallpaperTouchRoutingTests
{
    public static void Run()
    {
        foreach (var (width, height) in new[] { (2944, 1840), (1840, 2944) })
        {
            var viewport = CreateViewport(width, height);
            DisconnectedTargetReturnsEmptyPlan(viewport);
            NearbyReachableFallbackAfterDisconnectedSearch(viewport);
            BlockedTargetFallbackRemainsAvailable(viewport);
            OpenRouteRemainsAvailable(viewport);
        }
    }

    private static LiveWallpaperMapViewport CreateViewport(int width, int height)
    {
        Check(LiveWallpaperMapViewport.TryCreateCentered(
                width, height, 64, 64, 512, 512, .5f, out var viewport),
            "The wallpaper viewport fixture must load.");
        Check(viewport.Columns > 0 && viewport.Rows > 0,
            "The fixture must preserve positive viewport bounds.");
        return viewport;
    }

    private static void DisconnectedTargetReturnsEmptyPlan(LiveWallpaperMapViewport viewport)
    {
        var map = LoadMap(wall: true, blockTarget: false);
        var plan = LiveWallpaperJourneyPlanner.CreateToPoint(map, viewport, 416, 512, 608, 512);
        Check(plan.Points.Count == 0,
            "A tap across a real disconnected collision wall must return an empty route after bounded candidate searches.");
    }

    private static void NearbyReachableFallbackAfterDisconnectedSearch(LiveWallpaperMapViewport viewport)
    {
        var map = LoadMap(wall: true, blockTarget: false);
        var plan = LiveWallpaperJourneyPlanner.CreateToPoint(map, viewport, 416, 512, 552, 512);
        Check(plan.Points.Count > 1 && plan.Points[^1].PixelX < 512,
            "A failed search across the wall must still admit the nearby reachable fallback on Link's side.");
    }

    private static void BlockedTargetFallbackRemainsAvailable(LiveWallpaperMapViewport viewport)
    {
        var map = LoadMap(wall: false, blockTarget: true);
        var plan = LiveWallpaperJourneyPlanner.CreateToPoint(map, viewport, 416, 512, 608, 512);
        Check(plan.Points.Count > 1 &&
              !map.IntersectsCollision(plan.Points[^1].PixelX - 4, plan.Points[^1].PixelY - 10, 8, 10, false),
            "A blocked tap must still select its nearby reachable fallback.");
    }

    private static void OpenRouteRemainsAvailable(LiveWallpaperMapViewport viewport)
    {
        var map = LoadMap(wall: false, blockTarget: false);
        var plan = LiveWallpaperJourneyPlanner.CreateToPoint(map, viewport, 416, 512, 608, 512);
        Check(plan.Points.Count > 1 && plan.Points[^1].PixelX == 608 && plan.Points[^1].PixelY == 512,
            "A reachable tap must preserve its exact destination route.");
    }

    private static LiveWallpaperMap LoadMap(bool wall, bool blockTarget)
    {
        const int width = 64, height = 64;
        var text = new StringBuilder("3\n0\n0\ndungeon.png\n64\n64\n1\n");
        for (var y = 0; y < height; y++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", width)));
        text.AppendLine("2");
        text.AppendLine("c1");
        text.AppendLine("e2");
        var wallCount = wall ? height : 0;
        const int distantEnemyCount = 96;
        var targetBlockCount = blockTarget ? 1 : 0;
        text.AppendLine((wallCount + distantEnemyCount + targetBlockCount).ToString());
        if (wall)
            for (var y = 0; y < height; y++)
                text.AppendLine($"0;512;{y * 16}");
        if (blockTarget)
            text.AppendLine("0;604;502");
        // Planner collision checks consult all actors/enemies, including those
        // outside the visible crop. Keep realistic distant density in each case.
        for (var index = 0; index < distantEnemyCount; index++)
            text.AppendLine($"1;{(index % 12) * 16};{(index / 12) * 16}");
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map),
            "Touch-routing map fixture must load.");
        return map;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
