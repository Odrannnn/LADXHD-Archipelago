namespace ProjectZ
{
    public static class LiveWallpaperInteraction
    {
        public static int NextFeaturedCharacter(int current) =>
            current is >= 0 and < 2 ? current + 1 : 0;

        public static int NextScene(int current) => current == 0 ? 1 : 0;
    }
}
