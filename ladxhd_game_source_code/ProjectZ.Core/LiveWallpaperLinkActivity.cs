using System;

namespace ProjectZ
{
    public readonly struct LiveWallpaperLinkState
    {
        public LiveWallpaperLinkState(bool visible, bool walking, float journey)
        {
            Visible = visible;
            Walking = walking;
            Journey = Math.Clamp(journey, 0f, 1f);
        }

        public bool Visible { get; }
        public bool Walking { get; }
        public float Journey { get; }
    }

    public static class LiveWallpaperLinkActivity
    {
        public static LiveWallpaperLinkState Resolve(int mode, long elapsedMilliseconds, bool animated)
        {
            if (mode == 3)
                return new LiveWallpaperLinkState(false, false, 0.5f);
            if (!animated || mode == 1)
                return new LiveWallpaperLinkState(true, false, 0.5f);
            if (mode != 2)
            {
                var journey = PositiveModulo(elapsedMilliseconds, 14_000L) / 14_000f;
                return new LiveWallpaperLinkState(true, true, journey);
            }

            var position = PositiveModulo(elapsedMilliseconds, 24_000L);
            if (position < 10_000L)
                return new LiveWallpaperLinkState(true, true, position / 20_000f);
            if (position < 14_000L)
                return new LiveWallpaperLinkState(true, false, 0.5f);
            return new LiveWallpaperLinkState(
                true, true, 0.5f + (position - 14_000L) / 20_000f);
        }

        private static long PositiveModulo(long value, long modulus)
        {
            var remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
