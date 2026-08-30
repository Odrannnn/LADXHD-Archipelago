using System.Buffers;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperSceneEffectsTests
{
    public static void Run()
    {
        static void Check(bool result, string message)
        {
            if (!result)
                throw new InvalidOperationException(message);
        }
        static LiveWallpaperMap Map(string[] templates, params string[] objects)
        {
            var data = "3\n0\n0\ntileset0.png\n16\n16\n1\n" +
                string.Concat(Enumerable.Repeat(new string(',', 16) + "\n", 16)) +
                templates.Length + "\n" + string.Join('\n', templates) + "\n" +
                objects.Length + "\n" + string.Join('\n', objects) + "\n";
            Check(LiveWallpaperMap.TryLoad(new StringReader(data), out var map),
                "Scene-effect fixtures must load without a graphics device or game/save manager.");
            return map;
        }

        var map = Map(["tree0", "roof01", "bush", "stone", "shadowSetter"],
            "0;16;32", "1;0;0", "2;64;32", "3;80;47", "4;0;0;0.6;0.2",
            "2;96;32;;;true;false");
        var effects = map.SceneEffects;
        Check(effects.UseShadows && !effects.UseLighting && effects.Shadows.Count == 3 &&
            effects.ShadowHeight == 0.6f && effects.ShadowRotation == 0.2f,
            "Map shadow configuration must honor actual defaults/overrides and exclude shadowless roofs/bushes.");
        Check(effects.Shadows[0].SpriteId == "tree_0_shadow" &&
            effects.Shadows[0].EntityX == 32 && effects.Shadows[0].EntityY == 56 &&
            effects.Shadows[0].OffsetY == 0,
            "Trees must use their dedicated shadow atlas sprite and canonical entity anchor.");
        Check(effects.Shadows[1].BushKey == map.GetBushKey(64, 32) &&
            effects.Shadows[1].OffsetY == -1 && effects.Shadows[2].StoneLayout &&
            effects.Shadows[2].StoneKey == map.GetStoneKey(80, 48) &&
            effects.Shadows[2].EntityX == 88 && effects.Shadows[2].EntityY == 60,
            "Bush/stone shadow invalidation must share cut/lift keys, including off-grid stone placements.");
        Check(!Map(["shadowDisabler", "tree0"], "0;0;0", "1;0;0").SceneEffects.UseShadows,
            "Maps that disable game shadows must not acquire wallpaper shadows.");

        var lighting = Map(["houseBlacker", "doorLight", "lamp", "lamp2", "spriteLight"],
            "0;0;0", "1;32;48", "2;16;16", "3;48;16", "2;64;16;;;;true",
            "2;80;16;;;;;lamp_key", "2;96;16;;;;;;false",
            "4;0;0;doorLight;255;128;64;128;2;1");
        Check(lighting.SceneEffects.UseLighting &&
            lighting.SceneEffects.Ambient == GameSceneEffects.AmbientLight(255, 220, 180, 175) &&
            lighting.SceneEffects.Lights.Count == 4,
            "Lighting must use the game ambient multiplier, excluding unlit powder/key lamps and non-emitting lamps.");
        var lights = lighting.SceneEffects.Lights;
        Check(lights[0].X == 40 && lights[0].Y == 56 && lights[0].Size == 128 &&
            lights[0].Color.A == 100 && lights[1].X == 24 && lights[1].Y == 32 &&
            lights[1].Size == GameSceneEffects.LampSize &&
            lights[1].Color == new Color(255, 200, 200) &&
            lights[^1].SpriteId == "doorLight" && lights[^1].Rotation == 1 && lights[^1].Layer == 2,
            "Light sizes, lamp centres, colors, sprite rotation and stable layer order must match game definitions.");
        Check(lighting.Lamps.Count == 5 && lighting.Lamps[1].AnimationPath == "Objects/lamp_torch.ani" &&
            lighting.Lamps[0].AnimationName == "idle" && lighting.Lamps[2].AnimationName == "dead" &&
            lighting.Lamps[3].AnimationName == "dead" && lighting.Lamps[4].AnimationName == "idle" &&
            lighting.Lamps[0].AnimationKey != lighting.Lamps[2].AnimationKey,
            "Every lamp template must render its canonical animation without sharing lit/unlit animation state.");
        Check(Map(["caveBlacker"], "0;0;0;100;120;140;200").SceneEffects.Ambient ==
            GameSceneEffects.AmbientLight(100, 120, 140, 200),
            "Explicit map ambient parameters must override the template defaults.");
        Check(LiveWallpaperAtlas.TryLoad(new StringReader("1\n2\nlight:0,0,48,48,24,24\n"),
                "light", out var lightEntry) &&
            lightEntry.Width == 96 && lightEntry.Height == 96 && lightEntry.TextureScale == 2 &&
            lightEntry.OriginX / lightEntry.TextureScale == 24,
            "High-resolution light atlases must retain their reciprocal world scale, not double their size or pivot.");

        Check(GameSceneEffects.ProjectShadow(0, 0, 32, 0.75f, 0.125f) == new Vector2(4, 8) &&
            GameSceneEffects.ProjectShadow(16, 32, 32, 0.75f, 0.125f) == new Vector2(16, 32) &&
            GameSceneEffects.SpriteShadowYOffset(8) == -5 &&
            GameSceneEffects.SpriteShadowYOffset(8, 0) == -4 &&
            Math.Abs(GameSceneEffects.SpriteShadowOpacity(8) - 0.2f) < 0.001f &&
            GameSceneEffects.SpriteShadowOpacity(12) == 0 &&
            GameSceneEffects.GroundShadowOpacity(0.5f) == 0.5f &&
            GameSceneEffects.GroundShadowOpacity(8) == 1 &&
            GameSceneEffects.ShadowRenderScale(5.625f) == 2,
            "Shadow projection, elevation, blur resolution and fading must match the installed shader and BodyDrawShadowComponent.");
        var uniform = Enumerable.Repeat(unchecked((int)0xFFFFFFFF), 81).ToArray();
        GameSceneEffects.BlurShadowMask(uniform, new float[81], 9, 9);
        Check(uniform.All(pixel => pixel == unchecked((int)0x8C000000)),
            "The two-pass blur must preserve a uniform silhouette with exactly the game's 55% opacity.");
        var impulse = new int[81];
        impulse[40] = unchecked((int)0xFFFFFFFF);
        GameSceneEffects.BlurShadowMask(impulse, new float[81], 9, 9);
        Check((uint)impulse[40] >> 24 == 17 && (uint)impulse[39] >> 24 == 8 &&
            impulse[39] == impulse[41] && impulse[31] == impulse[49] &&
            impulse[0] == 0 && impulse.All(pixel => (pixel & 0xFFFFFF) == 0),
            "Blur must reproduce the shader's bilinear near/far taps and emit a black alpha mask, not sprite colors.");

        CheckPartialBlur(new Rectangle(30, 20, 16, 24), new Rectangle(32, 22, 16, 24));
        CheckPartialBlur(new Rectangle(0, 0, 16, 24), new Rectangle(1, 2, 16, 24));
        CheckPartialBlur(new Rectangle(145, 75, 16, 24), new Rectangle(150, 80, 16, 24));
        CheckPartialBlur(new Rectangle(30, 20, 16, 24), Rectangle.Empty);
        CheckPartialBlur(Rectangle.Empty, new Rectangle(30, 20, 16, 24));
        CheckSeparateShadowRegions();
        CheckPooledShadowBuffers();
        CheckOptimizedBlur();
        WallpaperScrollingEffectsTests.Run();

        using var xnb = new MemoryStream();
        using (var writer = new BinaryWriter(xnb, Encoding.UTF8, true))
        {
            writer.Write(new byte[] { (byte)'X', (byte)'N', (byte)'B', (byte)'a', 5, 1 });
            writer.Write(0);
            writer.Write7BitEncodedInt(1);
            writer.Write("Microsoft.Xna.Framework.Content.Texture2DReader");
            writer.Write(0);
            writer.Write7BitEncodedInt(0);
            writer.Write7BitEncodedInt(1);
            writer.Write(0);
            writer.Write(2);
            writer.Write(1);
            writer.Write(1);
            writer.Write(8);
            writer.Write(new byte[] { 64, 32, 16, 128, 0, 0, 0, 0 });
            xnb.Position = 6;
            writer.Write((int)xnb.Length);
        }
        xnb.Position = 0;
        Check(LiveWallpaperTexture.TryReadXnb(xnb, out var width, out var height, out var pixels) &&
            width == 2 && height == 1 && pixels[0] == unchecked((int)0x807F3F1F) && pixels[1] == 0,
            "Installed light pixels must be decoded from XNB and unpremultiplied for Android without generated textures.");
        var bytes = xnb.ToArray();
        Check(!LiveWallpaperTexture.TryReadXnb(new MemoryStream(bytes[..^1]), out _, out _, out _),
            "A truncated light texture must fail safely.");
        bytes[5] = 0x80;
        Check(!LiveWallpaperTexture.TryReadXnb(new MemoryStream(bytes), out _, out _, out _),
            "Unsupported compressed textures must be rejected rather than read as RGBA.");
    }

    private static void CheckSeparateShadowRegions()
    {
        // Distant actors must not blur the unchanged space between them.
        CheckRegionalBlur([new(10, 8, 10, 12), new(136, 72, 10, 12)],
            [new(12, 10, 10, 12), new(134, 70, 10, 12)], expectedRegions: 2, maxAreaFraction: 0.2f);
        // Overlap and bridge regions must coalesce, regardless of insertion order.
        CheckRegionalBlur([new(20, 20, 10, 12), new(24, 24, 10, 12)],
            [new(22, 20, 10, 12), new(26, 24, 10, 12)], expectedRegions: 1);
        CheckRegionalBlur([], [new(10, 10, 10, 10), new(40, 10, 10, 10), new(25, 10, 10, 10)],
            expectedRegions: 1);
        CheckRegionalBlur([new(-8, -6, 12, 12), new(154, 89, 12, 12)],
            [new(-6, -4, 12, 12), new(158, 94, 12, 12)]);
        CheckRegionalBlur([new(10, 8, 10, 12)], [new(136, 72, 10, 12)], expectedRegions: 2);
        CheckRegionalBlur([new(10, 8, 10, 12)], []);
        CheckRegionalBlur([], [new(136, 72, 10, 12)]);
        CheckRegionalBlur([new(-30, -30, 4, 4)], [new(200, 200, 4, 4)], expectedRegions: 0);
        var crowded = (from y in new[] { 8, 40, 72 } from x in new[] { 8, 64, 120 }
            select new Rectangle(x, y, 2, 2)).ToArray();
        CheckRegionalBlur([], crowded, expectedRegions: 1); // Ninth island reaches the bounded fallback.
        var random = new Random(107);
        for (var frame = 0; frame < 24; frame++)
        {
            Rectangle[] Shapes() => Enumerable.Range(0, random.Next(1, 14)).Select(_ =>
                new Rectangle(random.Next(-16, 170), random.Next(-16, 105),
                    random.Next(1, 18), random.Next(1, 24))).ToArray();
            CheckRegionalBlur(Shapes(), Shapes());
        }

        var regions = new Rectangle[GameSceneEffects.MaxShadowDirtyRegions];
        for (var pass = 0; pass < 100; pass++) Collect();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 1000; pass++) Collect();
        if (GC.GetAllocatedBytesForCurrentThread() != allocated)
            throw new InvalidOperationException("Collecting bounded shadow regions must not allocate per frame.");
        void Collect()
        {
            var count = 0;
            foreach (var bounds in crowded)
                GameSceneEffects.AddShadowDirtyRegion(regions, ref count, bounds, 160, 96);
        }
    }

    private static void CheckRegionalBlur(Rectangle[] previous, Rectangle[] current,
        int expectedRegions = -1, float maxAreaFraction = 1f)
    {
        const int width = 160, height = 96;
        var random = new Random(97);
        var background = Enumerable.Range(0, width * height)
            .Select(_ => random.Next(256) << 24).ToArray();
        var before = (int[])background.Clone();
        var after = (int[])background.Clone();
        foreach (var bounds in previous) Fill(before, bounds);
        foreach (var bounds in current) Fill(after, bounds);
        var expected = (int[])after.Clone();
        GameSceneEffects.BlurShadowMask(before, new float[before.Length], width, height);
        GameSceneEffects.BlurShadowMask(expected, new float[expected.Length], width, height);
        var regions = new Rectangle[GameSceneEffects.MaxShadowDirtyRegions];
        var count = 0;
        var union = Rectangle.Empty;
        foreach (var bounds in previous.Concat(current))
        {
            GameSceneEffects.AddShadowDirtyRegion(regions, ref count, bounds, width, height);
            union = union.IsEmpty ? bounds : Rectangle.Union(union, bounds);
        }
        if (count > regions.Length || expectedRegions >= 0 && count != expectedRegions)
            throw new InvalidOperationException("Shadow regions must stay bounded, separate distant actors and merge overlapping ones.");
        var scratch = new float[width * height];
        var lightBefore = before.Select((pixel, index) => Light(pixel, index)).ToArray();
        var sampledPixels = 0;
        for (var i = 0; i < count; i++)
        {
            var dirty = regions[i];
            if (dirty.IsEmpty || !new Rectangle(0, 0, width, height).Contains(dirty))
                throw new InvalidOperationException("Dirty regions must be clipped to the render target.");
            for (var j = 0; j < i; j++)
                if (dirty.Intersects(regions[j]))
                    throw new InvalidOperationException("Dirty output regions must not overlap.");
            var sample = GameSceneEffects.ShadowSampleRegion(dirty, width, height);
            sampledPixels += sample.Width * sample.Height;
            var local = ArrayPool<int>.Shared.Rent(sample.Width * sample.Height);
            try
            {
                for (var y = 0; y < sample.Height; y++)
                    Array.Copy(after, (sample.Y + y) * width + sample.X, local, y * sample.Width, sample.Width);
                GameSceneEffects.BlurShadowMask(local, scratch, sample.Width, sample.Height);
                for (var y = dirty.Top; y < dirty.Bottom; y++)
                {
                    Array.Copy(local, (y - sample.Y) * sample.Width + dirty.X - sample.X,
                        before, y * width + dirty.X, dirty.Width);
                    for (var x = dirty.Left; x < dirty.Right; x++)
                        lightBefore[y * width + x] = Light(before[y * width + x], y * width + x);
                }
            }
            finally { ArrayPool<int>.Shared.Return(local); }
        }
        var fullSample = GameSceneEffects.ShadowSampleRegion(
            GameSceneEffects.ShadowDirtyRegion(union, Rectangle.Empty, width, height), width, height);
        if (maxAreaFraction < 1 && sampledPixels > fullSample.Width * fullSample.Height * maxAreaFraction)
            throw new InvalidOperationException("Separated actors must avoid blurring the large unchanged area between them.");
        if (!before.SequenceEqual(expected) ||
            !lightBefore.SequenceEqual(expected.Select((pixel, index) => Light(pixel, index))))
            throw new InvalidOperationException("Regional shadows and relighting must exactly match a full redraw, including old silhouettes, overlaps and edge blur halos.");

        static Color Light(int shadow, int index)
        {
            var sunlight = LiveWallpaperDayCycle.Resolve(4, 0);
            var lit = sunlight.AtOcclusion(((uint)shadow >> 24) / 255f);
            // Deterministic spatial lamp coverage to check the light-map update footprint.
            return Color.Lerp(lit, new Color(255, 200, 200), (index % 31) / 31f);
        }
        static void Fill(int[] buffer, Rectangle bounds)
        {
            bounds = Rectangle.Intersect(bounds, new Rectangle(0, 0, width, height));
            for (var y = bounds.Top; y < bounds.Bottom; y++)
            for (var x = bounds.Left; x < bounds.Right; x++)
                buffer[y * width + x] = unchecked((int)0xCF000000);
        }
    }

    private static void CheckOptimizedBlur()
    {
        var random = new Random(2121);
        // Small dimensions exercise every clamped edge and overlapping kernel;
        // odd and full-target sizes cover strips, actor regions and refreshes.
        for (var width = 1; width <= 12; width++)
        for (var height = 1; height <= 10; height++)
            Compare(width, height);
        foreach (var (width, height) in new[] { (33, 65), (160, 96), (513, 257), (512, 960) })
            Compare(width, height);

        var pixels = new int[32 * 32];
        var scratch = new float[pixels.Length];
        for (var pass = 0; pass < 10; pass++)
            GameSceneEffects.BlurShadowMask(pixels, scratch, 32, 32);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 100; pass++)
            GameSceneEffects.BlurShadowMask(pixels, scratch, 32, 32);
        if (GC.GetAllocatedBytesForCurrentThread() != allocated)
            throw new InvalidOperationException("Shadow blur must not allocate per update.");

        void Compare(int width, int height)
        {
            var count = width * height;
            var actual = new int[count + 17];
            for (var i = 0; i < count; i++)
                actual[i] = (random.Next(256) << 24) | random.Next(0x1000000);
            Array.Fill(actual, unchecked((int)0xDEADBEEF), count, 17);
            var expected = (int[])actual.Clone();
            var scratch = new float[count + 17];
            Array.Fill(scratch, float.NaN);
            var expectedScratch = (float[])scratch.Clone();
            BlurShadowMaskReference(expected, expectedScratch, width, height);
            GameSceneEffects.BlurShadowMask(actual, scratch, width, height);
            if (!actual.AsSpan().SequenceEqual(expected) ||
                !scratch.AsSpan().SequenceEqual(expectedScratch))
                throw new InvalidOperationException($"Optimized blur must exactly match the original pixels and intermediate samples at {width}x{height}, including untouched tails.");
        }
    }

    // Frozen pre-optimization implementation: test oracle, not another renderer.
    internal static void BlurShadowMaskReference(int[] pixels, float[] scratch, int width, int height)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var row = y * width;
            float A(int dx) => (uint)pixels[row + Math.Clamp(x + dx, 0, width - 1)] >> 24;
            scratch[row + x] = A(0) * GameSceneEffects.ShadowBlurNearWeight +
                (A(-1) + A(1)) * (GameSceneEffects.ShadowBlurNearWeight * 0.5f) +
                (A(-2) + A(2) + A(-3) + A(3)) * (GameSceneEffects.ShadowBlurFarWeight * 0.5f);
        }
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            float A(int dy) => scratch[Math.Clamp(y + dy, 0, height - 1) * width + x];
            var alpha = A(0) * GameSceneEffects.ShadowBlurNearWeight +
                (A(-1) + A(1)) * (GameSceneEffects.ShadowBlurNearWeight * 0.5f) +
                (A(-2) + A(2) + A(-3) + A(3)) * (GameSceneEffects.ShadowBlurFarWeight * 0.5f);
            pixels[y * width + x] = (int)Math.Clamp(alpha * GameSceneEffects.ShadowOpacity, 0f, 255f) << 24;
        }
    }

    private static void CheckPooledShadowBuffers()
    {
        // Alternate full refreshes, thin exposed strips and tiny actor updates.
        // Poison both unused tails to detect a blur reading beyond its sample.
        var random = new Random(2119);
        foreach (var (width, height) in new[] { (160, 96), (1, 1), (3, 96), (160, 2), (19, 23), (1, 9) })
        {
            var count = width * height;
            var expected = Enumerable.Range(0, count).Select(_ => random.Next(256) << 24).ToArray();
            var pixels = ArrayPool<int>.Shared.Rent(count);
            var scratch = new float[count + 31];
            try
            {
                Array.Fill(pixels, unchecked((int)0xDEADBEEF));
                Array.Fill(scratch, float.NaN);
                expected.CopyTo(pixels, 0);
                GameSceneEffects.BlurShadowMask(expected, new float[count], width, height);
                GameSceneEffects.BlurShadowMask(pixels, scratch, width, height);
                if (!pixels.AsSpan(0, count).SequenceEqual(expected) ||
                    pixels.Skip(count).Any(pixel => pixel != unchecked((int)0xDEADBEEF)) ||
                    scratch.Skip(count).Any(value => !float.IsNaN(value)))
                    throw new InvalidOperationException("Pooled shadow samples must match exact-sized buffers without accessing unused tails.");
            }
            finally { ArrayPool<int>.Shared.Return(pixels); }
        }
    }

    private static void CheckPartialBlur(Rectangle previous, Rectangle current)
    {
        const int width = 160, height = 96;
        var random = new Random(97);
        var background = Enumerable.Range(0, width * height)
            .Select(_ => random.Next(256) << 24).ToArray();
        var before = (int[])background.Clone();
        var after = (int[])background.Clone();
        Fill(before, previous);
        Fill(after, current);
        var expected = (int[])after.Clone();
        GameSceneEffects.BlurShadowMask(before, new float[before.Length], width, height);
        GameSceneEffects.BlurShadowMask(expected, new float[expected.Length], width, height);
        var dirty = GameSceneEffects.ShadowDirtyRegion(previous, current, width, height);
        var sample = GameSceneEffects.ShadowSampleRegion(dirty, width, height);
        var local = new int[sample.Width * sample.Height];
        for (var y = 0; y < sample.Height; y++)
            Array.Copy(after, (sample.Y + y) * width + sample.X, local, y * sample.Width, sample.Width);
        GameSceneEffects.BlurShadowMask(local, new float[local.Length], sample.Width, sample.Height);
        for (var y = dirty.Top; y < dirty.Bottom; y++)
            Array.Copy(local, (y - sample.Y) * sample.Width + dirty.X - sample.X,
                before, y * width + dirty.X, dirty.Width);
        if (!before.SequenceEqual(expected))
            throw new InvalidOperationException(
                "Partial shadow updates must exactly match full blur, preserving static shadows and clearing moved/disappeared silhouettes even at viewport edges.");

        static void Fill(int[] buffer, Rectangle bounds)
        {
            bounds = Rectangle.Intersect(bounds, new Rectangle(0, 0, width, height));
            for (var y = bounds.Top; y < bounds.Bottom; y++)
            for (var x = bounds.Left; x < bounds.Right; x++)
                buffer[y * width + x] = unchecked((int)0xFF000000);
        }
    }
}
