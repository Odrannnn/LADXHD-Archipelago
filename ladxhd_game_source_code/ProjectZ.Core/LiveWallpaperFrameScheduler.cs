namespace ProjectZ
{
    public static class LiveWallpaperFrameScheduler
    {
        public const int AdaptiveFrameRate = 0;
        private const long StaticFrameDelayMilliseconds = 1_000L;

        public static int ResolveFrameRate(
            int requestedFrameRate,
            bool needsHighFrameRate,
            bool passive)
        {
            if (requestedFrameRate == AdaptiveFrameRate)
                return needsHighFrameRate ? 60 : passive ? 15 : 30;
            return requestedFrameRate == 60 ? 60 : requestedFrameRate <= 15 ? 15 : 30;
        }

        public static long GetDelayMilliseconds(bool animated, int requestedFrameRate)
        {
            if (!animated)
                return StaticFrameDelayMilliseconds;
            // Adaptive timing is resolved by the wallpaper engine using current
            // scene activity. Treat an unresolved adaptive value as its normal
            // 30 FPS tier so it can never accidentally fall into the 15 FPS
            // legacy-value clamp.
            var frameRate = ResolveFrameRate(
                requestedFrameRate, needsHighFrameRate: false, passive: false);
            return 1_000L / frameRate;
        }

        public static long GetCompensatedDelayMilliseconds(
            long nowMilliseconds,
            long previousDeadlineMilliseconds,
            bool animated,
            int requestedFrameRate,
            out long nextDeadlineMilliseconds)
        {
            var interval = GetDelayMilliseconds(animated, requestedFrameRate);
            if (previousDeadlineMilliseconds <= 0)
            {
                nextDeadlineMilliseconds = nowMilliseconds + interval;
                return interval;
            }

            var deadline = previousDeadlineMilliseconds + interval;
            if (deadline <= nowMilliseconds)
            {
                // The renderer missed this frame. Drop that deadline and resume
                // immediately instead of adding another full interval.
                nextDeadlineMilliseconds = nowMilliseconds;
                return 0L;
            }

            nextDeadlineMilliseconds = deadline;
            return deadline - nowMilliseconds;
        }
    }
}
