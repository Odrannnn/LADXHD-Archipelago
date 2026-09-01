using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperDungeonDoorTests
{
    public static void Run()
    {
        CheckPresentation();
        var map = Room("1;48;48;4;block", "2;96;48;;gate;2;",
            "4;0;0;gate;block;true");
        var door = map.DungeonDoors.Doors.Single();
        var block = map.Objects.Single(o => o.Template == "moveStone");
        var navigation = map.WithMovedBlocksForNavigation(new Dictionary<int, Vector2>(),
            new HashSet<int> { map.GetMoveStoneKey(48, 48) });
        Check(door.Amount == 1 && Solid(map), "A missing gate key must start closed and solid.");
        Check(!map.CanPushMoveStone(map.GetMoveStoneKey(48, 48), 0), "Block direction mask must still apply.");
        map.DungeonDoors.BlockPushed(block, 2, completed: false);
        map.DungeonDoors.Advance(30);
        Check(Solid(map), "Type-0 block gates must not open at push start.");
        map.DungeonDoors.BlockPushed(block, 2, completed: true);
        Check(Solid(map), "An opening door must retain collision before the half-open point.");
        map.DungeonDoors.Advance(9);
        Check(Solid(map), "Nine native update frames must not clear an opening door.");
        map.DungeonDoors.Advance(2);
        Check(!Solid(map) && !Solid(navigation),
            "Opening past half height must clear body collision and existing navigation snapshots.");
        map.DungeonDoors.Advance(30);
        Check(door.Amount == 0, "Opening animation must end at zero height.");
        map.DungeonDoors.Reset();
        Check(Solid(map) && door.Amount == 1, "A fresh ambient visit must reset its private gate state.");

        var open = Room("2;96;48;;gate;;", "4;0;0;gate;!entered|cleared;true");
        Check(!Solid(open), "Canonical negated conditions must initialize an open gate without a game save.");
        var closing = Room("1;48;48;4;block", "2;96;48;;gate;;",
            "4;0;0;gate;!block;true");
        closing.DungeonDoors.BlockPushed(block, 2, completed: true);
        Check(Solid(closing) && closing.DungeonDoors.Doors[0].Amount == 0,
            "Closing must activate the full collider immediately, before the first animation frame.");
        closing.DungeonDoors.Advance(10);
        Check(closing.DungeonDoors.Doors[0].Amount == 1, "Closing must use the native faster rate.");
        var explicitOpen = Room("2;96;48;;gate;;", "3;0;0;gate;2");
        Check(!Solid(explicitOpen), "ObjDungeonDoor accepts any nonzero, nonnull state value.");
        var locked = Room("2;96;48;1;smallDoor;;unlock", "2;112;48;3;bossDoor;;bossUnlock");
        Check(locked.DungeonDoors.Doors.All(d => d.BlocksMovement),
            "Missing small/nightmare keys must never be invented to open a locked gate.");
        var noKey = Room("2;96;48;;;;");
        Check(noKey.DungeonDoors.Doors.Count == 0 && !Solid(noKey),
            "A ddoor without a state key must be absent, like native IsDead.");

        CheckBlockSimulation(map);
        CheckKeys();
        CheckChestAwardTiming();
        CheckUnlockRoute();
        CheckInstalled();
        Console.WriteLine("Wallpaper dungeon door checks passed.");
    }

    private static void CheckPresentation()
    {
        var atlas = new Rectangle(32, 64, 16, 16);
        for (var mode = 0; mode < 4; mode++)
        for (var direction = 0; direction < 4; direction++)
        for (var step = 0; step <= 20; step++)
        {
            var amount = 1 - step * 0.05f;
            var height = (int)Math.Round(16 * amount);
            var actual = DungeonDoorGameplay.Source(DungeonDoorGameplay.Variant(atlas, mode), amount);
            Check(actual == new Rectangle(32 + mode * 16, 80 - height, 16, height),
                "Door source crop must preserve ObjDungeonDoor's exact variant and rounding.");
            Check(DungeonDoorGameplay.Rotation(direction) == (float)(Math.PI / 2 * (direction + 1)),
                "Door rotation must use the native pivot/direction convention.");
        }
        foreach (var fps in new[] { 15, 30, 60 })
        {
            float amount = 1;
            for (var frame = 0; frame < fps; frame++) amount = DungeonDoorGameplay.Open(amount, 60f / fps);
            Check(amount == 0, "Opening must finish equally at all rendering rates.");
            for (var frame = 0; frame < fps; frame++) amount = DungeonDoorGameplay.Close(amount, 60f / fps);
            Check(amount == 1, "Closing must finish equally at all rendering rates.");
        }
    }

    private static bool Solid(LiveWallpaperMap map)
    {
        var solid = map.IntersectsCollision(96, 48, 16, 16, false);
        Check(map.TryGetBlockingCollisionBounds(96, 48, 16, 16, false, out _) == solid,
            "Route recovery and body movement must agree about door collision.");
        return solid;
    }

    private static void CheckBlockSimulation(LiveWallpaperMap map)
    {
        LiveWallpaperMapViewport.TryCreateCentered(1200, 2608, map.Width, map.Height,
            40, 60, .5f, out var viewport);
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(40, 60);
        simulation.UpdateJourney(1, 0, 0, true, map, viewport, false, allowViewportFollow: true);
        Check(simulation.TryWalkTo(map, viewport, 80, 60), "A route through the block must be accepted.");
        var pushed = false;
        for (var frame = 1; frame <= 500; frame++)
        {
            var state = simulation.UpdateJourney(1, 0, frame * 17L, true, map, viewport, false,
                allowViewportFollow: true);
            pushed |= state.Action == LiveWallpaperLinkRouteAction.Pushing;
            if (pushed && map.DungeonDoors.Doors[0].Amount == 0) break;
        }
        Check(pushed && !Solid(map), "The real push action must deliver the gate event and advance its animation.");
        var plan = LiveWallpaperJourneyPlanner.CreateToPoint(map, viewport, 88, 60, 120, 60);
        Check(plan.Points.Count > 1 && plan.Points.Any(p => p.PixelX > 112),
            "Route planning must see the opened doorway without stale collision.");
    }

    private static void CheckKeys()
    {
        Check(!DungeonDoorGameplay.HasRequiredKey(1, 0) && DungeonDoorGameplay.HasRequiredKey(1, 1) &&
              !DungeonDoorGameplay.HasRequiredKey(3, null) && DungeonDoorGameplay.HasRequiredKey(3, 0),
            "Nightmare-key ownership must not be mistaken for a positive consumable count.");
        var map = Room("2;96;48;1;lock;;lock", "5;32;16;smallkeyChest;one;chestA", "6;0;0;one",
            "5;48;16;smallkeyChest;one;chestA", "5;64;16;smallkeyChest;two;wrongDungeon");
        var doors = map.DungeonDoors;
        var door = doors.Doors.Single();
        Check(!doors.CanUnlock(door), "An uncollected chest must not supply a key.");
        doors.CollectChest(map, map.GetChestKey(64, 16));
        Check(doors.SmallKeyCount == 0 && !doors.TryUnlock(door), "Other-dungeon keys cannot open this lock.");
        doors.CollectChest(map, map.GetChestKey(32, 16));
        doors.CollectChest(map, map.GetChestKey(32, 16));
        doors.CollectChest(map, map.GetChestKey(48, 16));
        Check(doors.SmallKeyCount == 1 && doors.CanUnlock(door), "Repeated presentation and shared chest keys grant only once.");
        Check(doors.TryUnlock(door) && doors.SmallKeyCount == 0 && !doors.TryUnlock(door),
            "One small key opens one lock and must not be spent again during its animation.");
        Check(Solid(map), "Spending a key does not bypass the opening collider.");
        doors.Advance(11);
        Check(!Solid(map), "The consumed-key door must open through the native animation.");
        doors.Reset();
        Check(doors.SmallKeyCount == 0 && !doors.HasNightmareKey && Solid(map),
            "A new ambient visit must reset keys and locks together.");

        var boss = Room("2;96;48;3;boss;;keyhole", "4;0;0;boss;keyhole&!entered;true",
            "5;32;16;nightmarekey;one;bossChest", "6;0;0;one");
        boss.DungeonDoors.CollectChest(boss, boss.GetChestKey(32, 16));
        Check(boss.DungeonDoors.TryUnlock(boss.DungeonDoors.Doors[0]) && boss.DungeonDoors.HasNightmareKey,
            "The real push flag must satisfy a conditional boss gate without consuming the nightmare key.");
        boss.DungeonDoors.Advance(20);
        Check(!Solid(boss), "Conditional keyhole activation must open its gate, not just the push flag.");
        var gated = Room("2;96;48;3;boss;;keyhole", "4;0;0;boss;keyhole&!entered;true",
            "3;0;0;entered;1", "5;32;16;nightmarekey;one;bossChest", "6;0;0;one");
        gated.DungeonDoors.CollectChest(gated, gated.GetChestKey(32, 16));
        Check(!gated.DungeonDoors.CanUnlock(gated.DungeonDoors.Doors[0]) && Solid(gated),
            "A key must not override an unmet installed door condition.");
        var many = new List<string> { "2;96;48;1;lock;;lock", "6;0;0;one" };
        for (var index = 0; index < 10; index++)
            many.Add($"5;{16 + index % 8 * 16};{16 + index / 8 * 16};smallkeyChest;one;key{index}");
        var capped = Room(many.ToArray());
        for (var index = 0; index < 10; index++)
            capped.DungeonDoors.CollectChest(capped, capped.GetChestKey(16 + index % 8 * 16, 16 + index / 8 * 16));
        Check(capped.DungeonDoors.SmallKeyCount == 9, "Small-key chests share the native base-item capacity.");
    }

    private static void CheckChestAwardTiming()
    {
        var text = new StringBuilder("3\n0\n0\noverworld.png\n20\n16\n1\n");
        for (var row = 0; row < 16; row++) text.AppendLine(string.Join(',', Enumerable.Repeat("0", 20)));
        text.Append("2\nchest\ndungeon\n2\n0;160;112;smallkeyChest;one;keyChest;0;false\n1;0;0;one\n");
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map), "Key chest timing fixture must load.");
        LiveWallpaperMapViewport.TryCreateCentered(160, 128, map.Width, map.Height, 168, 136, .5f, out var viewport);
        var variant = 0;
        for (; variant < 300; variant++)
            if (LiveWallpaperJourneyPlanner.Create(map, viewport, 1, variant, allowIslandLife: true)
                .Points.Any(p => p.Action == LiveWallpaperJourneyAction.OpenChest)) break;
        Check(variant < 300, "An installed key chest must be selectable by normal exploration.");
        var simulation = new LiveWallpaperLinkSimulation();
        long? openedAt = null;
        var awarded = false;
        for (var frame = 0; frame < 4000; frame++)
        {
            var time = variant * 20_000L + frame * 17L;
            var state = simulation.UpdateJourney(1, 0, time, true, map, viewport, allowIslandLife: true);
            if (state.Action == LiveWallpaperLinkRouteAction.OpenChest) openedAt ??= time;
            if (!openedAt.HasValue || time - openedAt.Value < ChestGameplayPresentation.OpeningMilliseconds)
                Check(map.DungeonDoors.SmallKeyCount == 0, "A key must not arrive before the chest finishes opening.");
            if (state.Action != LiveWallpaperLinkRouteAction.ShowItem) continue;
            Check(map.DungeonDoors.SmallKeyCount == 1 && state.ChestItemSpriteId == "smallkey",
                "The chest's actual key must be awarded when its native item presentation begins.");
            awarded = true;
            break;
        }
        Check(openedAt.HasValue && awarded, "Normal exploration must open and collect a key chest, not just present its sprite.");
    }

    private static void CheckUnlockRoute()
    {
        var map = Room("2;96;48;1;lock;;lock", "5;32;16;smallkeyChest;one;chest", "6;0;0;one",
            "0;96;16", "0;96;32", "0;96;64", "0;96;80", "0;96;96");
        LiveWallpaperMapViewport.TryCreateCentered(1200, 2608, map.Width, map.Height,
            40, 60, .5f, out var viewport);
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(40, 60);
        simulation.UpdateJourney(1, 0, 0, true, map, viewport, false, allowViewportFollow: true);
        Check(!LiveWallpaperJourneyPlanner.TryCreateUnlockPlan(map, viewport, 40, 60, out _),
            "Routes must not propose a locked-door interaction without the key.");
        map.DungeonDoors.CollectChest(map, map.GetChestKey(32, 16));
        Check(LiveWallpaperJourneyPlanner.TryCreateUnlockPlan(map, viewport, 40, 60, out var approach) &&
              approach.Points[^1].Action == LiveWallpaperJourneyAction.UnlockDoor,
            "With a key, plan to one reachable lock, not through several hypothetical open doors.");
        Check(simulation.TryWalkTo(map, viewport, 128, 60), "The inaccessible tap must first approach the reachable lock.");
        long? firstPush = null;
        var arrived = false;
        for (var frame = 1; frame <= 900; frame++)
        {
            var time = frame * 17L;
            var state = simulation.UpdateJourney(1, 0, time, true, map, viewport, false, allowViewportFollow: true);
            if (state.Action == LiveWallpaperLinkRouteAction.Pushing) firstPush ??= time;
            if (firstPush.HasValue && time - firstPush.Value < DungeonDoorGameplay.UnlockPushMilliseconds)
                Check(map.DungeonDoors.SmallKeyCount == 1, "Door pushes must honor the native inertia before consuming a key.");
            if (map.DungeonDoors.Doors[0].BlocksMovement)
                Check(state.MapX * 16 + 4 <= 96, "Link cannot walk through the opening door before collision clears.");
            if (state.MapX * 16 > 122)
            {
                arrived = true;
                break;
            }
        }
        Check(firstPush.HasValue && arrived && map.DungeonDoors.SmallKeyCount == 0,
            "Link must push, spend one key, wait for the gate, and resume the original tapped destination.");
    }

    private static void CheckInstalled()
    {
        var root = Environment.GetEnvironmentVariable("LADXHD_TEST_GAME_DATA");
        if (string.IsNullOrWhiteSpace(root)) return;
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "Maps"), "*.map"))
        {
            using var reader = File.OpenText(file);
            Check(LiveWallpaperMap.TryLoad(reader, out var map), "Installed map must load with dungeon doors.");
            var expected = map.Objects.Count(o => o.Template == "ddoor" &&
                o.Arguments.Count > 1 && !string.IsNullOrEmpty(o.Arguments[1]));
            Check(map.DungeonDoors.Doors.Count == expected, "Every valid installed ddoor must be represented, including blank mode0.");
            count += expected;
        }
        Check(count > 0, "Installed door coverage cannot be empty.");
        Console.WriteLine($"Installed dungeon doors checked: {count}");
    }

    private static LiveWallpaperMap Room(params string[] objects)
    {
        var text = new StringBuilder("3\n0\n0\ndungeon.png\n10\n8\n1\n");
        for (var y = 0; y < 8; y++) text.AppendLine(string.Join(',', Enumerable.Repeat("0", 10)));
        text.Append("7\nc1\nmoveStone\nddoor\nkeysetter\nkeyConditionSetter\nchest\ndungeon\n")
            .AppendLine((objects.Length + 32).ToString());
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
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map), "Door fixture must load.");
        return map;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
