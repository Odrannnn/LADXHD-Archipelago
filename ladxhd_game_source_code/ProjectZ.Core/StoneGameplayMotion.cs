using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    /// <summary>
    /// Canonical carried-stone values shared by ObjLink, ObjStone, and the
    /// lightweight wallpaper simulation.
    /// </summary>
    public static class StoneGameplayMotion
    {
        public const int CarryHeight = 13;
        public const float ThrowSpeed = 3f;
        public const float Gravity = -0.125f;
        public const int ThrowFlightFrames = 14;
        public const long ThrowFlightMilliseconds = 234L;

        public static Vector2 CreateThrowVelocity(int direction)
        {
            var cardinal = direction switch
            {
                0 => new Vector2(-1f, 0f),
                1 => new Vector2(0f, -1f),
                2 => new Vector2(1f, 0f),
                _ => new Vector2(0f, 1f)
            };
            return cardinal * ThrowSpeed;
        }

        public static float ResolveHeight(int frame)
        {
            var height = (float)CarryHeight;
            var velocity = 0f;
            for (var index = 0; index < Math.Max(0, frame) && height > 0f;
                 index++)
            {
                velocity = LinkGameplayMotion.ApplyGravity(
                    velocity, Gravity, 1f);
                height = Math.Max(0f, height + velocity);
            }
            return height;
        }
    }
}
