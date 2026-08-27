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
        private const float LinkWalkPixelsPerSecond = 60f;

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

        public static LiveWallpaperLinkState ResolveForScene(
            int mode, int scene, long elapsedMilliseconds, bool animated)
        {
            var traversalMilliseconds = (long)MathF.Round(
                LiveWallpaperLinkRoute.GetPathLengthPixels(scene) /
                LinkWalkPixelsPerSecond * 1_000f);
            return ResolveWithTraversal(mode, elapsedMilliseconds, animated,
                Math.Max(1L, traversalMilliseconds));
        }

        private static LiveWallpaperLinkState ResolveWithTraversal(
            int mode, long elapsedMilliseconds, bool animated,
            long traversalMilliseconds)
        {
            if (mode == 3)
                return new LiveWallpaperLinkState(false, false, 0.5f);
            if (!animated || mode == 1)
                return new LiveWallpaperLinkState(true, false, 0.5f);
            if (mode != 2)
            {
                var roundTrip = traversalMilliseconds * 2L;
                var journey = PositiveModulo(elapsedMilliseconds, roundTrip) /
                              (float)roundTrip;
                return new LiveWallpaperLinkState(true, true, journey);
            }

            const long restMilliseconds = 4_000L;
            var cycle = traversalMilliseconds * 2L + restMilliseconds * 2L;
            var position = PositiveModulo(elapsedMilliseconds, cycle);
            if (position < traversalMilliseconds)
                return new LiveWallpaperLinkState(
                    true, true, position / (float)traversalMilliseconds * 0.5f);
            position -= traversalMilliseconds;
            if (position < restMilliseconds)
                return new LiveWallpaperLinkState(true, false, 0.5f);
            position -= restMilliseconds;
            if (position < traversalMilliseconds)
                return new LiveWallpaperLinkState(
                    true, true, 0.5f + position / (float)traversalMilliseconds * 0.5f);
            return new LiveWallpaperLinkState(true, false, 0f);
        }

        private static long PositiveModulo(long value, long modulus)
        {
            var remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
