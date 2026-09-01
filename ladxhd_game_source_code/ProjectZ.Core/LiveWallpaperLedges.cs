using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public readonly struct LiveWallpaperLedge
    {
        internal LiveWallpaperLedge(int index, int pixelX, int pixelY,
            int offsetX, int offsetY, int width, int height,
            float jumpHeight, float jumpSpeed, int inertiaMilliseconds,
            bool ignoreCollision, bool moveOnTop)
        {
            Index = index;
            PixelX = pixelX;
            PixelY = pixelY;
            Offset = new Vector2(offsetX, offsetY);
            Width = width;
            Height = height;
            JumpHeightMultiplier = jumpHeight *
                                   RailJumpGameplay.GetHeightMultiplier(Offset);
            JumpSpeedMultiplier = jumpSpeed *
                                  RailJumpGameplay.GetSpeedMultiplier(Offset);
            InertiaMilliseconds = inertiaMilliseconds;
            IgnoreCollision = ignoreCollision;
            MoveOnTop = moveOnTop;
            Direction = RailJumpGameplay.GetDirection(Offset);
        }

        public int Index { get; }
        public int PixelX { get; }
        public int PixelY { get; }
        public Vector2 Offset { get; }
        public int Width { get; }
        public int Height { get; }
        public float JumpHeightMultiplier { get; }
        public float JumpSpeedMultiplier { get; }
        public int InertiaMilliseconds { get; }
        public bool IgnoreCollision { get; }
        public bool MoveOnTop { get; }
        public int Direction { get; }

        public Vector2 GetGoal(Vector2 playerPosition,
            float bodyWidth, float bodyHeight) =>
            RailJumpGameplay.GetGoal(playerPosition,
                PixelX, PixelY, Width, Height, Offset,
                bodyWidth, bodyHeight);
    }

    internal static class LiveWallpaperLedges
    {
        public static LiveWallpaperLedge[] Parse(
            IReadOnlyList<LiveWallpaperMapObject> objects)
        {
            if (objects == null || objects.Count == 0)
                return [];
            var ledges = new List<LiveWallpaperLedge>();
            for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++)
            {
                var obj = objects[objectIndex];
                if (!string.Equals(obj.Template, "jump", StringComparison.Ordinal))
                    continue;
                var offsetX = GetInt(obj.Arguments, 0, 0);
                var offsetY = GetInt(obj.Arguments, 1, 0);
                if (offsetX == 0 && offsetY == 0)
                    continue;
                var width = Math.Max(1, GetInt(obj.Arguments, 2, 16));
                var height = Math.Max(1, GetInt(obj.Arguments, 3, 16));
                ledges.Add(new LiveWallpaperLedge(
                    ledges.Count, obj.PixelX, obj.PixelY,
                    offsetX, offsetY, width, height,
                    GetFloat(obj.Arguments, 4, 1f),
                    GetFloat(obj.Arguments, 5, 1f),
                    Math.Max(0, GetInt(obj.Arguments, 6, 0)),
                    GetBool(obj.Arguments, 7, false),
                    GetBool(obj.Arguments, 8, false)));
            }
            return ledges.ToArray();
        }

        private static int GetInt(IReadOnlyList<string> values, int index, int fallback) =>
            index >= 0 && index < values.Count && int.TryParse(
                values[index], NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var value)
                ? value
                : fallback;

        private static float GetFloat(IReadOnlyList<string> values, int index, float fallback) =>
            index >= 0 && index < values.Count && float.TryParse(
                values[index], NumberStyles.Float, CultureInfo.InvariantCulture,
                out var value)
                ? value
                : fallback;

        private static bool GetBool(IReadOnlyList<string> values, int index, bool fallback) =>
            index >= 0 && index < values.Count && bool.TryParse(values[index], out var value)
                ? value
                : fallback;
    }
}
