using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperPathingRecoveryTests
{
    public static void Run()
    {
        SameMapResumePreservesPositionAndManualTap();
        ViewportChangePreservesManualTap();
        EnclosedNoRouteDoesNotTeleport();
        FallbackStaysInReachableComponent();
        StalledManualRetryIsBoundedAndRetainsGoal();
    }

    private static void SameMapResumePreservesPositionAndManualTap()
    {
        var map = LoadMap(64, 64, Array.Empty<string>());
        var viewport = CreateViewport(map, 1080, 2400);
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(256, 256);
        Check(simulation.TryWalkTo(map, viewport, 640, 256),
            "The resume fixture must accept a reachable manual destination.");
        var beforePause = simulation.UpdateJourney(1, 0, 34, true, map, viewport, false);
        var afterResume = simulation.UpdateJourney(1, 0, 2_100, true, map, viewport, false);
        Check(Distance(beforePause, afterResume) < 2,
            "A same-map time gap must resume from Link's actual position instead of a journey preset.");
        Check(ManualGoal(simulation) == new Vector2(640, 256) && ManualActive(simulation),
            "A same-map resume must retain the outstanding manual tap.");
    }

    private static void ViewportChangePreservesManualTap()
    {
        var map = LoadMap(96, 96, Array.Empty<string>());
        var portrait = CreateViewport(map, 1080, 2400);
        var landscape = CreateViewport(map, 2400, 1080);
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(512, 512);
        Check(simulation.TryWalkTo(map, portrait, 960, 512),
            "The viewport-change fixture must accept a reachable manual destination.");
        var beforeRotation = simulation.UpdateJourney(1, 0, 34, true, map, portrait, false);
        var afterRotation = simulation.UpdateJourney(1, 0, 51, true, map, landscape, false);
        Check(Distance(beforeRotation, afterRotation) < 3,
            "A viewport change must not reset Link to an autonomous journey entry point.");
        Check(ManualGoal(simulation) == new Vector2(960, 512) && ManualActive(simulation),
            "A viewport change must replan the original manual target rather than replace it.");
    }

    private static void EnclosedNoRouteDoesNotTeleport()
    {
        // Link's body is offset (-4,-10), so the one free c1 tile gives the
        // snapped (136,144) body a genuine floor while every 8px neighbour
        // overlaps the collision ring. No fallback candidate remains.
        var walls = new List<string>();
        for (var y = 0; y < 32; y++)
        for (var x = 0; x < 32; x++)
            if (x != 8 || y != 8)
                walls.Add($"0;{x * 16};{y * 16}");
        var map = LoadMap(32, 32, walls.ToArray());
        var viewport = CreateViewport(map, 1080, 2400);
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(136, 144);
        var state = simulation.UpdateJourney(1, 0, 0, true, map, viewport, false);
        var plan = (LiveWallpaperJourneyPlan)GetPrivate(simulation, "_journeyPlan");
        Check(plan.Points.Count == 0,
            "The one-cell collision-ring fixture must force the planner's empty no-route plan.");
        Check(Math.Abs(state.MapX * 16 - 136) < .01f && Math.Abs(state.MapY * 16 - 144) < .01f,
            "An enclosed no-route arrival must stand at Link's real position, never teleport to a fallback activity.");
    }

    private static void FallbackStaysInReachableComponent()
    {
        var map = LoadTinyPocketMap();
        const int minX = 8, minY = 16, maxX = 1016, maxY = 1016;
        const int startX = 416, startY = 512;
        var candidates = Enumerable.Range(0, 63)
            .SelectMany(row => Enumerable.Range(0, 64)
                .Select(column => new Point(minX + column * 16, minY + row * 16)))
            .Where(point => (point.X != startX || point.Y != startY) &&
                IsWalkableCandidate(map, point))
            .ToList();
        candidates.Sort((left, right) =>
        {
            var leftDistance = (left.X - startX) * (left.X - startX) +
                               (left.Y - startY) * (left.Y - startY);
            var rightDistance = (right.X - startX) * (right.X - startX) +
                                (right.Y - startY) * (right.Y - startY);
            return rightDistance.CompareTo(leftDistance);
        });
        var variant = FindVariantWithUnreachableOpening(candidates, startX);
        var offset = PositiveHash(startX, variant, 113) % candidates.Count;
        Check(Enumerable.Range(0, 48).All(index =>
                  candidates[(offset + index) % candidates.Count].X > 512),
            "Fixture precondition must make the old bounded fallback spend all 48 attempts in the disconnected area.");
        var fallback = typeof(LiveWallpaperJourneyPlanner).GetMethod(
            "CreateReachableFallback", BindingFlags.Static | BindingFlags.NonPublic)!;
        var plan = (LiveWallpaperJourneyPlan)fallback.Invoke(null,
            [map, (float)startX, (float)startY, minX, minY, maxX, maxY, variant])!;
        Check(plan.Points.Count > 1 && plan.Points.All(point => point.PixelX < 512),
            "After its first unreachable candidate, fallback must filter to Link's tiny reachable component.");
    }

    private static void StalledManualRetryIsBoundedAndRetainsGoal()
    {
        var walls = Enumerable.Range(0, 64).Select(y => $"0;512;{y * 16}").ToArray();
        var map = LoadMap(64, 64, walls);
        var viewport = CreateViewport(map, 1080, 2400);
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(416, 512);
        SetPrivate(simulation, "_manualDestinationActive", true);
        SetPrivate(simulation, "_manualDestination", new Vector2(608, 512));
        SetPrivate(simulation, "_manualRetryPosition", new Vector2(416, 512));
        var retry = typeof(LiveWallpaperLinkSimulation).GetMethod("RetryManualDestination",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Check(!(bool)retry.Invoke(simulation, [map, viewport])! &&
              !(bool)retry.Invoke(simulation, [map, viewport])! &&
              !(bool)retry.Invoke(simulation, [map, viewport])!,
            "A stalled unreachable tap must use bounded retries rather than an unbounded replan loop.");
        Check(ManualActive(simulation) && ManualGoal(simulation) == new Vector2(608, 512) &&
              (int)GetPrivate(simulation, "_manualReplanAttempts") == 2,
            "A bounded stalled retry must retain the user's goal until normal no-route fallback handling.");
    }

    private static LiveWallpaperMapViewport CreateViewport(
        LiveWallpaperMap map, int width, int height)
    {
        Check(LiveWallpaperMapViewport.TryCreateCentered(width, height, map.Width, map.Height,
                map.Width * 8, map.Height * 8, .5f, out var viewport),
            "The recovery fixture viewport must load.");
        return viewport;
    }

    private static LiveWallpaperMap LoadMap(int width, int height, string[] objects)
    {
        var text = new StringBuilder($"3\n0\n0\nrecovery.png\n{width}\n{height}\n1\n");
        for (var row = 0; row < height; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", width)));
        text.AppendLine("2");
        text.AppendLine("c1");
        text.AppendLine("e2");
        text.AppendLine(objects.Length.ToString());
        foreach (var entry in objects) text.AppendLine(entry);
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map) && !map.Is2DMap,
            "The recovery fixture map must parse as a top-down map.");
        return map;
    }

    private static LiveWallpaperMap LoadTinyPocketMap()
    {
        var walls = new List<string>();
        for (var y = 0; y < 64; y++)
        for (var x = 0; x < 64; x++)
        {
            var inPocket = x is >= 24 and <= 27 && y is >= 30 and <= 33;
            if (x <= 32 && !inPocket)
                walls.Add($"0;{x * 16};{y * 16}");
        }
        return LoadMap(64, 64, walls.ToArray());
    }

    private static bool IsWalkableCandidate(LiveWallpaperMap map, Point point) =>
        !map.IntersectsCollision(point.X - 4, point.Y - 10, 8, 10, false);

    private static int FindVariantWithUnreachableOpening(
        IReadOnlyList<Point> candidates, int startX)
    {
        for (var variant = 0; variant < 10_000; variant++)
        {
            var offset = PositiveHash(startX, variant, 113) % candidates.Count;
            if (Enumerable.Range(0, 48).All(index =>
                    candidates[(offset + index) % candidates.Count].X > 512))
                return variant;
        }
        throw new InvalidOperationException(
            "The disconnected fallback fixture could not select an unreachable first 48 candidates.");
    }

    private static int PositiveHash(int first, int second, int salt)
    {
        unchecked
        {
            var value = first * 73856093 ^ second * 19349663 ^ salt * 83492791;
            return value == int.MinValue ? int.MaxValue : Math.Abs(value);
        }
    }

    private static bool ManualActive(LiveWallpaperLinkSimulation simulation) =>
        (bool)GetPrivate(simulation, "_manualDestinationActive");

    private static Vector2 ManualGoal(LiveWallpaperLinkSimulation simulation) =>
        (Vector2)GetPrivate(simulation, "_manualDestination");

    private static object GetPrivate(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void SetPrivate(object instance, string name, object value) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private static float Distance(LiveWallpaperSimulatedLinkState left,
        LiveWallpaperSimulatedLinkState right) =>
        Vector2.Distance(new Vector2(left.MapX, left.MapY), new Vector2(right.MapX, right.MapY)) * 16f;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
