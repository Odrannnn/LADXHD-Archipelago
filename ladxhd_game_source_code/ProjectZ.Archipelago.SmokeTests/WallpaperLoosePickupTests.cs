using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperLoosePickupTests
{
    private static readonly Rectangle SmallKeySource = new(64, 21, 7, 13);
    private const float WaterEffectFixtureDuration = 300;

    public static void Run()
    {
        CheckDirectPickupAndDuplicateSaveKey();
        CheckSpawnerConditionAndBlockEvent();
        CheckEnemyDropMotionAndLifetime();
        CheckDeepWaterDropLifecycle();
        CheckDeepWaterTimingAndProbe();
        CheckDeepWaterLandingAndSplashPosition();
        CheckWaterLossRefreshAndCleanup();
        Console.WriteLine("Wallpaper loose-pickup checks passed.");
    }

    private static void CheckDirectPickupAndDuplicateSaveKey()
    {
        var map = Room(
            "0;64;64;d;first;smallkey;one",
            "0;96;64;d;first;smallkey;one",
            "0;128;64;d;high;smallkey;one");
        var doors = map.DungeonDoors;
        Check(doors.LooseKeys.Count == 3 && doors.LooseKeys.All(item => item.Active && !item.Visible),
            "A supported loose key requires its real sprite rectangle before it can render or collect.");
        foreach (var item in doors.LooseKeys) item.SetSpriteRectangle(SmallKeySource);
        var first = doors.LooseKeys[0];
        Check(!doors.CollectLooseKeys(first.X - 4, first.Y - 10, 8, 10, 0, false),
            "A falling d-mode key cannot be picked up before it lands.");
        AdvanceToLand(map, 180);
        Check(first.CanCollect && first.Visible,
            "The installed small-key source rectangle must make a landed direct key visible and collectable.");
        Check(doors.CollectLooseKeys(first.X - 4, first.Y - 10, 8, 10, 0, true) == false &&
              !first.Collected,
            "ObjItem rejects collection while Link is falling.");
        Check(doors.CollectLooseKeys(first.X - 4, first.Y - 10, 8, 10, 0, false) &&
              doors.SmallKeyCount == 1 && first.Collected,
            "A direct small key awards exactly one dungeon-local key.");
        Check(!doors.CollectLooseKeys(first.X - 4, first.Y - 10, 8, 10, 0, false) &&
              doors.SmallKeyCount == 1 && doors.LooseKeys.Count(item => item.Collected) == 1,
            "Duplicate native save-key instances cannot award or collect twice.");
        var high = doors.LooseKeys.Single(item => item.SaveKey == "high");
        Check(high.CanCollect && !doors.CollectLooseKeys(high.X - 4, high.Y - 10, 8, 10, 8, false) &&
              !high.Collected,
            "Native item collection uses the strict vertical-distance bound.");
    }

    private static void CheckSpawnerConditionAndBlockEvent()
    {
        var map = Room(
            "2;32;64;15;blockFlag;;;;0",
            "1;96;64;blockFlag;1;item;d.spawned.smallkey.one;true");
        var doors = map.DungeonDoors;
        var item = doors.LooseKeys.Single();
        item.SetSpriteRectangle(SmallKeySource);
        var block = map.Objects.Single(obj => obj.Template == "moveStone");
        Check(!item.Active && !item.Visible,
            "A spawner requiring key value one must stay absent while its default is zero.");
        doors.BlockPushed(block, 2, completed: false);
        Check(!item.Active, "A type-zero block flag must not activate its spawner before push completion.");
        doors.BlockPushed(block, 2, completed: true);
        Check(item.Active, "The actual move-stone completion flag must activate its exact objectSpawner key/value.");
        AdvanceToLand(map, 180);
        Check(doors.CollectLooseKeys(item.X - 4, item.Y - 10, 8, 10, 0, false) &&
              doors.SmallKeyCount == 1,
            "A spawned key must use the same local dungeon bound as a direct item.");
    }

    private static void CheckEnemyDropMotionAndLifetime()
    {
        var map = Room();
        var doors = map.DungeonDoors;
        var ruby = doors.SpawnEnemyDrop("ruby", 100, 120);
        Check(ruby != null && ruby.X == 100 && ruby.Y == 120 && ruby.ItemName == "ruby",
            "Enemy drops must retain the death entity coordinates without a guessed tile offset.");
        ruby.SetSpriteRectangle(new Rectangle(0, 0, 8, 8));
        Check(!doors.CollectLooseKeys(ruby.X - 4, ruby.Y - 10, 8, 10, 0, false),
            "A j-mode enemy drop must not collect before its first landing.");
        AdvanceToLand(map, 180);
        Check(ruby.CanCollect && !doors.CollectLooseKeys(ruby.X - 4, ruby.Y - 10, 8, 10, 0, false) &&
              doors.CollectedDropRupees == 1,
            "Ordinary ruby collection is tracked separately and does not masquerade as a loose key.");
        Check(!doors.CollectLooseKeys(ruby.X - 4, ruby.Y - 10, 8, 10, 0, false) &&
              doors.CollectedDropRupees == 1,
            "A collected ordinary drop cannot be awarded repeatedly during its fade.");

        var heart = doors.SpawnEnemyDrop("heart", 120, 120);
        heart.SetSpriteRectangle(new Rectangle(0, 0, 8, 8));
        var viewport = Viewport(map);
        doors.UpdateLooseKeys(viewport, 60 * 15);
        Check(doors.EnemyDrops.Contains(heart) && heart.Fading && heart.Visible,
            "An uncollected enemy drop must begin its native fade at 15 seconds, not disappear early.");
        doors.UpdateLooseKeys(viewport, 22);
        Check(!doors.EnemyDrops.Contains(heart),
            "An uncollected enemy drop must be removed only after its 350 ms fade completes.");
        Check(doors.SpawnEnemyDrop("fairy", 0, 0) == null,
            "Unsupported fairy actors must not be replaced with an ordinary pickup.");
    }

    private static void CheckDeepWaterDropLifecycle()
    {
        var deepMap = WaterRoom(deep: true, itemY: 67);
        var deepDoors = deepMap.DungeonDoors;
        var sinking = SpawnDrop(deepDoors, "ruby", 96, 67);
        AdvanceUntil(deepMap, () => sinking.CanCollect, 240, 1);
        Check(sinking.CanCollect && sinking.Height == 0,
            "A deep-water drop must first reach the native grounded state without a bounce.");
        AdvanceUntil(deepMap, () => sinking.LostInWater, 20, 1);
        Check(sinking.LostInWater && sinking.WaterEffectVisible && !sinking.CanCollect,
            "A grounded deep-water drop must sink once, show its splash, and lose collection immediately.");
        Check(!deepDoors.CollectLooseKeys(sinking.X - 4, sinking.Y - 10, 8, 10, 0, false) &&
              deepDoors.CollectedDropRupees == 0,
            "A sunk ordinary drop must not leave a ghost pickup.");
        var initialEffectElapsed = sinking.WaterEffectElapsed;
        deepDoors.UpdateLooseKeys(Viewport(deepMap), 1, deepMap);
        Check(sinking.LostInWater && sinking.WaterEffectElapsed > initialEffectElapsed,
            "Repeated refresh/update passes must not resurrect a sunk drop or restart its splash.");

        var collectedMap = WaterRoom(deep: true, itemY: 67);
        var collectedDoors = collectedMap.DungeonDoors;
        var collected = SpawnDrop(collectedDoors, "heart", 96, 67);
        AdvanceUntil(collectedMap, () => collected.CanCollect, 240, 1);
        Check(!collectedDoors.CollectLooseKeys(collected.X - 4, collected.Y - 10, 8, 10, 0, false) &&
              collected.Collected && collectedDoors.CollectedDropHearts == 1,
            "Collection immediately after landing must win over the pending deep-water sink.");
        collectedDoors.UpdateLooseKeys(Viewport(collectedMap), 30, collectedMap);
        Check(!collected.LostInWater,
            "A collected drop must never generate a later water splash.");

        var shallowMap = WaterRoom(deep: false, itemY: 67);
        var shallowDoors = shallowMap.DungeonDoors;
        var shallow = SpawnDrop(shallowDoors, "ruby", 96, 67);
        AdvanceUntil(shallowMap, () => shallow.CanCollect, 240, 1);
        shallowDoors.UpdateLooseKeys(Viewport(shallowMap), 60, shallowMap);
        Check(shallow.CanCollect && !shallow.LostInWater && !shallow.WaterEffectVisible,
            "Shallow water must not sink an ordinary ground drop.");
    }

    private static void CheckDeepWaterTimingAndProbe()
    {
        var arrivals = new List<float>();
        foreach (var hertz in new[] { 15, 30, 60 })
        {
            var map = WaterRoom(deep: true, itemY: 67);
            var item = SpawnDrop(map.DungeonDoors, "ruby", 96, 67);
            var frameScale = 60f / hertz;
            var elapsed = 0f;
            for (var frame = 0; frame < hertz * 5 && !item.LostInWater; frame++)
            {
                map.DungeonDoors.UpdateLooseKeys(Viewport(map), frameScale, map);
                elapsed += 1000f / hertz;
            }
            Check(item.LostInWater,
                "Deep-water drops must sink at every supported wallpaper update rate.");
            arrivals.Add(elapsed);
            map.DungeonDoors.UpdateLooseKeys(Viewport(map), 60f / hertz * 3, map);
            Check(item.WaterEffectVisible,
                "The native water effect duration must not depend on wallpaper frame rate.");
        }
        Check(arrivals.Max() - arrivals.Min() <= 1000f / 15 + .01f,
            "Deep-water landing and the 125 ms sink threshold must remain cadence-stable at 15/30/60 Hz.");

        double remaining = 125;
        DroppedItemMotion.AdvanceDeepWater(ref remaining, grounded: true, deepWater: true, elapsedMilliseconds: 124);
        Check(remaining > 0, "The shared native deep-water timer must not sink before 125 ms.");
        DroppedItemMotion.AdvanceDeepWater(ref remaining, grounded: true, deepWater: true, elapsedMilliseconds: 1);
        Check(remaining <= 0, "The shared native deep-water timer must expire at 125 ms.");
        DroppedItemMotion.AdvanceDeepWater(ref remaining, grounded: false, deepWater: true, elapsedMilliseconds: 1);
        Check(remaining == 125, "Airborne motion must reset the native deep-water timer.");
        DroppedItemMotion.AdvanceDeepWater(ref remaining, grounded: true, deepWater: false, elapsedMilliseconds: 1);
        Check(remaining == 125, "Dry/shallow terrain must reset the native deep-water timer.");
    }

    private static void CheckDeepWaterLandingAndSplashPosition()
    {
        float shallowHeight = .1f, shallowVelocity = -1f;
        var shallowGrounded = false;
        DroppedItemMotion.AdvanceVertical(ref shallowHeight, ref shallowVelocity, ref shallowGrounded);
        Check(shallowGrounded && shallowVelocity > 0,
            "Ordinary ground landing must retain ObjItem's native rebound.");

        float deepHeight = .1f, deepVelocity = -1f;
        var deepGrounded = false;
        DroppedItemMotion.AdvanceVertical(ref deepHeight, ref deepVelocity, ref deepGrounded, deepWater: true);
        Check(deepGrounded && deepHeight == 0 && deepVelocity == 0,
            "Deep-water ground contact must disable the normal item rebound.");

        Check(DroppedItemMotion.WaterSplashPosition(96, 67) == new Point(96, 63),
            "The water splash must use ObjItem's body center: X and Y-4, not a sprite-center guess.");
    }

    private static void CheckWaterLossRefreshAndCleanup()
    {
        var map = WaterRoom(deep: true, itemY: 67,
            "2;32;64;15;refreshFlag;;;;0",
            "0;96;56;d;waterKey;smallkey;one");
        var doors = map.DungeonDoors;
        var key = doors.LooseKeys.Single();
        key.SetSpriteRectangle(SmallKeySource);
        AdvanceUntil(map, () => key.LostInWater, 240, 1);
        var block = map.Objects.Single(obj => obj.Template == "moveStone");
        doors.BlockPushed(block, 2, completed: true);
        Check(key.LostInWater && !key.CanCollect &&
              !doors.CollectLooseKeys(key.X - 4, key.Y - 10, 8, 10, 0, false),
            "The actual BlockPushed/RefreshDoors path must not resurrect a sunk loose key.");

        var noDurationMap = WaterRoom(deep: true, itemY: 67);
        var noDurationDoors = noDurationMap.DungeonDoors;
        var noDuration = noDurationDoors.SpawnEnemyDrop("ruby", 96, 67);
        noDuration.SetSpriteRectangle(new Rectangle(0, 0, 8, 8));
        AdvanceUntil(noDurationMap, () => noDuration.LostInWater, 240, 1);
        Check(!noDuration.WaterEffectVisible && !noDurationDoors.EnemyDrops.Contains(noDuration),
            "A missing animation duration must discard the native splash instead of inventing a visible lifetime.");

        var cleanupMap = WaterRoom(deep: true, itemY: 67);
        var cleanupDoors = cleanupMap.DungeonDoors;
        var cleanup = SpawnDrop(cleanupDoors, "heart", 96, 67);
        AdvanceUntil(cleanupMap, () => cleanup.LostInWater, 240, 1);
        cleanupDoors.UpdateLooseKeys(Viewport(cleanupMap), WaterEffectFixtureDuration / 1000f * 60 + 1,
            cleanupMap);
        Check(!cleanupDoors.EnemyDrops.Contains(cleanup),
            "A completed water effect must clean up its lost ordinary drop.");
    }

    private static LiveWallpaperItemPickup SpawnDrop(
        LiveWallpaperDungeonDoors doors, string itemName, float x, float y)
    {
        var item = doors.SpawnEnemyDrop(itemName, x, y);
        Check(item != null, "The ordinary drop fixture must create its supported item.");
        item.SetSpriteRectangle(new Rectangle(0, 0, 8, 8));
        item.SetWaterEffectDuration(WaterEffectFixtureDuration);
        return item;
    }

    private static void AdvanceUntil(
        LiveWallpaperMap map, Func<bool> condition, int maximumFrames, float frames)
    {
        for (var frame = 0; frame < maximumFrames && !condition(); frame++)
            map.DungeonDoors.UpdateLooseKeys(Viewport(map), frames, map);
        Check(condition(), "Drop fixture did not reach its expected native state in the bounded update window.");
    }

    private static void AdvanceToLand(LiveWallpaperMap map, float frames)
    {
        map.DungeonDoors.UpdateLooseKeys(Viewport(map), frames);
    }

    private static LiveWallpaperMapViewport Viewport(LiveWallpaperMap map)
    {
        Check(LiveWallpaperMapViewport.TryCreateCentered(192, 160, map.Width, map.Height,
            96, 80, .5f, out var viewport), "Pickup fixture viewport must load.");
        return viewport;
    }

    private static LiveWallpaperMap Room(params string[] objects)
    {
        var text = new StringBuilder("3\n0\n0\ndungeon.png\n12\n10\n1\n");
        for (var row = 0; row < 10; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", 12)));
        text.Append("6\nitem\nobjectSpawner\nmoveStone\ndungeon\nwater\nwaterDeep\n")
            .AppendLine((objects.Length + 1).ToString());
        foreach (var obj in objects) text.AppendLine(obj);
        text.AppendLine("3;0;0;one");
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map),
            "Loose-pickup fixture must load.");
        return map;
    }

    // The item body probes its map position at Y-3.  With the item at 67, the
    // deep tile begins exactly at 64: this catches a guessed sprite-center probe.
    private static LiveWallpaperMap WaterRoom(bool deep, int itemY, params string[] objects)
    {
        var tileY = (int)MathF.Floor((itemY - 3) / 16f) * 16;
        return Room(objects.Concat(new[] { $"{(deep ? 5 : 4)};96;{tileY}" }).ToArray());
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
