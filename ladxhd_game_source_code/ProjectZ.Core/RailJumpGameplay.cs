using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    /// <summary>
    /// Canonical calculations used by ObjJump and the lightweight wallpaper.
    /// Authored map values remain the source of direction, landing, height and speed.
    /// </summary>
    public static class RailJumpGameplay
    {
        public const float BaseSpeed = 0.045f;
        public const float BaseHeight = 12f;

        public static int GetDirection(Vector2 offset)
        {
            if (offset == Vector2.Zero)
                return 0;
            var degrees = MathHelper.ToDegrees((float)(
                Math.Atan2(offset.Y, offset.X) + MathF.PI * 1.25f));
            while (degrees >= 360f)
                degrees -= 360f;
            return (int)(degrees / 90f);
        }

        public static Vector2 GetGoal(
            Vector2 playerPosition,
            float triggerX, float triggerY,
            float triggerWidth, float triggerHeight,
            Vector2 offset, float bodyWidth, float bodyHeight)
        {
            var direction = GetDirection(offset);
            var goal = playerPosition;
            if (direction == 0)
                goal.X = triggerX + triggerWidth + offset.X - bodyWidth / 2f;
            else if (direction == 2)
                goal.X = triggerX + offset.X + bodyWidth / 2f;
            else if (direction == 1)
                goal.Y = triggerY + triggerHeight + offset.Y;
            else if (direction == 3)
                goal.Y = triggerY + offset.Y + bodyHeight;
            if (direction % 2 != 0)
                goal.X += offset.X;
            else
                goal.Y += offset.Y;
            return goal;
        }

        public static float GetHeightMultiplier(Vector2 offset)
        {
            var multiplier = 1f;
            var length = offset.Length();
            if (length > 16f)
                multiplier += (length - 16f) / 32f;
            if (offset.Y < -4f)
                multiplier *= 0.75f;
            return multiplier;
        }

        public static float GetSpeedMultiplier(Vector2 offset)
        {
            var length = offset.Length();
            return length > 16f ? 1f - (length - 16f) / 80f : 1f;
        }

        public static float GetProgressAmount(float percentage) =>
            MathF.Sin(percentage * (MathF.PI * 0.3f)) /
            MathF.Sin(MathF.PI * 0.3f);

        public static float GetHeight(float percentage, float jumpHeight,
            float goalHeight = 0f) =>
            MathF.Sin(percentage * MathF.PI) * jumpHeight +
            percentage * goalHeight;
    }
}
