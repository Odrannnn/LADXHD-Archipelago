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
        CheckArrivalEndpointExclusion();
        CheckMovedBlockRoutes();
        CheckInstalledStairs();
        CheckTelephoneFurnitureAndTunic();
        CheckLedgesAndBooks();
        CheckDungeonFloorFixtures();
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

    private static void CheckArrivalEndpointExclusion()
    {
        var map = LoadRoom("2;32;32;16;16;arrival;next.map;return;3;1",
            "2;64;32;16;16;onward;next.map;other;3;1");
        var stair = map.Portals[0];
        var onward = map.Portals[1];
        var build = typeof(LiveWallpaperJourneyPlanner).GetMethod("BuildEndpoints",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        List<Vector2> Endpoints(string excluded)
        {
            var result = (System.Collections.IEnumerable)build.Invoke(null,
                [map, 40, 32, 80, 64, 40, 32, 80, 64, excluded])!;
            return result.Cast<object>().Select(endpoint => new Vector2(
                (int)endpoint.GetType().GetProperty("X")!.GetValue(endpoint)!,
                (int)endpoint.GetType().GetProperty("Y")!.GetValue(endpoint)!)).ToList();
        }
        bool Activates(LiveWallpaperMapPortal portal, Vector2 point) =>
            portal.ShouldActivateAt(point.X, point.Y, 0, portal.Direction);

        Check(Endpoints(null).Any(p => Activates(stair, p)),
            "The stair must remain available when it is not the excluded arrival.");
        var endpoints = Endpoints("arrival");
        Check(endpoints.All(p => !Activates(stair, p)),
            "Arrival exclusion must also remove edge endpoints overlapping the stair trigger, not just its exact centre.");
        Check(endpoints.Any(p => Activates(onward, p)) &&
              endpoints.Any(p => !Activates(onward, p)),
            "Excluding an arrival must preserve onward stairs and ordinary exploration targets.");
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
        foreach (var mode in new[] { 0, 1 })
        {
            var passageway = LoadRoom("1;48;48;4",
                $"2;64;48;16;16;exit;next.map;entry;3;{mode}");
            Check(!passageway.CanPushMoveStone(key, 2),
                "A push must preserve ObjMoveStone's full-tile passageway collision, including stairs.");
        }
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
        text.Append("3\nc1\nmoveStone\ndoor\n").AppendLine((objects.Length + 32).ToString());
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

    private static void CheckTelephoneFurnitureAndTunic()
    {
        var text = new StringBuilder("3\n0\n0\nhouse.png\n10\n8\n1\n");
        for (var row = 0; row < 8; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", 10)));
        text.Append("9\nc1\nphone\nsign\ncave_table\ncave_bed\nvase_empty\nvase_flower\nbanana\nnpc_bag\n")
            .AppendLine("41");
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
        text.AppendLine("1;48;32")
            .AppendLine("2;48;32;ulrira;;;")
            .AppendLine("2;64;32;ulrira_telephone;;;")
            .AppendLine("3;80;16")
            .AppendLine("4;112;16")
            .AppendLine("5;96;64")
            .AppendLine("6;112;64")
            .AppendLine("7;16;80")
            .AppendLine("8;32;80");
        Check(LiveWallpaperMap.TryLoad(
                new StringReader(text.ToString()), out var map),
            "Telephone furniture fixture must load.");

        var phone = map.Decorations.SingleOrDefault(
            decoration => decoration.SpriteId == "phone");
        Check(phone.SpriteId == "phone" && phone.EntityX == 56 &&
              phone.EntityY == 46 && phone.PlayerLayer,
            "The installed phone must use ObjSprite's exact atlas id, entity offset and layer.");
        Check(map.Decorations.Any(d => d.SpriteId == "cave_table") &&
              map.Decorations.Any(d => d.SpriteId == "cave_bed") &&
              map.Decorations.Any(d => d.SpriteId == "vase_empty") &&
              map.Decorations.Any(d => d.SpriteId == "vase_flower") &&
              map.Decorations.Any(d => d.SpriteId == "bananas") &&
              map.Decorations.Any(d => d.SpriteId == "npc_bag" &&
                  d.AtlasName == "npcs"),
            "Installed indoor furniture must retain its canonical atlas entries.");
        Check(map.IntersectsCollision(48, 36, 16, 12, false) &&
              map.Actors.Count == 2 &&
              map.Actors.All(actor =>
                  actor.Kind == LiveWallpaperMapActorKind.Telephone),
            "The phone collider and both native telephone dialog identifiers must form interaction targets.");

        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(40, 48);
        var plan = new LiveWallpaperJourneyPlan(
        [
            new LiveWallpaperJourneyPoint(
                40, 48, LiveWallpaperJourneyAction.Interact)
        ], interactionPointIndex: 0, interactionActorIndex: 0);
        var type = typeof(LiveWallpaperLinkSimulation);
        type.GetField("_currentJourneyMap",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(simulation, map);
        type.GetField("_journeyPlan",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(simulation, plan);
        type.GetField("_journeyPointIndex",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(simulation, 0);
        var reached = type.GetMethod("OnJourneyPointReached",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        reached.Invoke(simulation, [1000L]);
        Check(simulation.CloakType == ProjectZ.InGame.Things.GameManager.CloakBlue,
            "One telephone interaction must advance green to blue through the gameplay AP rule.");
        reached.Invoke(simulation, [1100L]);
        Check(simulation.CloakType == ProjectZ.InGame.Things.GameManager.CloakBlue,
            "One held interaction must not cycle the tunic more than once.");
        type.GetField("_interactionPauseStarted",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(simulation, false);
        type.GetField("_journeyPointIndex",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(simulation, 0);
        reached.Invoke(simulation, [4000L]);
        Check(simulation.CloakType == ProjectZ.InGame.Things.GameManager.CloakRed,
            "The next telephone interaction must advance blue to red.");
    }

    private static void CheckLedgesAndBooks()
    {
        var text = new StringBuilder("3\n0\n0\nhouse.png\n10\n8\n1\n");
        for (var row = 0; row < 8; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", 10)));
        text.Append("3\nc1\njump\nbook\n").AppendLine("35");
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
        // A full-width authored downward ledge makes reverse detouring
        // impossible. Values match ObjJump's constructor argument order.
        text.AppendLine("1;16;48;0;18;128;8;1;1;100;false;false")
            .AppendLine("2;32;80;;book1;2")
            .AppendLine("2;48;80;;book2;");
        Check(LiveWallpaperMap.TryLoad(
                new StringReader(text.ToString()), out var map),
            "Ledge and book fixture must load.");
        Check(map.Ledges.Count == 1 && map.Ledges[0].Direction == 3 &&
              map.Ledges[0].InertiaMilliseconds == 100 &&
              map.IntersectsCollision(16, 48, 128, 8, false),
            "ObjJump must remain a solid, directional authored ledge during ordinary movement.");

        var books = map.Decorations.Where(d => d.SpriteId.StartsWith("book_"))
            .OrderBy(d => d.EntityX).ToArray();
        Check(books.Length == 2 &&
              books[0].SpriteId == "book_2" &&
              books[0].EntityX == 40 && books[0].EntityY == 96 &&
              books[0].DrawOffsetX == -4 && books[0].DrawOffsetY == -11 &&
              !books[0].PlayerLayer && books[1].SpriteId == "book_0",
            "ObjBook must use its native atlas index, entity anchor, draw offset and bottom layer.");

        Check(LiveWallpaperMapViewport.TryCreateCentered(
                1200, 2608, map.Width, map.Height,
                40, 60, .5f, out var viewport),
            "Ledge fixture viewport must be valid.");
        var down = LiveWallpaperJourneyPlanner.CreateToPoint(
            map, viewport, 80, 32, 80, 88);
        Check(down.Points.Any(point =>
                point.Action == LiveWallpaperJourneyAction.RailJump &&
                point.LedgeIndex == 0),
            "A destination below an authored ledge must schedule its native rail jump.");
        var up = LiveWallpaperJourneyPlanner.CreateToPoint(
            map, viewport, 80, 88, 80, 32);
        Check(up.Points.All(point =>
                  point.Action != LiveWallpaperJourneyAction.RailJump) &&
              (up.Points.Count == 0 || up.Points[^1].PixelY >= 56),
            "The same ledge must not create a reverse edge that lets Link climb it.");

        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(80, 32);
        simulation.UpdateJourney(1, 0, 0, true, map, viewport, false,
            allowViewportFollow: true);
        Check(simulation.TryWalkTo(map, viewport, 80, 88),
            "The downward ledge tap route must be accepted.");
        var sawJump = false;
        var crossed = false;
        for (var frame = 1; frame <= 600; frame++)
        {
            var state = simulation.UpdateJourney(
                1, 0, frame * 17L, true, map, viewport, false,
                allowViewportFollow: true);
            sawJump |= state.Action == LiveWallpaperLinkRouteAction.FeatherJump;
            if (state.MapY * 16f > 72f)
            {
                crossed = true;
                break;
            }
        }
        Check(sawJump && crossed,
            "Link must complete the authored rail-jump curve and land below the ledge.");
    }

    private static void CheckDungeonFloorFixtures()
    {
        var text = new StringBuilder("3\n0\n0\ndungeon.png\n8\n6\n1\n");
        for (var row = 0; row < 6; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", 8)));
        text.Append("6\nc1\nbutton\ncolorJumpTile\ndungeonSwitch\nspikes\niceBlock\n")
            .AppendLine("31");
        for (var x = 0; x < 8; x++)
        {
            text.AppendLine($"0;{x * 16};0");
            text.AppendLine($"0;{x * 16};80");
        }
        for (var y = 1; y < 5; y++)
        {
            text.AppendLine($"0;0;{y * 16}");
            text.AppendLine($"0;112;{y * 16}");
        }
        text.AppendLine("1;32;32;room_button")
            .AppendLine("2;48;32;2")
            .AppendLine("2;64;32;9")
            .AppendLine("3;80;32;room_switch")
            .AppendLine("1;96;64;ow_castle_button_2")
            .AppendLine("4;32;64")
            .AppendLine("5;64;64");
        Check(LiveWallpaperMap.TryLoad(
                new StringReader(text.ToString()), out var map),
            "Dungeon floor-fixture map must load.");

        var button = map.Decorations.Single(d => d.SpriteId == "button");
        var tiles = map.Decorations
            .Where(d => d.SpriteId.StartsWith("color_tile_"))
            .OrderBy(d => d.EntityX).ToArray();
        var dungeonSwitch = map.Decorations.Single(
            d => d.SpriteId == "dungeon_switch");
        Check(button.EntityX == 32 && button.EntityY == 32 &&
              !button.PlayerLayer && button.TopLeft,
            "ObjButton must retain its native unpressed atlas frame, anchor and bottom layer.");
        Check(tiles.Length == 2 && tiles[0].SpriteId == "color_tile_2" &&
              tiles[1].SpriteId == "color_tile_2" &&
              tiles.All(tile => !tile.PlayerLayer && tile.TopLeft),
            "ObjColorJumpTile must clamp and draw its installed native starting frame.");
        Check(dungeonSwitch.EntityX == 80 && dungeonSwitch.EntityY == 48 &&
              dungeonSwitch.DrawOffsetY == -16 && dungeonSwitch.PlayerLayer &&
              dungeonSwitch.TopLeft,
            "ObjDungeonSwitch must retain its native entity depth and separate sprite offset.");
        var spikes = map.AnimatedObjects.Single(
            animatedObject => animatedObject.AnimationPath == "Objects/spikes.ani");
        var iceBlock = map.AnimatedObjects.Single(
            animatedObject => animatedObject.AnimationPath == "Objects/ice block.ani");
        Check(spikes.AnimationName == "idle" &&
              spikes.DrawX == 32 && spikes.DrawY == 64 &&
              spikes.EntityX == 32 && spikes.EntityY == 64,
            "ObjSpikes must retain its native animation and zero component offset.");
        Check(iceBlock.AnimationName == "idle" &&
              iceBlock.DrawX == 64 && iceBlock.DrawY == 64 &&
              iceBlock.EntityX == 72 && iceBlock.EntityY == 72,
            "ObjIceBlock must retain its native centred entity and separate -8,-8 draw offset.");
        Check(map.IntersectsCollision(37, 35, 6, 3, false) &&
              map.IntersectsCollision(81, 36, 14, 12, false) &&
              !map.IntersectsCollision(48, 32, 16, 16, false) &&
              !map.IntersectsCollision(101, 67, 6, 3, false) &&
              !map.IntersectsCollision(32, 64, 16, 16, false) &&
              map.IntersectsCollision(64, 64, 16, 16, false),
            "Fixture collision must match gameplay: buttons and switches block, color tiles and spikes remain walkable, and intact ice blocks are solid.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
