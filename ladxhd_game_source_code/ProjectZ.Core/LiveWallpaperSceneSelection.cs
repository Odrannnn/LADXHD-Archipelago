using System;

namespace ProjectZ
{
    public static class LiveWallpaperSceneSelection
    {
        private const long RotationIntervalMilliseconds = 45_000L;
        private const int InstalledSceneCount = 3;

        public static int Resolve(int selection, long elapsedMilliseconds, bool installedMapAvailable)
        {
            if (!installedMapAvailable || selection <= 0)
                return 0;
            if (selection <= InstalledSceneCount)
                return selection;
            if (selection != InstalledSceneCount + 1)
                return 0;

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
    }
}
