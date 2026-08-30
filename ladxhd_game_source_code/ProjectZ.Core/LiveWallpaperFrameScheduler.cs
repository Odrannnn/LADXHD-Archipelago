namespace ProjectZ
{
    public static class LiveWallpaperFrameScheduler
    {
        private const long StaticFrameDelayMilliseconds = 1_000L;

        public static long GetDelayMilliseconds(bool animated, int requestedFrameRate)
        {
            if (!animated)
                return StaticFrameDelayMilliseconds;
            var frameRate = requestedFrameRate == 60 ? 60 : requestedFrameRate <= 15 ? 15 : 30;
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
