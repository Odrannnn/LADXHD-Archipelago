using System;

namespace ProjectZ
{
    public static class LiveWallpaperSceneSelection
    {
        private const long RotationIntervalMilliseconds = 45_000L;
        private const long TransitionDurationMilliseconds = 1_200L;
        private const int InstalledSceneCount = 3;

        public static int Resolve(int selection, long elapsedMilliseconds, bool installedMapAvailable)
        {
            if (!installedMapAvailable)
                return 0;
            if (selection <= 0)
                return 1;
            if (selection <= InstalledSceneCount)
                return selection;
            if (selection != InstalledSceneCount + 1)
                return 1;

            var interval = Math.Max(0L, elapsedMilliseconds) / RotationIntervalMilliseconds;
            return 1 + (int)(interval % InstalledSceneCount);
        }

        public static bool TryGetTileOrigin(int scene, out int tileX, out int tileY)
        {
            (tileX, tileY) = scene switch
            {
                1 => (20, 72),
                2 => (10, 112),
                3 => (10, 32),
                _ => (-1, -1)
            };
            return tileX >= 0;
        }

        public static float GetRotationTransitionOpacity(int selection, long elapsedMilliseconds)
        {
            if (selection != InstalledSceneCount + 1)
                return 0f;

            var position = PositiveModulo(elapsedMilliseconds, RotationIntervalMilliseconds);
            if (position < TransitionDurationMilliseconds)
                return 1f - position / (float)TransitionDurationMilliseconds;

            var fadeOutStart = RotationIntervalMilliseconds - TransitionDurationMilliseconds;
            return position >= fadeOutStart
                ? (position - fadeOutStart) / (float)TransitionDurationMilliseconds
                : 0f;
        }

        private static long PositiveModulo(long value, long modulus)
        {
            var remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
