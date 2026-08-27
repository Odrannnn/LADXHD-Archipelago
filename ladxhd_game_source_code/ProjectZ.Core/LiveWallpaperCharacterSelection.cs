using System;

namespace ProjectZ
{
    public static class LiveWallpaperCharacterSelection
    {
        private const long RotationIntervalMilliseconds = 30_000L;

        public static int Resolve(int selection, int scene, long elapsedMilliseconds)
        {
            if (selection is >= 0 and <= 2)
                return selection;
            if (selection == 4)
            {
                return scene switch
                {
                    1 => 0,
                    2 => 2,
                    3 => 1,
                    5 => 2,
                    6 => 0,
                    7 => 2,
                    _ => ResolveRotation(elapsedMilliseconds)
                };
            }
            return ResolveRotation(elapsedMilliseconds);
        }

        private static int ResolveRotation(long elapsedMilliseconds)
        {
            var interval = Math.Max(0L, elapsedMilliseconds) / RotationIntervalMilliseconds;
            return (int)(interval % 3L);
        }
    }
}
