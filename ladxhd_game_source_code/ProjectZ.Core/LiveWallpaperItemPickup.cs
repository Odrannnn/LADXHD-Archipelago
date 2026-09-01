using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ;

// A supported ObjItem, not a replacement script interpreter or a saved inventory.
public sealed class LiveWallpaperItemPickup
{
    public float X { get; internal set; }
    public float Y { get; internal set; }
    public string ItemName { get; }
    public string SpriteId => ItemName == "ruby" ? "rubyBlue" : ItemName;
    public bool Despawn { get; }
    public string SaveKey { get; }
    public string LocationBound { get; }
    internal string SpawnKey { get; }
    internal string SpawnValue { get; }
    internal bool CanDespawn { get; }
    private readonly string _mode;
    private bool _spawned;
    private bool _grounded;
    private bool _landed;
    private float _height;
    private float _velocity;
    private float _frames;
    private Rectangle _collection;
    private bool _hasSprite;
    private long _ticks;
    private bool _expired;
    private double _deepWaterCounter;
    private float _waterEffectDuration;
    public bool Active { get; private set; }
    public bool Collected { get; private set; }
    public bool LostInWater { get; private set; }
    public float WaterEffectElapsed { get; private set; }
    public bool WaterEffectVisible => Active && LostInWater && WaterEffectElapsed < _waterEffectDuration;
    public float Height => _height;
    public float CollectionElapsed { get; private set; }
    public bool Fading => Collected || _expired;
    public bool Visible => Active && !LostInWater && _hasSprite && (!Fading ||
        CollectionElapsed <= DroppedItemMotion.CollectionDespawnMilliseconds);
    public bool CanCollect => Active && !LostInWater && _hasSprite && !Fading && _landed;

    internal LiveWallpaperItemPickup(int x, int y, string mode, string saveKey, string bound,
        string spawnKey, string spawnValue, bool canDespawn, string itemName = "smallkey", bool despawn = false)
    {
        ItemName = itemName;
        Despawn = despawn;
        var position = DroppedItemMotion.ItemPosition(x, y);
        X = position.X;
        Y = position.Y;
        _mode = mode;
        SaveKey = saveKey;
        LocationBound = bound ?? "";
        SpawnKey = spawnKey;
        SpawnValue = string.IsNullOrEmpty(spawnValue) ? "0" : spawnValue;
        CanDespawn = canDespawn;
        Reset();
    }

    internal void Reset()
    {
        Active = Collected = _spawned = _expired = LostInWater = false;
        _grounded = _landed = string.IsNullOrEmpty(_mode);
        _height = _mode == "d" ? DroppedItemMotion.ItemDropHeight : 0;
        _velocity = _mode == "j" ? DroppedItemMotion.ItemJumpVelocity : 0;
        _frames = CollectionElapsed = WaterEffectElapsed = 0;
        _ticks = 0;
        _deepWaterCounter = 0;
    }

    public void SetWaterEffectDuration(float milliseconds) => _waterEffectDuration = Math.Max(0, milliseconds);

    public void SetSpriteRectangle(Rectangle source)
    {
        _hasSprite = source.Width > 0 && source.Height > 0;
        // Reuse GameItem's collection rectangle, including the atlas-width origin.
        _collection = (ItemName == "smallkey" ? new GameItem(collectWidth: DungeonDoorGameplay.SmallKeyCollectWidth,
            collectHeight: DungeonDoorGameplay.SmallKeyCollectHeight,
            collectOffsetX: DungeonDoorGameplay.SmallKeyCollectOffsetX) :
            ItemManager.CreateOrdinaryDrop(ItemName, null, false)).CreateCollectRectangle(source);
    }

    internal void Refresh(Func<string, string> readKey)
    {
        if (Collected || LostInWater) return; // Removed instances cannot respawn on a condition refresh.
        if (!string.IsNullOrEmpty(SaveKey) && readKey(SaveKey) == "1")
        {
            Active = false;
            return;
        }
        var matches = string.IsNullOrEmpty(SpawnKey) || (readKey(SpawnKey) ?? "0") == SpawnValue;
        if (matches) _spawned = true;
        Active = matches || _spawned && !CanDespawn;
    }

    internal void Advance(float frames, bool deepWater = false)
    {
        if (!Active || frames <= 0) return;
        _frames += frames;
        while (_frames >= 1)
        {
            _frames--;
            AdvanceFrame(deepWater);
        }
    }

    private void AdvanceFrame(bool deepWater)
    {
        const float milliseconds = 1000f / DroppedItemMotion.FramesPerSecond;
        if (LostInWater)
        {
            WaterEffectElapsed = Math.Min(_waterEffectDuration + 1, WaterEffectElapsed + milliseconds);
            return;
        }
        _ticks++;
        if (Despawn && !Fading && _ticks * 1000d / DroppedItemMotion.FramesPerSecond >=
            DroppedItemMotion.UncollectedDespawnMilliseconds)
        {
            _expired = true;
            CollectionElapsed = 0;
            return;
        }
        if (Fading)
        {
            CollectionElapsed = Math.Min(DroppedItemMotion.CollectionDespawnMilliseconds + 1,
                CollectionElapsed + milliseconds);
            return;
        }
        // ObjectManager updates ObjItem's AI before its body: observe the
        // previous landing first, then integrate this tick's vertical motion.
        _landed |= _grounded;
        if (DroppedItemMotion.AdvanceDeepWater(ref _deepWaterCounter, _grounded, deepWater, milliseconds))
        {
            LostInWater = true;
            WaterEffectElapsed = 0;
            return;
        }
        if (!_grounded || _velocity != 0)
            DroppedItemMotion.AdvanceVertical(ref _height, ref _velocity, ref _grounded, deepWater);
    }

    public bool Intersects(float x, float y, float width, float height) =>
        x < X + _collection.Right && x + width > X + _collection.X &&
        y < Y + _collection.Bottom && y + height > Y + _collection.Y;

    internal bool TryCollect(float x, float y, float width, float height, float z, bool falling)
    {
        // ObjItem.OnCollision: no pickup while falling; height difference is strict.
        if (!CanCollect || falling || Math.Abs(Height - z) >= 8 || !Intersects(x, y, width, height)) return false;
        Collected = true;
        CollectionElapsed = 0;
        return true;
    }
}
