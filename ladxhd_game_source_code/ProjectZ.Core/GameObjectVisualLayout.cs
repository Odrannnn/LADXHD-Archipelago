using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    /// <summary>
    /// Canonical sprite transforms shared by the game objects and non-gameplay
    /// renderers. Keeping these here prevents Android wallpaper placement from
    /// reinterpreting object anchors independently from the actual game.
    /// </summary>
    public static class GameObjectVisualLayout
    {
        private static readonly (int X, int Y, bool FlipX, bool FlipY)[,]
            ClassicLeafFrames =
        {
            { (-4, 2, false, false), (4, -5, true, true), (6, 5, false, false), (10, 1, true, false) },
            { (-1, 1, false, false), (4, -7, true, true), (6, 8, false, false), (7, 2, true, false) },
            { (0, 0, true, false), (2, -8, true, true), (4, 4, true, false), (7, 10, true, false) },
            { (1, -2, true, false), (1, 4, true, true), (5, 4, true, false), (7, 12, true, false) },
            { (0, -3, true, false), (-2, 4, true, true), (8, 8, true, false), (9, 14, true, false) },
            { (-1, -4, false, false), (-6, 4, false, true), (9, 8, true, false), (10, 15, false, false) },
            { (-2, -5, false, false), (-7, 3, false, true), (12, 8, false, false), (11, 17, false, false) },
            { (-3, -6, false, false), (-9, 1, false, true), (13, 9, false, false), (12, 15, false, false) }
        };

        public const int StoneVerticalOffset = 3;
        public const long ClassicLeafAnimationMilliseconds = 8L * 34L;
        public const long ClassicLeafFadeMilliseconds = 120L;

        public static Vector2 GetStoneEntityPosition(float mapX, float mapY) =>
            new(mapX + 8f, mapY + 16f - StoneVerticalOffset);

        public static Vector2 GetStoneMapPosition(float entityX, float entityY) =>
            new(entityX - 8f, entityY - 16f + StoneVerticalOffset);

        public static Vector2 GetStoneSpriteOffset(
            float sourceWidth, float sourceHeight) =>
            new(-MathF.Floor(sourceWidth / 2f),
                -sourceHeight + StoneVerticalOffset);

        /// <summary>ObjLeafClassic's exact eight-frame spline and fade.</summary>
        public static bool TryGetClassicLeafState(
            int leafIndex, long elapsedMilliseconds,
            out Vector2 offset, out bool flipX, out bool flipY, out float alpha)
        {
            offset = Vector2.Zero;
            flipX = false;
            flipY = false;
            alpha = 1f;
            if (elapsedMilliseconds < 0 || elapsedMilliseconds >=
                ClassicLeafAnimationMilliseconds + ClassicLeafFadeMilliseconds)
                return false;
            leafIndex = Math.Clamp(leafIndex, 0, 3);
            var animationTime = Math.Min(
                elapsedMilliseconds, ClassicLeafAnimationMilliseconds);
            var progress = Math.Clamp(
                animationTime / (float)ClassicLeafAnimationMilliseconds, 0f, 1f);
            var scaled = progress * 7f;
            var segment = Math.Min((int)scaled, 6);
            var local = scaled - segment;
            Vector2 Point(int frame)
            {
                var value = ClassicLeafFrames[Math.Clamp(frame, 0, 7), leafIndex];
                return new Vector2(value.X, value.Y);
            }
            offset = CatmullRom(
                Point(segment - 1), Point(segment),
                Point(segment + 1), Point(segment + 2), local);
            var visual = ClassicLeafFrames[segment, leafIndex];
            flipX = visual.FlipX;
            flipY = visual.FlipY;
            if (elapsedMilliseconds > ClassicLeafAnimationMilliseconds)
                alpha = Math.Clamp(1f -
                    (elapsedMilliseconds - ClassicLeafAnimationMilliseconds) /
                    (float)ClassicLeafFadeMilliseconds, 0f, 1f);
            return true;
        }

        private static Vector2 CatmullRom(
            Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
