using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ;

// Runtime-only state owned by one loaded wallpaper map. No SaveManager, scripts,
// audio or AP session. Navigation copies share these live collision states.
public sealed class LiveWallpaperDungeonDoors
{
    public sealed class Door
    {
        internal Door(LiveWallpaperMapObject source)
        {
            X = source.PixelX;
            Y = source.PixelY;
            Mode = Number(source, 0);
            Key = Text(source, 1);
            Direction = Number(source, 2);
            PushKey = Text(source, 3);
        }
        public int X { get; }
        public int Y { get; }
        public int Mode { get; }
        public int Direction { get; }
        public string Key { get; }
        public string PushKey { get; }
        public float Amount { get; internal set; } = 1;
        public bool BlocksMovement { get; internal set; } = true;
        internal bool WantsOpen;
    }

    private sealed class Setter
    {
        public string Key;
        public ConditionNode Condition;
        public bool Reset;
        public bool Initialized;
        public bool Active;
    }

    private readonly IReadOnlyList<LiveWallpaperMapObject> _objects;
    private readonly Dictionary<string, string> _keys = new(StringComparer.Ordinal);
    private readonly List<Setter> _setters = new();
    private readonly Dictionary<(int X, int Y), List<Door>> _at = new();
    private readonly Dictionary<string, int> _smallKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nightmareKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _collectedKeyChests = new(StringComparer.Ordinal);
    private readonly string _location;
    private readonly Func<string, string> _readKey;
    private bool _animating;
    public IReadOnlyList<Door> Doors { get; }
    public IReadOnlyList<LiveWallpaperItemPickup> LooseKeys { get; }
    public List<LiveWallpaperItemPickup> EnemyDrops { get; } = new();
    public int CollectedDropRupees { get; private set; }
    public int CollectedDropHearts { get; private set; }

    public LiveWallpaperDungeonDoors(IReadOnlyList<LiveWallpaperMapObject> objects)
    {
        _objects = objects;
        _location = objects.Where(o => o.Template == "dungeon").Select(o => Text(o, 0))
            .LastOrDefault(name => name != null) ?? "";
        _readKey = ReadKey;
        Doors = objects.Where(o => o.Template == "ddoor" && Text(o, 1) != null)
            .Select(o => new Door(o)).ToArray();
        var looseKeys = new List<LiveWallpaperItemPickup>();
        foreach (var obj in objects)
        {
            var item = obj;
            var spawner = obj.Template == "objectSpawner" && Text(obj, 2) == "item";
            if (spawner)
            {
                // ObjObjectSpawner's '.' separator / '$' escape, not a guessed item name.
                var args = (Text(obj, 3) ?? "").Split('.');
                for (var i = 0; i < args.Length; i++) args[i] = args[i].Replace('$', '.');
                item = new LiveWallpaperMapObject("item", obj.PixelX, obj.PixelY, args);
            }
            if (item.Template != "item" || Text(item, 2) != "smallkey" ||
                Text(item, 0) is not (null or "j" or "d")) continue;
            looseKeys.Add(new LiveWallpaperItemPickup(item.PixelX, item.PixelY,
                Text(item, 0), Text(item, 1), Text(item, 3),
                spawner ? Text(obj, 0) : null, spawner ? Text(obj, 1) : null,
                spawner && (!bool.TryParse(Text(obj, 4), out var despawn) || despawn)));
        }
        LooseKeys = looseKeys;
        foreach (var door in Doors)
        {
            if (!_at.TryGetValue((door.X, door.Y), out var entries))
                _at[(door.X, door.Y)] = entries = new();
            entries.Add(door);
        }
        foreach (var obj in objects)
        {
            if (obj.Template != "keyConditionSetter" || Text(obj, 0) == null || Text(obj, 1) == null)
                continue;
            try
            {
                _setters.Add(new Setter
                {
                    Key = Text(obj, 0), Condition = SaveCondition.GetConditionNode(Text(obj, 1)),
                    Reset = !bool.TryParse(Text(obj, 2), out var reset) || reset
                });
            }
            catch (ArgumentException) { /* Unsupported/malformed installed condition: leave its gate closed. */ }
            catch (FormatException) { }
            catch (KeyNotFoundException) { }
        }
        Reset();
    }

    public bool BlocksAt(int x, int y) => _at.TryGetValue((x, y), out var doors) &&
        doors.Any(door => door.BlocksMovement);

    public void Reset()
    {
        _smallKeys.Clear();
        _nightmareKeys.Clear();
        _collectedKeyChests.Clear();
        EnemyDrops.Clear();
        CollectedDropRupees = CollectedDropHearts = 0;
        foreach (var item in LooseKeys) item.Reset();
        _keys.Clear();
        foreach (var setter in _setters) setter.Initialized = false;
        // Constructors execute in installed object order. Conditions run after
        // initialization, as key listeners do when the map finishes loading.
        foreach (var obj in _objects)
        {
            if (obj.Template == "keysetter" && Text(obj, 0) is { } key)
                _keys[key] = Text(obj, 1);
            if (!LiveWallpaperMap.IsMoveStoneTemplate(obj.Template)) continue;
            var blockKey = Text(obj, 1);
            if (blockKey != null && Number(obj, 5) == 1) _keys[blockKey] = "0";
            _keys[(blockKey ?? "") + "_dir"] = "-1";
        }
        EvaluateSetters();
        RefreshLooseKeys();
        foreach (var door in Doors)
        {
            door.WantsOpen = DungeonDoorGameplay.IsOpenKey(ReadKey(door.Key));
            door.Amount = door.WantsOpen ? 0 : 1;
            door.BlocksMovement = !door.WantsOpen;
        }
        _animating = false;
    }

    private string ReadKey(string key) => _keys.TryGetValue(key, out var value) ? value : null;

    public int SmallKeyCount => _smallKeys.GetValueOrDefault("") +
        (_location.Length == 0 ? 0 : _smallKeys.GetValueOrDefault(_location));
    public bool HasNightmareKey => _nightmareKeys.Contains("") || _nightmareKeys.Contains(_location);

    private void RefreshLooseKeys()
    {
        foreach (var item in LooseKeys) item.Refresh(_readKey);
    }

    public bool UpdateLooseKeys(LiveWallpaperMapViewport viewport, float frames, LiveWallpaperMap map = null)
    {
        var ready = false;
        foreach (var item in LooseKeys)
            if (item.X >= viewport.OriginX * 16 - 16 && item.Y >= viewport.OriginY * 16 - 16 &&
                item.X <= (viewport.OriginX + viewport.Columns) * 16 + 16 &&
                item.Y <= (viewport.OriginY + viewport.Rows) * 16 + 16)
            {
                var wasReady = item.CanCollect;
                item.Advance(frames, IsPickupInDeepWater(map, item));
                ready |= !wasReady && item.CanCollect;
            }
        foreach (var item in EnemyDrops)
        {
            var wasReady = item.CanCollect;
            item.Advance(frames, IsPickupInDeepWater(map, item));
            ready |= !wasReady && item.CanCollect;
        }
        EnemyDrops.RemoveAll(item => !item.Active || item.Fading && !item.Visible ||
            item.LostInWater && !item.WaterEffectVisible);
        return ready;
    }

    private static bool IsPickupInDeepWater(LiveWallpaperMap map, LiveWallpaperItemPickup item) =>
        map != null && !map.Is2DMap && map.IsDeepWaterAt(
            item.X + DroppedItemMotion.ItemBodyOffsetX + DroppedItemMotion.ItemBodyWidth / 2f,
            item.Y + DroppedItemMotion.ItemBodyOffsetY + DroppedItemMotion.ItemBodyHeight + BodyComponent.DefaultDeepWaterOffset);

    public bool CollectLooseKeys(float x, float y, float width, float height, float z, bool falling)
    {
        var collected = false;
        foreach (var item in LooseKeys)
        {
            if (!item.TryCollect(x, y, width, height, z, falling)) continue;
            _smallKeys[item.LocationBound] = Math.Min(DungeonDoorGameplay.SmallKeyCapacity,
                _smallKeys.GetValueOrDefault(item.LocationBound) + DungeonDoorGameplay.SmallKeyPickupCount);
            if (!string.IsNullOrEmpty(item.SaveKey)) _keys[item.SaveKey] = "1";
            collected = true;
            // Hide other instances with the same native save key before the next pickup.
            RefreshDoors();
        }
        foreach (var item in EnemyDrops)
        {
            if (!item.TryCollect(x, y, width, height, z, falling)) continue;
            if (item.ItemName == "ruby") CollectedDropRupees++;
            if (item.ItemName == "heart") CollectedDropHearts++;
        }
        return collected;
    }

    public LiveWallpaperItemPickup SpawnEnemyDrop(string itemName, float x, float y)
    {
        // Fairies and powerups have their own actors/behaviors; never substitute a heart.
        if (itemName is not ("ruby" or "heart")) return null;
        var item = new LiveWallpaperItemPickup(0, 0, "j", null, null, null, null, false, itemName, true)
            { X = x, Y = y };
        item.Refresh(_readKey);
        EnemyDrops.Add(item);
        return item;
    }

    // Called at the same point as ObjChest.OpeningEnd, not on opening the lid. Only installed
    // chest contents are awarded, with the same alias, cap and location bound.
    public void CollectChest(LiveWallpaperMap map, int chestKey)
    {
        foreach (var chest in _objects)
        {
            if (chest.Template != "chest" || map.GetChestKey(chest.PixelX, chest.PixelY) != chestKey)
                continue;
            var item = Text(chest, 0);
            if (item is not ("smallkey" or "smallkeyChest" or "nightmarekey")) return;
            var identity = Text(chest, 2) is { } key ? "key:" + key : "position:" + chestKey;
            if (!_collectedKeyChests.Add(identity)) return;
            var bound = Text(chest, 1) ?? "";
            if (item == "nightmarekey") _nightmareKeys.Add(bound);
            else _smallKeys[bound] = Math.Min(DungeonDoorGameplay.SmallKeyCapacity,
                _smallKeys.GetValueOrDefault(bound) + DungeonDoorGameplay.SmallKeyPickupCount);
            return;
        }
    }

    public bool CanUnlock(Door door)
    {
        if (door == null || door.WantsOpen || door.Amount != 1 || string.IsNullOrEmpty(door.PushKey) ||
            !DungeonDoorGameplay.HasRequiredKey(door.Mode,
                door.Mode == 1 ? SmallKeyCount : HasNightmareKey ? 0 : null)) return false;
        if (door.Key == door.PushKey) return true;
        // Only plan a conditional lock when the installed expression already
        // allows its actual push flag to open it. Unimplemented script prerequisites
        // remain blocked; never substitute the door's own key for the push key.
        return _setters.Any(setter => setter.Key == door.Key &&
            setter.Condition.Check(key => key == door.PushKey ? "1" : ReadKey(key)));
    }

    public bool TryUnlock(Door door)
    {
        if (!Doors.Contains(door) || !CanUnlock(door)) return false;
        if (door.Mode == 1)
        {
            var bound = _smallKeys.GetValueOrDefault("") > 0 ? "" : _location;
            _smallKeys[bound]--;
        }
        // Nightmare-key ownership is retained even though its native count is zero.
        _keys[door.PushKey] = "1";
        RefreshDoors();
        return true;
    }

    // ObjMoveStone sets type 1 keys at push start and type 0 keys at completion.
    // No other key is guessed from a door name, position or dungeon number.
    public void BlockPushed(LiveWallpaperMapObject block, int direction, bool completed)
    {
        if (Doors.Count == 0 && LooseKeys.Count == 0) return;
        var type = Number(block, 5);
        var key = Text(block, 1);
        if (!completed && Text(block, 7) is { } resetKey) _keys[resetKey] = "0";
        if (type == (completed ? 0 : 1) && key != null)
        {
            _keys[key] = "1";
            _keys[key + "_dir"] = direction.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        RefreshDoors();
    }

    private void RefreshDoors()
    {
        EvaluateSetters();
        RefreshLooseKeys();
        foreach (var door in Doors)
        {
            var open = DungeonDoorGameplay.IsOpenKey(ReadKey(door.Key));
            if (open == door.WantsOpen) continue;
            door.WantsOpen = open;
            // Close() makes the complete body solid immediately, before animation.
            if (!open) door.BlocksMovement = true;
            _animating = true;
        }
    }

    private void EvaluateSetters()
    {
        // Reuse the actual condition tree with an isolated key reader. Evaluate
        // only on events; bounded propagation prevents malformed cycles hanging
        // a wallpaper frame. No general script interpreter is run in the background.
        for (var pass = 0; pass <= _setters.Count; pass++)
        {
            var changed = false;
            foreach (var setter in _setters)
            {
                var active = setter.Condition.Check(_readKey);
                if (setter.Initialized && active == setter.Active) continue;
                setter.Initialized = true;
                if (!active && !setter.Reset) continue;
                setter.Active = active;
                var value = active ? "1" : "0";
                if (ReadKey(setter.Key) == value) continue;
                _keys[setter.Key] = value;
                changed = true;
            }
            if (!changed) break;
        }
    }

    public void Advance(float frames)
    {
        if (!_animating || frames <= 0) return;
        _animating = false;
        foreach (var door in Doors)
        {
            door.Amount = door.WantsOpen
                ? DungeonDoorGameplay.Open(door.Amount, frames)
                : DungeonDoorGameplay.Close(door.Amount, frames);
            if (door.WantsOpen)
                door.BlocksMovement = DungeonDoorGameplay.BlocksWhileOpening(door.Amount);
            _animating |= door.Amount > 0 && door.Amount < 1;
        }
    }

    private static string Text(LiveWallpaperMapObject obj, int index) =>
        index < obj.Arguments.Count && !string.IsNullOrEmpty(obj.Arguments[index])
            ? obj.Arguments[index] : null;
    private static int Number(LiveWallpaperMapObject obj, int index) =>
        int.TryParse(Text(obj, index), out var number) ? number : 0;
}
