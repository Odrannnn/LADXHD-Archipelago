using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperScrollingEffectsTests
{
    public static void Run()
    {
        foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1),
            (-32, 0), (32, 0), (0, -32), (0, 32), (-32, -32), (32, 32),
            (-32, 32), (32, -32), (180, 110), (-180, -110) })
        {
            CheckScroll(dx, dy, moving: false);
            CheckScroll(dx, dy, moving: true);
        }
        foreach (var (dx, dy) in new[] { (0f, 0f), (192f, 0f), (-192f, 0f), (0f, 128f),
            (191f, 0f), (0.5f, 0f), (0f, -0.5f), (float.NaN, 0f), (0f, float.PositiveInfinity) })
            Check(!GameSceneEffects.TryGetEffectScroll(192, 128, dx, dy, out _),
                "Non-overlapping, fractional or invalid scrolls must use a full redraw.");

        var strips = new Rectangle[4];
        Check(GameSceneEffects.TryGetEffectScroll(512, 1024, -32, -32, out var scroll),
            "An ordinary one-tile camera step should reuse the cached centre.");
        var count = scroll.WriteExposedRegions(strips, 512, 1024);
        var samplePixels = 0;
        for (var i = 0; i < count; i++)
        {
            var sample = GameSceneEffects.ShadowSampleRegion(strips[i], 512, 1024);
            samplePixels += sample.Width * sample.Height;
        }
        Check(samplePixels < 512 * 1024 / 4,
            "A one-tile diagonal scroll must sample less than a quarter of the full blur target.");
        for (var i = 0; i < 100; i++) Plan();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) Plan();
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated,
            "Planning cache scrolls must not allocate per camera step.");
        void Plan()
        {
            GameSceneEffects.TryGetEffectScroll(512, 1024, -32, -32, out var plan);
            plan.WriteExposedRegions(strips, 512, 1024);
        }
    }

    private static void CheckScroll(int dx, int dy, bool moving)
    {
        const int width = 192, height = 128, oldX = 300, oldY = 300;
        var newX = oldX - dx;
        var newY = oldY - dy;
        Check(GameSceneEffects.TryGetEffectScroll(width, height, dx, dy, out var scroll) &&
            scroll.OffsetX == dx && scroll.OffsetY == dy,
            "Cached pixels must move opposite to the camera's world-origin movement.");
        Rectangle[] previous = [new(oldX + 12, oldY + 12, 18, 16),
            new(oldX + 150, oldY + 80, 12, 16), new(oldX + 48, oldY + 28, 14, 18)];
        Rectangle[] current = moving ? [new(oldX + 16, oldY + 14, 18, 16), Rectangle.Empty,
            new(oldX + 48, oldY + 28, 14, 18), new(newX + 40, newY + 40, 16, 16)] : previous;
        var oldRaw = Raw(oldX, oldY, previous);
        var newRaw = Raw(newX, newY, current);
        var oldBlur = (int[])oldRaw.Clone();
        var expected = (int[])newRaw.Clone();
        var scratch = new float[width * height];
        GameSceneEffects.BlurShadowMask(oldBlur, scratch, width, height);
        GameSceneEffects.BlurShadowMask(expected, scratch, width, height);
        var shifted = (int[])oldBlur.Clone(); // Exposed stale pixels must be overwritten.
        var sunlight = LiveWallpaperDayCycle.Resolve(4, 0);
        var oldLight = oldBlur.Select((pixel, i) => Light(pixel, oldX + i % width, oldY + i / width)).ToArray();
        var light = (Color[])oldLight.Clone();
        for (var y = 0; y < scroll.Source.Height; y++)
        {
            var source = (scroll.Source.Y + y) * width + scroll.Source.X;
            var destination = (scroll.Destination.Y + y) * width + scroll.Destination.X;
            Array.Copy(oldBlur, source, shifted, destination, scroll.Source.Width);
            Array.Copy(oldLight, source, light, destination, scroll.Source.Width);
        }
        var regions = new Rectangle[4 + GameSceneEffects.MaxShadowDirtyRegions];
        var stripCount = scroll.WriteExposedRegions(regions, width, height);
        var covered = new bool[width * height];
        for (var i = 0; i < stripCount; i++)
        for (var y = regions[i].Top; y < regions[i].Bottom; y++)
        for (var x = regions[i].Left; x < regions[i].Right; x++)
        {
            Check(!covered[y * width + x] && !scroll.Reusable.Contains(x, y),
                "Exposed edge strips must partition the non-reusable border without covering the centre twice.");
            covered[y * width + x] = true;
        }
        for (var i = 0; i < covered.Length; i++)
            Check(covered[i] != scroll.Reusable.Contains(i % width, i / width),
                "Every non-reusable pixel, including the old clamped blur border, must be refreshed.");
        var dynamicCount = 0;
        if (moving)
            for (var i = 0; i < Math.Max(previous.Length, current.Length); i++)
            {
                var before = i < previous.Length ? previous[i] : Rectangle.Empty;
                var after = i < current.Length ? current[i] : Rectangle.Empty;
                if (before == after) continue;
                foreach (var world in new[] { before, after })
                {
                    if (world.IsEmpty) continue;
                    var bounds = world;
                    bounds.Offset(-newX, -newY); // Both old and current commands use the NEW cache origin.
                    GameSceneEffects.AddShadowDirtyRegion(regions.AsSpan(stripCount), ref dynamicCount,
                        bounds, width, height);
                }
            }
        var local = new int[width * height];
        for (var i = 0; i < stripCount + dynamicCount; i++)
        {
            var dirty = regions[i];
            var sample = GameSceneEffects.ShadowSampleRegion(dirty, width, height);
            for (var y = 0; y < sample.Height; y++)
                Array.Copy(newRaw, (sample.Y + y) * width + sample.X, local, y * sample.Width, sample.Width);
            GameSceneEffects.BlurShadowMask(local, scratch, sample.Width, sample.Height);
            for (var y = dirty.Top; y < dirty.Bottom; y++)
            {
                Array.Copy(local, (y - sample.Y) * sample.Width + dirty.X - sample.X,
                    shifted, y * width + dirty.X, dirty.Width);
                for (var x = dirty.Left; x < dirty.Right; x++)
                    light[y * width + x] = Light(shifted[y * width + x], newX + x, newY + y);
            }
        }
        Check(shifted.SequenceEqual(expected) && light.SequenceEqual(expected.Select((pixel, i) =>
                Light(pixel, newX + i % width, newY + i / width))),
            $"Scrolled blur/light caches must equal a full redraw (dx={dx}, dy={dy}, moving={moving}).");

        Color Light(int shadow, int x, int y) => Color.Lerp(
            sunlight.AtOcclusion(((uint)shadow >> 24) / 255f), new Color(255, 200, 200),
            ((x * 3 + y * 7) % 31) / 31f);
        static int[] Raw(int originX, int originY, Rectangle[] actors)
        {
            var pixels = new int[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var worldX = originX + x;
                var worldY = originY + y;
                var alpha = ((worldX * 73) ^ (worldY * 127)) & 255;
                foreach (var actor in actors)
                    if (actor.Contains(worldX, worldY)) alpha = 220;
                pixels[y * width + x] = alpha << 24;
            }
            return pixels;
        }
    }

    private static void Check(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }
}
