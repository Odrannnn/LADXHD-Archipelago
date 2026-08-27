namespace ProjectZ
{
    public readonly struct LiveWallpaperWildlifeState
    {
        public LiveWallpaperWildlifeState(bool showButterflies, bool showOwl)
        {
            ShowButterflies = showButterflies;
            ShowOwl = showOwl;
        }

        public bool ShowButterflies { get; }
        public bool ShowOwl { get; }
    }

    public static class LiveWallpaperWildlife
    {
        public static LiveWallpaperWildlifeState Resolve(
            int mode, LiveWallpaperTimePhase phase)
        {
            if (mode == 1)
                return new LiveWallpaperWildlifeState(true, true);
            return phase switch
            {
                LiveWallpaperTimePhase.Day => new LiveWallpaperWildlifeState(true, false),
                LiveWallpaperTimePhase.Sunset => new LiveWallpaperWildlifeState(true, true),
                _ => new LiveWallpaperWildlifeState(false, true)
            };
        }
    }
}
