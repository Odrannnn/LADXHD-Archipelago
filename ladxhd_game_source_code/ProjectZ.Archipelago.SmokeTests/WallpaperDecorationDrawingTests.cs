using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperDecorationDrawingTests
{
    public static void Run()
    {
        var random = new Random(2122);
        foreach (var (width, height) in new[] { (1200, 2608), (2608, 1200), (589, 1280), (240, 160) })
        foreach (var offset in new[] { 0f, 0.5f, 1f })
        {
            if (!LiveWallpaperMapViewport.TryCreateCentered(width, height, 160, 128,
                    1024.25f, 768.75f, offset, out var initial))
                throw new InvalidOperationException("Decoration fixture viewport must load.");
            foreach (var scroll in new[] { 0f, 0.125f, 1f, 13.875f })
            {
                var viewport = initial.WithCameraOrigin(initial.CameraOriginX + scroll,
                    initial.CameraOriginY + scroll, 160, 128);
                // Preserve the old inclusive 64-game-pixel visibility boundary,
                // including draw offsets, fractional following and both rotations.
                foreach (var delta in new[] { -0.001f, 0f, 0.001f })
                foreach (var edgeX in new[] { viewport.OriginX * 16f - 64f,
                             (viewport.OriginX + viewport.Columns) * 16f + 64f })
                foreach (var edgeY in new[] { viewport.OriginY * 16f - 64f,
                             (viewport.OriginY + viewport.Rows) * 16f + 64f })
                {
                    var chest = new LiveWallpaperMapDecoration("chest_back", 0, 0,
                        topLeft: true, drawOffsetX: 7, drawOffsetY: -13, sourceOffsetX: 32);
                    Compare(chest, viewport, edgeX - 7 + delta, edgeY + 13 + delta);
                }
                for (var i = 0; i < 100; i++)
                {
                    var decoration = new LiveWallpaperMapDecoration("tree_0", random.Next(2560), random.Next(2048),
                        playerLayer: i % 2 == 0, topLeft: i % 3 == 0, stoneLayout: i % 5 == 0,
                        drawOffsetX: random.Next(-64, 65), drawOffsetY: random.Next(-64, 65));
                    Compare(decoration, viewport, decoration.EntityX, decoration.EntityY);
                }
            }
        }

        LiveWallpaperMapViewport.TryCreateCentered(1200, 2608, 160, 128,
            1024, 768, 0.5f, out var view);
        var block = new LiveWallpaperMapDecoration("movestone_0", 0, 0, playerLayer: false, topLeft: true);
        if (block.TryGetDrawAnchor(view, block.EntityX, block.EntityY, out _, out _) ||
            !block.TryGetDrawAnchor(view, 1024.5f, 768.25f, out _, out _))
            throw new InvalidOperationException("Moved blocks must be culled at their resolved position, not their original cell.");
        Compare(block, view, 1024.5f, 768.25f);

        var first = new LiveWallpaperMapDecoration("shared", 0, 0, atlasName: "objects");
        var second = new LiveWallpaperMapDecoration("shared", 0, 0, atlasName: "items");
        var assets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["objects\nshared"] = 1, ["items\nshared"] = 2
        };
        if (assets[first.AssetKey] != 1 || assets[second.AssetKey] != 2 ||
            !ReferenceEquals(first.AssetKey, first.AssetKey))
            throw new InvalidOperationException("Cached keys must retain atlas separation and reuse the same string.");
        var key = first.AssetKey;
        for (var i = 0; i < 100; i++) first.TryGetDrawAnchor(view, 1024, 768, out _, out _);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++)
        {
            if (!ReferenceEquals(key, first.AssetKey)) throw new InvalidOperationException("Sprite key changed.");
            first.TryGetDrawAnchor(view, 1024, 768, out _, out _);
        }
        if (GC.GetAllocatedBytesForCurrentThread() != allocated)
            throw new InvalidOperationException("Decoration key/visibility queries must not allocate per frame.");
    }

    private static void Compare(LiveWallpaperMapDecoration decoration, LiveWallpaperMapViewport viewport,
        float entityX, float entityY)
    {
        var expectedVisible = ReferenceAnchor(decoration, viewport, entityX, entityY, out var expectedX, out var expectedY);
        var visible = decoration.TryGetDrawAnchor(viewport, entityX, entityY, out var x, out var y);
        if (visible != expectedVisible || x != expectedX || y != expectedY ||
            decoration.AssetKey != decoration.AtlasName + "\n" + decoration.SpriteId)
            throw new InvalidOperationException("Early decoration culling must preserve the original anchor, visibility boundary and sprite identity exactly.");
    }

    // Frozen arithmetic from DrawInstalledMapDecoration before the early-cull change.
    internal static bool ReferenceAnchor(LiveWallpaperMapDecoration decoration, LiveWallpaperMapViewport viewport,
        float entityX, float entityY, out float anchorX, out float anchorY)
    {
        var scale = viewport.TileSize / 16f;
        anchorX = viewport.Left + ((entityX + decoration.DrawOffsetX) / 16f - viewport.OriginX) * viewport.TileSize;
        anchorY = viewport.Top + ((entityY + decoration.DrawOffsetY) / 16f - viewport.OriginY) * viewport.TileSize;
        if (anchorX < viewport.Left - 64f * scale ||
            anchorX > viewport.Left + viewport.Columns * viewport.TileSize + 64f * scale ||
            anchorY < viewport.Top - 64f * scale ||
            anchorY > viewport.Top + viewport.Rows * viewport.TileSize + 64f * scale)
            return false;
        return true;
    }
}
