namespace ProjectZ
{
    public static class LiveWallpaperFrameScheduler
    {
        private const long StaticFrameDelayMilliseconds = 1_000L;

        public static long GetDelayMilliseconds(bool animated, int requestedFrameRate)
        {
            if (!animated)
                return StaticFrameDelayMilliseconds;
            var frameRate = requestedFrameRate <= 15 ? 15 : 30;
            return 1_000L / frameRate;
        }
    }
}
