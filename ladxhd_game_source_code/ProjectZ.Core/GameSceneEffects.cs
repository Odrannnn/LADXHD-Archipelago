using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public readonly record struct SceneEffectScroll(Rectangle Source, Rectangle Destination, Rectangle Reusable)
    {
        public int OffsetX => Destination.X - Source.X;
        public int OffsetY => Destination.Y - Source.Y;

        // Non-overlapping strips around the reusable centre. Keep them separate
        // from actor dirty bounds: merging touching L-shaped strips wastes the centre.
        public int WriteExposedRegions(Span<Rectangle> regions, int width, int height)
        {
            if (regions.Length < 4)
                throw new ArgumentException("Scrolling requires room for four edge strips.");
            var count = 0;
            if (Reusable.Top > 0)
                regions[count++] = new Rectangle(0, 0, width, Reusable.Top);
            if (Reusable.Bottom < height)
                regions[count++] = new Rectangle(0, Reusable.Bottom, width, height - Reusable.Bottom);
            if (Reusable.Left > 0)
                regions[count++] = new Rectangle(0, Reusable.Top, Reusable.Left, Reusable.Height);
            if (Reusable.Right < width)
                regions[count++] = new Rectangle(Reusable.Right, Reusable.Top, width - Reusable.Right, Reusable.Height);
            return count;
        }
    }

    /// <summary>Game shadow/light arithmetic, independent of a graphics device.</summary>
    public static class GameSceneEffects
    {
        public const float ShadowOpacity = 0.55f;
        public const float ShadowBlurNearWeight = 0.35f;
        public const float ShadowBlurFarWeight = 0.15f;
        public const int ShadowBlurRadius = 3;
        public const int MaxShadowDirtyRegions = 8;
        public const int LampSize = 160;
        public const int LampRed = 255;
        public const int LampGreen = 200;
        public const int LampBlue = 200;

        public static float SpriteShadowOpacity(float elevation) =>
            Math.Max(0f, 1f - elevation / 10f);

        public static float GroundShadowOpacity(float elevation) =>
            Math.Clamp(elevation, 0f, 1f);

        public static float SpriteShadowYOffset(float elevation, float offsetY = -1f) =>
            -elevation * 0.5f + offsetY;

        // FullShadowEffect's vertex transform: bottom edge stays fixed; the
        // top moves down by height*(1-heightScale), and sideways by height*rotation.
        public static Vector2 ProjectShadow(
            float x, float y, float spriteHeight, float heightScale, float rotation) =>
            new(x + (spriteHeight - y) * rotation,
                spriteHeight + (y - spriteHeight) * heightScale);

        public static float ShadowRenderScale(float cameraScale) =>
            cameraScale / Math.Clamp(cameraScale / 2f, 1f, 10f);

        public static bool TryGetEffectScroll(int width, int height, float offsetX, float offsetY,
            out SceneEffectScroll scroll)
        {
            scroll = default;
            // Only exact pixel translations preserve raster/blur samples. Large
            // jumps and fractional scales use the existing full-render fallback.
            if (!float.IsFinite(offsetX) || !float.IsFinite(offsetY) ||
                offsetX != MathF.Truncate(offsetX) || offsetY != MathF.Truncate(offsetY) ||
                MathF.Abs(offsetX) >= width || MathF.Abs(offsetY) >= height ||
                offsetX == 0 && offsetY == 0)
                return false;
            var dx = (int)offsetX;
            var dy = (int)offsetY;
            var source = new Rectangle(Math.Max(0, -dx), Math.Max(0, -dy),
                width - Math.Abs(dx), height - Math.Abs(dy));
            var destination = source;
            destination.Offset(dx, dy);
            var reusable = destination;
            // Exclude both old and new blur borders, where clamped sampling
            // differs from the neighbouring world's pixels after translation.
            reusable.Inflate(-ShadowBlurRadius, -ShadowBlurRadius);
            if (reusable.Width <= 0 || reusable.Height <= 0)
                return false;
            scroll = new SceneEffectScroll(source, destination, reusable);
            return true;
        }

        // Include the original and new silhouettes, then the shader's filter
        // footprint. The input rectangle needs another radius of unchanged
        // neighbouring pixels so a local blur equals a full-target blur.
        public static Rectangle ShadowDirtyRegion(Rectangle previous, Rectangle current,
            int width, int height)
        {
            var bounds = previous.IsEmpty ? current : current.IsEmpty ? previous :
                Rectangle.Union(previous, current);
            return ShadowSampleRegion(bounds, width, height);
        }

        public static Rectangle ShadowSampleRegion(Rectangle bounds, int width, int height)
        {
            if (bounds.IsEmpty)
                return Rectangle.Empty;
            bounds.Inflate(ShadowBlurRadius, ShadowBlurRadius);
            return Rectangle.Intersect(bounds, new Rectangle(0, 0, width, height));
        }

        // Keep distant moving silhouettes separate. Regions contain the blur
        // footprint and never overlap, so the same list can clip lamp composition.
        // Merge neighbours when one sampled rectangle costs no more than two;
        // the fixed-capacity fallback bounds work in crowded scenes without allocation.
        public static void AddShadowDirtyRegion(Span<Rectangle> regions, ref int count,
            Rectangle silhouette, int width, int height)
        {
            var dirty = ShadowSampleRegion(silhouette, width, height);
            if (dirty.IsEmpty)
                return;
            if (regions.IsEmpty)
                throw new ArgumentException("Shadow updates require a region buffer.");
            for (var i = 0; i < count;)
            {
                var other = regions[i];
                var union = Rectangle.Union(dirty, other);
                if (dirty.Intersects(other) ||
                    SampleArea(union, width, height) <=
                    SampleArea(dirty, width, height) + SampleArea(other, width, height))
                {
                    dirty = union;
                    regions[i] = regions[--count];
                    i = 0; // The enlarged bounds may now meet an earlier region.
                }
                else
                    i++;
            }
            if (count == regions.Length)
            {
                for (var i = 0; i < count; i++)
                    dirty = Rectangle.Union(dirty, regions[i]);
                count = 0;
            }
            regions[count++] = dirty;

            static long SampleArea(Rectangle bounds, int width, int height)
            {
                var sample = ShadowSampleRegion(bounds, width, height);
                return (long)sample.Width * sample.Height;
            }
        }

        // BlurH/BlurV sample +/-0.5 and +/-2.5 texels with linear filtering.
        // At texel centres this is a seven-tap kernel. The buffers are reused by
        // the wallpaper; no per-frame allocation or generated artwork is needed.
        public static void BlurShadowMask(int[] pixels, float[] scratch, int width, int height)
        {
            if (width <= 0 || height <= 0 || pixels.Length < width * height ||
                scratch.Length < width * height)
                throw new ArgumentException("Invalid shadow buffer dimensions.");
            for (var y = 0; y < height; y++)
            {
                var row = y * width;
                // Slide the same seven alpha samples across this row. Only the
                // entering sample needs a read/clamp; keep the original sum order.
                float centre = (uint)pixels[row] >> 24;
                var left1 = centre;
                var left2 = centre;
                var left3 = centre;
                float right1 = (uint)pixels[row + Math.Min(1, width - 1)] >> 24;
                float right2 = (uint)pixels[row + Math.Min(2, width - 1)] >> 24;
                float right3 = (uint)pixels[row + Math.Min(3, width - 1)] >> 24;
                for (var x = 0; x < width; x++)
                {
                    scratch[row + x] = centre * ShadowBlurNearWeight +
                        (left1 + right1) * (ShadowBlurNearWeight * 0.5f) +
                        (left2 + right2 + left3 + right3) * (ShadowBlurFarWeight * 0.5f);
                    left3 = left2;
                    left2 = left1;
                    left1 = centre;
                    centre = right1;
                    right1 = right2;
                    right2 = right3;
                    right3 = (uint)pixels[row + Math.Min(x + 4, width - 1)] >> 24;
                }
            }
            for (var y = 0; y < height; y++)
            {
                // The clamped source rows are identical for every pixel in this
                // output row. Hoist their offsets, retaining contiguous access.
                var row = y * width;
                var above1 = Math.Max(y - 1, 0) * width;
                var above2 = Math.Max(y - 2, 0) * width;
                var above3 = Math.Max(y - 3, 0) * width;
                var below1 = Math.Min(y + 1, height - 1) * width;
                var below2 = Math.Min(y + 2, height - 1) * width;
                var below3 = Math.Min(y + 3, height - 1) * width;
                for (var x = 0; x < width; x++)
                {
                    var alpha = scratch[row + x] * ShadowBlurNearWeight +
                        (scratch[above1 + x] + scratch[below1 + x]) * (ShadowBlurNearWeight * 0.5f) +
                        (scratch[above2 + x] + scratch[below2 + x] + scratch[above3 + x] + scratch[below3 + x]) *
                        (ShadowBlurFarWeight * 0.5f);
                    pixels[row + x] = (int)Math.Clamp(alpha * ShadowOpacity, 0f, 255f) << 24;
                }
            }
        }

        // LightShader mode=0 and normal LightState=0 multiplies scene RGB by
        // light-map RGB. Alpha is deliberately opaque for Canvas Multiply.
        public static Color AmbientLight(int red, int green, int blue, int alpha) =>
            new Color(red, green, blue) * (alpha / 255f);
    }
}
