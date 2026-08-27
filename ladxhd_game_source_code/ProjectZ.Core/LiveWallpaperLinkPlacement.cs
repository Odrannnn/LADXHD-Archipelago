namespace ProjectZ
{
    public readonly struct LiveWallpaperLinkSpritePlacement
    {
        public LiveWallpaperLinkSpritePlacement(float anchorX, float anchorY, float scale)
        {
            AnchorX = anchorX;
            AnchorY = anchorY;
            Scale = scale;
        }

        public float AnchorX { get; }
        public float AnchorY { get; }
        public float Scale { get; }
    }

    public static class LiveWallpaperLinkPlacement
    {
        // ObjLink applies this offset before the animation and frame offsets.
        private const float LinkSpriteOffsetX = -7f;
        private const float LinkSpriteOffsetY = -16f;
        private const float GameTileSize = 16f;

        public static LiveWallpaperLinkSpritePlacement Resolve(
            LiveWallpaperMapViewport viewport,
            LiveWallpaperSimulatedLinkState link)
        {
            var scale = viewport.TileSize / GameTileSize;
            var entityX = viewport.Left +
                          (link.MapX - viewport.OriginX) * viewport.TileSize;
            var entityY = viewport.Top +
                          (link.MapY - viewport.OriginY) * viewport.TileSize -
                          link.Height * scale;
            return new LiveWallpaperLinkSpritePlacement(
                entityX + LinkSpriteOffsetX * scale,
                entityY + LinkSpriteOffsetY * scale,
                scale);
        }
    }
}
