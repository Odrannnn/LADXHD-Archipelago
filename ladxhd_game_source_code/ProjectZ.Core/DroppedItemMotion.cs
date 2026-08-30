using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public readonly struct DroppedItemMotionState
    {
        public DroppedItemMotionState(Vector2 offset, float height, bool grounded)
        {
            Offset = offset;
            Height = Math.Max(0f, height);
            Grounded = grounded;
        }

        public Vector2 Offset { get; }
        public float Height { get; }
        public bool Grounded { get; }
    }

    /// <summary>
    /// ObjItem's normal bush-drop launch and body motion at the engine's 60 Hz
    /// update rate. Gameplay and lightweight renderers share these constants.
    /// </summary>
    public static class DroppedItemMotion
    {
        public const float InitialHorizontalVelocity = 0.5f;
        public const float InitialVerticalVelocity = 0.75f;
        public const float Gravity = -0.1f;
        public const float Bounciness = 0.7f;
        public const float GroundDrag = 0.8f;
        public const float AirDrag = 0.9f;
        public const float FramesPerSecond = 60f;
        public const int CollectionDespawnMilliseconds = 350;
        public const int CollectionFadeStartMilliseconds = 250;
        public const int CollectionMoveStopMilliseconds = 250;

        public static void ResolveCollectedVisual(
            long elapsedMilliseconds, out float verticalOffset,
            out float alpha, out bool visible)
        {
            var elapsed = Math.Max(0L, elapsedMilliseconds);
            verticalOffset = elapsed < CollectionMoveStopMilliseconds
                ? -MathF.Sin(
                    elapsed / (float)CollectionMoveStopMilliseconds *
                    MathF.PI / 1.5f) * 10f
                : 0f;
            alpha = elapsed < CollectionFadeStartMilliseconds
                ? 1f
                : Math.Clamp(
                    1f - (elapsed - CollectionFadeStartMilliseconds) /
                    (float)(CollectionDespawnMilliseconds -
                            CollectionFadeStartMilliseconds), 0f, 1f);
            visible = elapsed <= CollectionDespawnMilliseconds;
        }

        public static Vector3 CreateVelocity(Vector2 direction)
        {
            if (direction.LengthSquared() > 1f)
                direction.Normalize();
            return new Vector3(
                direction.X * InitialHorizontalVelocity,
                direction.Y * InitialHorizontalVelocity,
                InitialVerticalVelocity);
        }

        public static DroppedItemMotionState Resolve(
            Vector2 direction, long elapsedMilliseconds)
        {
            var velocity = CreateVelocity(direction);
            var offset = Vector2.Zero;
            var height = 0f;
            var grounded = false;
            var frames = Math.Max(0, (int)Math.Floor(
                elapsedMilliseconds * FramesPerSecond / 1000d));
            for (var frame = 0; frame < frames; frame++)
            {
                velocity.Z = LinkGameplayMotion.ApplyGravity(
                    velocity.Z, Gravity, 1f);
                if (height + velocity.Z > 0f &&
                    (!grounded || velocity.Z >= 0f || Math.Abs(height) > 2f))
                {
                    height += velocity.Z;
                    grounded = false;
                }
                else
                {
                    velocity.Z = LinkGameplayMotion.ResolveGroundVelocity(
                        velocity.Z, Bounciness, deepWater: false);
                    height = 0f;
                    grounded = true;
                }

                offset += new Vector2(velocity.X, velocity.Y);
                var drag = grounded ? GroundDrag : AirDrag;
                velocity.X *= drag;
                velocity.Y *= drag;
                if (Math.Abs(velocity.X) < 0.01f)
                    velocity.X = 0f;
                if (Math.Abs(velocity.Y) < 0.01f)
                    velocity.Y = 0f;

                if (grounded && velocity.Z == 0f &&
                    velocity.X == 0f && velocity.Y == 0f)
                    break;
            }
            return new DroppedItemMotionState(offset, height, grounded);
        }
    }
}
