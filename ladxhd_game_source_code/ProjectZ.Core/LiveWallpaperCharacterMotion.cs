using System;

namespace ProjectZ
{
    public readonly struct LiveWallpaperCharacterMotionState
    {
        public LiveWallpaperCharacterMotionState(
            float horizontalOffset, float lift, bool facingRight, bool showNotes)
        {
            HorizontalOffset = Math.Clamp(horizontalOffset, -1f, 1f);
            Lift = Math.Clamp(lift, 0f, 1f);
            FacingRight = facingRight;
            ShowNotes = showNotes;
        }

        public float HorizontalOffset { get; }
        public float Lift { get; }
        public bool FacingRight { get; }
        public bool ShowNotes { get; }
    }

    public static class LiveWallpaperCharacterMotion
    {
        public static LiveWallpaperCharacterMotionState Resolve(
            int character, long elapsedMilliseconds, bool animated)
        {
            if (!animated)
                return new LiveWallpaperCharacterMotionState(0f, 0f, true, false);

            return character switch
            {
                0 => new LiveWallpaperCharacterMotionState(0f, 0f, true, true),
                1 => ResolveWander(elapsedMilliseconds, 6_000L, twoHops: true),
                2 => ResolveWander(elapsedMilliseconds, 5_200L, twoHops: false),
                _ => new LiveWallpaperCharacterMotionState(0f, 0f, true, false)
            };
        }

        private static LiveWallpaperCharacterMotionState ResolveWander(
            long elapsedMilliseconds, long period, bool twoHops)
        {
            var phase = PositiveModulo(elapsedMilliseconds, period) / (float)period;
            var angle = phase * MathF.PI * 2f;
            var horizontal = MathF.Sin(angle);
            var liftWave = MathF.Sin(angle * (twoHops ? 2f : 1f));
            return new LiveWallpaperCharacterMotionState(
                horizontal, MathF.Abs(liftWave), MathF.Cos(angle) >= 0f, false);
        }

        private static long PositiveModulo(long value, long modulus)
        {
            var remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
