namespace ProjectZ
{
    public readonly struct LiveWallpaperSceneLayout
    {
        public LiveWallpaperSceneLayout(float groundTileRow, float featuredXRatio)
        {
            GroundTileRow = groundTileRow;
            FeaturedXRatio = featuredXRatio;
        }

        public float GroundTileRow { get; }
        public float FeaturedXRatio { get; }
    }

    public static class LiveWallpaperSceneLayouts
    {
        public static LiveWallpaperSceneLayout Resolve(int scene) => scene switch
        {
            1 => new LiveWallpaperSceneLayout(5.6f, 0.72f),
            2 => new LiveWallpaperSceneLayout(6.0f, 0.82f),
            3 => new LiveWallpaperSceneLayout(5.35f, 0.66f),
            _ => new LiveWallpaperSceneLayout(0f, 0.78f)
        };

        public static float ResolveFeaturedXRatio(int position, int scene) => position switch
        {
            1 => 0.24f,
            2 => 0.5f,
            3 => 0.76f,
            _ => Resolve(scene).FeaturedXRatio
        };
    }
}
