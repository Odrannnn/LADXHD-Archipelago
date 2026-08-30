using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperIndoorNavigationTests
{
    public static void Run()
    {
        CheckStairs();
        CheckMovedBlockRoutes();
        CheckInstalledStairs();
    }

    private static void CheckStairs()
    {
        for (var direction = 0; direction < 4; direction++)
        {
            var stair = new LiveWallpaperMapPortal(32, 32, 16, 16,
                direction, 1, "stairs", "next.map", "return");
            // The old planner snapped y=45 to y=48, outside its 1.5px gate.
            Check(stair.ShouldActivateAt(40, 48, 0, direction),
                "Grounded Link on the grid-snapped stairs must activate them.");
            Check(!stair.ShouldActivateAt(40, 48, 0, direction, grounded: false),
                "A feather jump over top-down stairs must not activate them.");
            Check(!stair.ShouldActivateAt(28, 48, 0, direction),
                "Standing next to stairs must not activate them.");
            var expected = direction switch
            {
                0 => new Vector2(30, 45),
                1 => new Vector2(40, 34),
                2 => new Vector2(50, 45),
                _ => new Vector2(40, 56)
            };
            var spawn = new Vector2(stair.GetLinkSpawnX(false), stair.GetLinkSpawnY(false));
            Check(spawn == expected, "Stairs must use ObjDoor's inset trigger and walk-in offset.");
            Check(!stair.ShouldActivateAt(spawn.X, spawn.Y, 0, direction),
                "Arriving at stairs must not immediately activate the return stair.");
        }

        // Compare the shared calculation against the previous game's formula
        // for both modes, odd/even dimensions, and side-view ladders.
        foreach (var mode in new[] { 0, 1 })
        foreach (var sideView in new[] { false, true })
        foreach (var width in new[] { 16, 17, 32 })
        for (var direction = 0; direction < 4; direction++)
        {
            var r = mode == 1 && !sideView
                ? new Rectangle(38, 38, width - 12, 4)
                : new Rectangle(32, 32, width, 16);
            Check(DoorGameplayGeometry.GetTrigger(32, 32, width, 16, mode, sideView) == r,
                "Shared trigger must preserve gameplay geometry.");
            var offset = mode == 1 && !sideView ? 4 : 0;
            var expected = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f + 5);
            if (direction == 0) expected.X = r.X - 4 - offset;
            if (direction == 1) expected.Y = r.Y - offset;
            if (direction == 2) expected.X = r.Right + 4 + offset;
            if (direction == 3) expected.Y = r.Bottom + 10 + offset;
            if (sideView && direction % 2 == 0) expected.Y = r.Bottom;
            if (sideView && direction == 1) expected.Y -= 4;
            if (sideView && direction == 3) expected.Y += 4;
            Check(DoorGameplayGeometry.GetWalkingSpawn(r, direction, mode, sideView, 8, 10) == expected,
                "Shared arrival must preserve all walking-door and ladder offsets.");
        }
    }

    private static void CheckMovedBlockRoutes()
    {
        var map = LoadRoom("1;48;48;4");
        var key = map.GetMoveStoneKey(48, 48);
        var positions = new Dictionary<int, Vector2> { [key] = new(64, 48) };
        var relocated = new HashSet<int> { key };
        var navigation = map.WithMovedBlocksForNavigation(positions, relocated);
        Check(!navigation.IntersectsCollision(48, 48, 16, 16, false) &&
              navigation.IntersectsCollision(64, 48, 16, 16, false, includeMoveStones: false),
            "The vacated tile must be free, and the moved tile solid even in a push-capable route.");
        Check(map.IntersectsCollision(48, 48, 16, 16, false) &&
              !map.IntersectsCollision(64, 48, 16, 16, false),
            "Navigation must not mutate installed map collision or render data.");
        Check(!navigation.TryGetMoveStoneAt(48, 48, 16, 16, out _) &&
              !navigation.CanPushMoveStone(key, 2),
            "A completed push must not schedule another push from the old position.");
        positions.Clear();
        Check(navigation.IntersectsCollision(64, 48, 16, 16, false),
            "A completed navigation snapshot must not retain mutable simulation dictionaries.");

        LiveWallpaperMapViewport.TryCreateCentered(1200, 2608, map.Width, map.Height,
            40, 60, .5f, out var viewport);
        var plan = LiveWallpaperJourneyPlanner.CreateToPoint(navigation, viewport, 40, 60, 112, 60);
        Check(plan.Points.Count > 1 && plan.Points.All(p =>
                p.Action != LiveWallpaperJourneyAction.PushBlock &&
                !navigation.IntersectsCollision(p.PixelX - 4, p.PixelY - 10, 8, 10, false)),
            "A route after pushing must detour around the block's new position, including its endpoint.");

        var blockedMap = LoadRoom("1;48;48;4", "0;64;48");
        Check(!blockedMap.CanPushMoveStone(key, 2) && !map.CanPushMoveStone(key, 0),
            "Push planning must reject a blocked destination and a forbidden direction.");
        var blockedPlan = LiveWallpaperJourneyPlanner.CreateToPoint(blockedMap, viewport, 40, 60, 112, 60);
        Check(blockedPlan.Points.Count > 1 && blockedPlan.Points.All(p =>
                p.Action != LiveWallpaperJourneyAction.PushBlock),
            "An impossible push must route around the block, not repeatedly walk into it.");

        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(40, 60);
        simulation.UpdateJourney(1, 0, 0, true, map, viewport, false, allowViewportFollow: true);
        Check(simulation.TryWalkTo(map, viewport, 112, 60), "Tap route must be accepted.");
        var sawPush = false;
        var arrived = false;
        for (var frame = 1; frame <= 900; frame++)
        {
            var state = simulation.UpdateJourney(1, 0, frame * 17L, true, map,
                viewport, false, allowViewportFollow: true);
            sawPush |= state.Action == LiveWallpaperLinkRouteAction.Pushing;
            if (Math.Abs(state.MapX * 16 - 112) < 3 && Math.Abs(state.MapY * 16 - 60) < 5)
            {
                arrived = true;
                break;
            }
        }
        Check(sawPush && arrived, "Link must finish a push, replan around it, and reach the tapped goal.");
        var push = typeof(LiveWallpaperLinkSimulation).GetMethod("TryStartMoveStonePush",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Check(!(bool)push.Invoke(simulation, [map, key, Vector2.UnitX, 20_000L, false])!,
            "ObjMoveStone's moved state must refuse a second push.");

        var fallen = map.WithMovedBlocksForNavigation(new Dictionary<int, Vector2>(), relocated);
        Check(!fallen.IntersectsCollision(48, 48, 16, 16, false) &&
              !fallen.IntersectsCollision(64, 48, 16, 16, false),
            "A fallen block must leave neither an original nor destination ghost collider.");
    }

    private static LiveWallpaperMap LoadRoom(params string[] objects)
    {
        var text = new StringBuilder("3\n0\n0\ndungeon.png\n10\n8\n1\n");
        for (var row = 0; row < 8; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", 10)));
        text.Append("2\nc1\nmoveStone\n").AppendLine((objects.Length + 32).ToString());
        for (var x = 0; x < 10; x++)
        {
            text.AppendLine($"0;{x * 16};0");
            text.AppendLine($"0;{x * 16};112");
        }
        for (var y = 1; y < 7; y++)
        {
            text.AppendLine($"0;0;{y * 16}");
            text.AppendLine($"0;144;{y * 16}");
        }
        foreach (var obj in objects) text.AppendLine(obj);
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map),
            "Indoor fixture must load.");
        return map;
    }

    private static void CheckInstalledStairs()
    {
        var root = Environment.GetEnvironmentVariable("LADXHD_TEST_GAME_DATA");
        if (string.IsNullOrWhiteSpace(root)) return;
        Check(Directory.Exists(root), "Configured installed data must exist.");
        var count = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "Maps"), "*.map"))
        {
            using var reader = File.OpenText(path);
            if (!LiveWallpaperMap.TryLoad(reader, out var map) || map.Is2DMap) continue;
            foreach (var stair in map.Portals.Where(p => p.Mode == 1 && p.HasDestination))
            {
                var x = MathF.Round(stair.LinkTargetX / 8) * 8;
                var y = MathF.Round(stair.LinkTargetY / 8) * 8;
                Check(stair.ShouldActivateAt(x, y, 0, stair.Direction),
                    "Installed stair grid endpoint must overlap its real trigger.");
                count++;
            }
        }
        Check(count > 0, "Installed stair coverage must not be empty.");
        Console.WriteLine($"Installed stair triggers checked: {count}");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
