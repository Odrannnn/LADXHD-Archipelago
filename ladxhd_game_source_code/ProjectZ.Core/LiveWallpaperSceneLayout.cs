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
            5 => new LiveWallpaperSceneLayout(6.0f, 0.5f),
            6 => new LiveWallpaperSceneLayout(6.0f, 0.76f),
            7 => new LiveWallpaperSceneLayout(6.0f, 0.5f),
            8 => new LiveWallpaperSceneLayout(6.0f, 0.42f),
            9 => new LiveWallpaperSceneLayout(6.0f, 0.68f),
            10 => new LiveWallpaperSceneLayout(6.0f, 0.62f),
            11 => new LiveWallpaperSceneLayout(6.0f, 0.55f),
            12 => new LiveWallpaperSceneLayout(6.0f, 0.5f),
            13 => new LiveWallpaperSceneLayout(6.0f, 0.58f),
            14 => new LiveWallpaperSceneLayout(6.0f, 0.5f),
            15 => new LiveWallpaperSceneLayout(6.0f, 0.5f),
            _ => new LiveWallpaperSceneLayout(5.6f, 0.72f)
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
