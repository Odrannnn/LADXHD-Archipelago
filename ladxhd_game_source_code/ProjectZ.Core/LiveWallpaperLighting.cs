namespace ProjectZ
{
    public enum LiveWallpaperTimePhase
    {
        Day,
        Sunset,
        Night
    }

    public static class LiveWallpaperLighting
    {
        public static LiveWallpaperTimePhase Resolve(int mode, int localHour)
        {
            if (mode == 1)
                return LiveWallpaperTimePhase.Day;
            if (mode == 2)
                return LiveWallpaperTimePhase.Sunset;
            if (mode == 3)
                return LiveWallpaperTimePhase.Night;

            var hour = ((localHour % 24) + 24) % 24;
            if (hour >= 7 && hour < 18)
                return LiveWallpaperTimePhase.Day;
            if (hour >= 5 && hour < 7 || hour >= 18 && hour < 21)
                return LiveWallpaperTimePhase.Sunset;
            return LiveWallpaperTimePhase.Night;
        }
    }
}
