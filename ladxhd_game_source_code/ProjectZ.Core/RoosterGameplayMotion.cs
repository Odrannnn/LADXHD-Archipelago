using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public readonly struct RoosterReleaseMotionState
    {
        public RoosterReleaseMotionState(
            Vector2 position, float height, Vector2 velocity,
            float verticalVelocity, bool grounded)
        {
            Position = position;
            Height = Math.Max(0f, height);
            Velocity = velocity;
            VerticalVelocity = verticalVelocity;
            Grounded = grounded;
        }

        public Vector2 Position { get; }
        public float Height { get; }
        public Vector2 Velocity { get; }
        public float VerticalVelocity { get; }
        public bool Grounded { get; }
    }

    /// <summary>
    /// Canonical ObjCock pickup and flight-height rules shared with the
    /// lightweight wallpaper simulation.
    /// </summary>
    public static class RoosterGameplayMotion
    {
        public const int CarryHeight = 14;
        public const float HoverHeight = 36f;
        public const float HoverAmplitude = 1.5f;
        public const float HoverPeriodMilliseconds = 450f;
        public const float RisePerFrame = 0.5f;
        public const float Gravity = -0.075f;
        public const float GroundDrag = 0.85f;
        public const float ThrownAirDrag = 0.975f;
        public const long PullMilliseconds =
            (long)LinkGameplayMotion.PullMilliseconds;
        public const long PreCarryMilliseconds =
            (long)LinkGameplayMotion.PreCarryMilliseconds;
        public const long PickupSequenceMilliseconds =
            PullMilliseconds + PreCarryMilliseconds;

        public static Vector3 ResolvePickupPosition(
            Vector3 startPosition, Vector3 linkPosition,
            long elapsedMilliseconds)
        {
            if (elapsedMilliseconds <= PullMilliseconds)
                return startPosition;
            var target = new Vector3(
                linkPosition.X, linkPosition.Y,
                linkPosition.Z + CarryHeight);
            return LinkGameplayMotion.ResolvePreCarryPosition(
                startPosition, target,
                elapsedMilliseconds - PullMilliseconds);
        }

        public static float ResolveHoverTarget(double totalElapsedMilliseconds) =>
            HoverHeight + MathF.Sin(
                (float)totalElapsedMilliseconds / HoverPeriodMilliseconds *
                MathF.PI * 2f) * HoverAmplitude;

        public static float AdvanceFlightHeight(
            float currentHeight, double totalElapsedMilliseconds,
            float timeMultiplier)
        {
            var target = ResolveHoverTarget(totalElapsedMilliseconds);
            var amount = Math.Max(0f, RisePerFrame * timeMultiplier);
            if (Math.Abs(currentHeight - target) < amount)
                return target;
            return currentHeight < target
                ? currentHeight + amount
                : currentHeight - amount;
        }

        public static RoosterReleaseMotionState AdvanceRelease(
            RoosterReleaseMotionState state, float timeMultiplier)
        {
            var multiplier = Math.Max(0f, timeMultiplier);
            var verticalVelocity = LinkGameplayMotion.ApplyGravity(
                state.VerticalVelocity, Gravity, multiplier);
            var height = state.Height;
            var grounded = state.Grounded;
            if (height + verticalVelocity * multiplier > 0f &&
                (!grounded || verticalVelocity >= 0f ||
                 Math.Abs(height) > 2f))
            {
                height += verticalVelocity * multiplier;
                grounded = false;
            }
            else
            {
                verticalVelocity = LinkGameplayMotion.ResolveGroundVelocity(
                    verticalVelocity, 0f, deepWater: false);
                height = 0f;
                grounded = true;
            }

            var position = state.Position + state.Velocity * multiplier;
            var drag = grounded ? GroundDrag : ThrownAirDrag;
            var velocity = state.Velocity * MathF.Pow(drag, multiplier);
            if (Math.Abs(velocity.X) < 0.01f * multiplier)
                velocity.X = 0f;
            if (Math.Abs(velocity.Y) < 0.01f * multiplier)
                velocity.Y = 0f;
            return new RoosterReleaseMotionState(
                position, height, velocity, verticalVelocity, grounded);
        }
    }
}
