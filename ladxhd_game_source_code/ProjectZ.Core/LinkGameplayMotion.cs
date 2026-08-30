using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    /// <summary>
    /// Link action calculations shared by gameplay and lightweight renderers.
    /// These are the canonical values and update order used by ObjLink and
    /// SystemBody; wallpaper code must not maintain parallel approximations.
    /// </summary>
    public static class LinkGameplayMotion
    {
        public const float PullMilliseconds = 100f;
        public const float PreCarryMilliseconds = 200f;
        public const float FeatherVelocity = 2.35f;
        public const float Gravity = -0.15f;
        public const float WalkSpeed = 1f;
        public const float PegasusBootsSpeed = 2f;
        public const float PegasusBootsChargeMilliseconds = 533f;
        public const float PegasusBootsParticleMilliseconds = 120f;
        public const float HookshotSpeed = 3f;
        public const float HookshotMaximumDistance = 120f;
        public const float CornerCorrectionThreshold = 2.5f;
        public const float CollisionEscapeEpsilon = 0.01f;
        public const int FeatherAirborneFramesAt60Fps = 31;
        // ObjLink receives one grounded movement update on the feather press
        // before SystemBody begins the 31-update airborne arc.
        public const int FeatherTravelFramesAt60Fps =
            FeatherAirborneFramesAt60Fps + 1;
        // UseBracelet runs before PreCarrying can become Carrying. A distinct
        // throw press is therefore first eligible on the following 60 Hz update.
        public const long MinimumSeparateInputMilliseconds = 17L;

        public static Vector3 ResolvePreCarryPosition(
            Vector3 carryStartPosition,
            Vector3 targetPosition,
            float preCarryCounter)
        {
            var counter = Math.Clamp(
                preCarryCounter, 0f, PreCarryMilliseconds);
            var pickupTime = 1f - MathF.Cos(
                counter / PreCarryMilliseconds * MathF.PI * 0.5f);
            var carryPositionXY = Vector2.Lerp(
                new Vector2(carryStartPosition.X, carryStartPosition.Y),
                new Vector2(targetPosition.X, targetPosition.Y),
                1f - MathF.Cos(pickupTime * MathF.PI * 0.5f));
            var carryPositionZ = MathHelper.Lerp(
                carryStartPosition.Z, targetPosition.Z,
                MathF.Sin(pickupTime * MathF.PI * 0.5f));
            return new Vector3(
                carryPositionXY.X, carryPositionXY.Y, carryPositionZ);
        }

        public static float ApplyGravity(
            float velocity, float gravity, float timeMultiplier) =>
            Math.Clamp(velocity + gravity * timeMultiplier, -6f, 6f);

        public static float ResolveGroundVelocity(
            float velocity, float bounciness, bool deepWater) =>
            velocity * bounciness < -0.4f && !deepWater
                ? velocity * -bounciness
                : 0f;

        public static Vector2 ResolveAirVelocity(
            Vector2 previousVelocity,
            Vector2 input,
            float speed,
            float timeMultiplier)
        {
            if (input.LengthSquared() <= 0f)
                return previousVelocity;
            if (input.LengthSquared() > 1f)
                input.Normalize();
            var targetVelocity = input * speed;
            var velocityDifference =
                (previousVelocity - targetVelocity).Length();
            if (velocityDifference <= 0f)
                return previousVelocity;
            var amount = Math.Clamp(
                0.05f / velocityDifference * timeMultiplier, 0f, 1f);
            return Vector2.Lerp(previousVelocity, targetVelocity, amount);
        }

        public static float ResolveHoleSpeedMultiply(
            float collisionAreaPercentage, float absorbStop)
        {
            if (collisionAreaPercentage <= absorbStop)
                return 1f;
            var normalized = (collisionAreaPercentage - absorbStop) /
                             Math.Max(0.0001f, 1f - absorbStop);
            var slowdown = MathF.Pow(
                Math.Clamp(normalized, 0f, 0.8f), 1f);
            return 1f - slowdown;
        }

        public static Vector2 ResolveHoleAbsorption(
            Vector2 previousAbsorption, Vector2 holeDirection,
            float collisionAreaPercentage, float absorbStop,
            float timeMultiplier)
        {
            if (collisionAreaPercentage <= absorbStop)
                return Vector2.Zero;
            var holePull = holeDirection * collisionAreaPercentage * 0.60f;
            var oldPercentage = MathF.Pow(0.5f, timeMultiplier);
            return previousAbsorption * oldPercentage +
                   holePull * (1f - oldPercentage);
        }

        public static bool BlocksInsideCollisionMovement(
            float oldOverlap, float newOverlap, float bodyArea,
            float insideCollisionEscape)
        {
            if (oldOverlap <= 0f)
                return true;
            if (newOverlap > oldOverlap + CollisionEscapeEpsilon)
                return true;
            if (newOverlap < oldOverlap - CollisionEscapeEpsilon)
                return false;
            if (oldOverlap >= bodyArea - CollisionEscapeEpsilon)
                return false;
            return bodyArea <= 0f ||
                   newOverlap / bodyArea >= insideCollisionEscape;
        }

        public static float ResolveHorizontalCornerNudge(
            float playerTop, float playerHeight,
            float wallTop, float wallBottom,
            float threshold)
        {
            var playerBottom = playerTop + playerHeight;
            var overlapTop = playerBottom - wallTop;
            var overlapBottom = wallBottom - playerTop;
            if (overlapTop > 0f && overlapTop <= threshold &&
                overlapBottom > playerHeight)
                return -overlapTop - CollisionEscapeEpsilon;
            if (overlapBottom > 0f && overlapBottom <= threshold &&
                overlapTop > playerHeight)
                return overlapBottom + CollisionEscapeEpsilon;
            return 0f;
        }

        public static float ResolveVerticalCornerNudge(
            float playerLeft, float playerWidth,
            float wallLeft, float wallRight,
            float threshold)
        {
            var playerRight = playerLeft + playerWidth;
            var overlapLeft = playerRight - wallLeft;
            var overlapRight = wallRight - playerLeft;
            if (overlapLeft > 0f && overlapLeft <= threshold &&
                overlapRight > playerWidth)
                return -overlapLeft - CollisionEscapeEpsilon;
            if (overlapRight > 0f && overlapRight <= threshold &&
                overlapLeft > playerWidth)
                return overlapRight + CollisionEscapeEpsilon;
            return 0f;
        }
    }
}
