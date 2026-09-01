using System;
using System.Collections.Generic;

namespace ProjectZ
{
    /// <summary>
    /// Lightweight state for ObjBreakingFloor. The installed object begins as its
    /// exact floor sprite, collapses after 670 ms of grounded Link contact, owns
    /// the same inset hole rectangle, and respawns after 15 seconds.
    /// </summary>
    public sealed class LiveWallpaperBreakingFloors
    {
        public const long BreakMilliseconds = 670L;
        public const long RespawnMilliseconds = 15_000L;

        private sealed class Floor
        {
            public Floor(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
            public float BreakCounter { get; set; }
            public long BrokenAt { get; set; } = -1L;
            public bool Broken => BrokenAt >= 0L;
        }

        private readonly List<Floor> _floors = [];
        private readonly Dictionary<(int X, int Y), Floor> _byPosition = [];

        public LiveWallpaperBreakingFloors(
            IReadOnlyList<LiveWallpaperMapObject> objects)
        {
            if (objects == null)
                return;
            foreach (var mapObject in objects)
            {
                if (!TryGetSpriteId(mapObject.Template, out _))
                    continue;
                var floor = new Floor(mapObject.PixelX, mapObject.PixelY);
                _floors.Add(floor);
                _byPosition[(floor.X, floor.Y)] = floor;
            }
        }

        public int Count => _floors.Count;

        public void Reset()
        {
            foreach (var floor in _floors)
            {
                floor.BreakCounter = 0f;
                floor.BrokenAt = -1L;
            }
        }

        public bool Advance(
            float bodyX, float bodyY, float bodyWidth, float bodyHeight,
            long elapsedDelta, long elapsedMilliseconds,
            bool canTrigger = true)
        {
            var changed = false;
            var delta = Math.Max(0L, elapsedDelta);
            foreach (var floor in _floors)
            {
                if (floor.Broken)
                {
                    if (elapsedMilliseconds - floor.BrokenAt <
                        RespawnMilliseconds)
                        continue;
                    floor.BrokenAt = -1L;
                    floor.BreakCounter = 0f;
                    changed = true;
                    continue;
                }

                // ObjBreakingFloor uses (x, y + 4, 16, 8) for Link contact.
                var touching = canTrigger && bodyWidth > 0f && bodyHeight > 0f &&
                    bodyX < floor.X + 16f && bodyX + bodyWidth > floor.X &&
                    bodyY < floor.Y + 12f && bodyY + bodyHeight > floor.Y + 4f;
                if (!touching)
                {
                    floor.BreakCounter = Math.Max(
                        0f, floor.BreakCounter - delta * 1.5f);
                    continue;
                }

                floor.BreakCounter += delta;
                if (floor.BreakCounter < BreakMilliseconds)
                    continue;
                floor.BrokenAt = elapsedMilliseconds;
                changed = true;
            }
            return changed;
        }

        public bool IsBrokenAt(int pixelX, int pixelY) =>
            _byPosition.TryGetValue((pixelX, pixelY), out var floor) &&
            floor.Broken;

        public static bool IsBreakingFloorSprite(string spriteId) =>
            spriteId is "breaking_floor_0" or "breaking_floor_1" or
                "breaking_floor_2" or "breaking_floor_3" or
                "breaking_floor_4" or "breaking_floor_5" or
                "breaking_floor_6" or "breaking_floor_7" or
                "breaking_floor_8";

        public static bool TryGetSpriteId(
            string template, out string spriteId)
        {
            spriteId = template switch
            {
                "caveBreakingFloor" => "breaking_floor_0",
                "caveBreakingFloor2" => "breaking_floor_1",
                "caveBreakingFloor3" => "breaking_floor_2",
                "dungeonHole" => "breaking_floor_3",
                "breakingFloorCastle" => "breaking_floor_4",
                "dungeon5BreakingFloor" => "breaking_floor_5",
                "dungeon2BreakingFloor" => "breaking_floor_6",
                "dungeon8BreakingFloor" => "breaking_floor_7",
                "breakingFloorHouse" => "breaking_floor_8",
                _ => null
            };
            return spriteId != null;
        }
    }
}
