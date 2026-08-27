using System;

namespace ProjectZ
{
    public static class LiveWallpaperSceneSelection
    {
        private const long RotationIntervalMilliseconds = 45_000L;
        private const long TransitionDurationMilliseconds = 1_200L;
        public const int RotationSelection = 4;
        public const int MaximumSelection = 15;
        private static readonly int[] InstalledScenes =
            [1, 2, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];

        public static int Resolve(int selection, long elapsedMilliseconds, bool installedMapAvailable)
        {
            if (!installedMapAvailable)
                return 0;
            if (selection != RotationSelection &&
                TryGetTileOrigin(selection, out _, out _))
                return selection;
            if (selection != RotationSelection)
                return 1;

            var interval = Math.Max(0L, elapsedMilliseconds) / RotationIntervalMilliseconds;
            return InstalledScenes[(int)(interval % InstalledScenes.Length)];
        }

        public static int NextFixedScene(int current)
        {
            var index = Array.IndexOf(InstalledScenes, current);
            return index >= 0 && index + 1 < InstalledScenes.Length
                ? InstalledScenes[index + 1]
                : InstalledScenes[0];
        }

        public static bool TryGetTileOrigin(int scene, out int tileX, out int tileY)
        {
            (tileX, tileY) = scene switch
            {
                1 => (20, 72),
                2 => (10, 112),
                3 => (10, 32),
                5 => (92, 42),
                6 => (129, 99),
                7 => (61, 6),
                8 => (91, 100),
                9 => (55, 73),
                10 => (61, 61),
                11 => (28, 43),
                12 => (124, 34),
                13 => (132, 8),
                14 => (145, 111),
                15 => (124, 70),
                _ => (-1, -1)
            };
            return tileX >= 0;
        }

        public static float GetRotationTransitionOpacity(int selection, long elapsedMilliseconds)
        {
            if (selection != RotationSelection)
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
