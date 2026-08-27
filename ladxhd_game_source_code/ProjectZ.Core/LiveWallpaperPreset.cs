namespace ProjectZ
{
    public readonly struct LiveWallpaperPreset
    {
        public LiveWallpaperPreset(
            int scene, int timeOfDay, int featuredCharacter, int characterPosition,
            int linkActivity, int wildlifeSchedule)
        {
            Scene = scene;
            TimeOfDay = timeOfDay;
            FeaturedCharacter = featuredCharacter;
            CharacterPosition = characterPosition;
            LinkActivity = linkActivity;
            WildlifeSchedule = wildlifeSchedule;
        }

        public int Scene { get; }
        public int TimeOfDay { get; }
        public int FeaturedCharacter { get; }
        public int CharacterPosition { get; }
        public int LinkActivity { get; }
        public int WildlifeSchedule { get; }
    }

    public static class LiveWallpaperPresets
    {
        public static bool TryResolve(int preset, out LiveWallpaperPreset value)
        {
            value = preset switch
            {
                1 => new LiveWallpaperPreset(1, 2, 4, 0, 1, 0),
                2 => new LiveWallpaperPreset(3, 3, 4, 0, 1, 0),
                3 => new LiveWallpaperPreset(4, 0, 4, 0, 2, 0),
                _ => default
            };
            return preset is >= 1 and <= 3;
        }
    }
}
