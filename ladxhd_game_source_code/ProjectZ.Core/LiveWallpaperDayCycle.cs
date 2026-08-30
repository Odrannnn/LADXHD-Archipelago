using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public readonly record struct LiveWallpaperSunlight(
        Vector3 Ambient, Vector3 Direct, float ShadowHeightMultiplier, float ShadowRotationOffset)
    {
        public bool HasDirectLight => Direct.LengthSquared() > 0.000001f;

        // Per-pixel illumination, not a screen overlay: occlusion removes only
        // direct sunlight. Local light textures are composited afterwards, so
        // a lamp can illuminate a shaded pixel without changing the sky light.
        public Color AtOcclusion(float coverage) => new(Vector3.Clamp(
            Ambient + Direct * (1f - Math.Clamp(coverage, 0f, 1f)), Vector3.Zero, Vector3.One));
    }

    public static class LiveWallpaperDayCycle
    {
        public const int DefaultSunrise = 6 * 60;
        public const int DefaultSunset = 19 * 60;
        public const int OriginalLightingMode = 5;

        public static bool IsValidSchedule(int sunrise, int sunset) =>
            sunrise >= 0 && sunset < 24 * 60 && sunset - sunrise >= 120 && sunset - sunrise <= 1320;

        public static bool UsesSunlight(int mode, string mapName) =>
            mode != OriginalLightingMode && string.Equals(mapName, "overworld.map",
                StringComparison.OrdinalIgnoreCase);

        public static LiveWallpaperSunlight Resolve(int mode, double localMinutes,
            int sunrise = DefaultSunrise, int sunset = DefaultSunset)
        {
            if (!IsValidSchedule(sunrise, sunset))
            {
                sunrise = DefaultSunrise;
                sunset = DefaultSunset;
            }
            // Keep the pure calculation minute-resolved for previews. The
            // renderer's cache below samples automatic lighting every 10 minutes.
            var minute = double.IsFinite(localMinutes) ?
                (float)Math.Floor((localMinutes % 1440 + 1440) % 1440) : 720f;
            minute = mode switch
            {
                1 => (sunrise + sunset) * 0.5f,
                2 => sunset - 15f,
                3 => (sunset + (1440 - sunset + sunrise) * 0.5f) % 1440,
                4 => sunrise + 15f,
                _ => minute
            };
            var noon = (sunrise + sunset) * 0.5f;
            minute = noon + ((minute - noon + 720f) % 1440f + 1440f) % 1440f - 720f;
            // This cycle is a new wallpaper feature, not a claimed original
            // game sun model. It drives the existing 2D shadow projection and
            // installed light textures; it does not invent normal maps or art.
            var dayProgress = Math.Clamp((minute - sunrise) / (sunset - sunrise), 0f, 1f);
            var altitude = MathF.Sin(dayProgress * MathF.PI);
            var daylight = Smooth(sunrise - 30f, sunrise + 45f, minute) *
                (1f - Smooth(sunset - 45f, sunset + 30f, minute));
            var skyDaylight = Smooth(sunrise - 60f, sunrise + 90f, minute) *
                (1f - Smooth(sunset - 90f, sunset + 60f, minute));
            var ambient = Vector3.Lerp(new Vector3(0.62f, 0.68f, 0.82f),
                new Vector3(0.46f, 0.53f, 0.64f), skyDaylight);
            var highSun = Smooth(0f, 0.65f, altitude);
            var direct = Vector3.Lerp(new Vector3(0.54f, 0.25f, 0.10f),
                new Vector3(0.54f, 0.47f, 0.36f), highSun) * daylight;
            direct = Vector3.Min(direct, Vector3.One - ambient);
            return new(ambient, direct,
                1.6f + (0.45f - 1.6f) * altitude,
                -0.65f * MathF.Cos(dayProgress * MathF.PI));
        }

        private static float Smooth(float start, float end, float value)
        {
            var t = Math.Clamp((value - start) / (end - start), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }

    public sealed class LiveWallpaperSunlightCache
    {
        public const int UpdateIntervalMinutes = 10;
        private (bool Enabled, int Mode, int Minute, int Sunrise, int Sunset)? _key;
        public bool Enabled { get; private set; }
        public LiveWallpaperSunlight Value { get; private set; }

        // Returns false without running the solar calculation when inputs match.
        // Wall-clock buckets also handle resume, midnight and clock corrections;
        // there is no background timer. Fixed previews ignore clock changes.
        public bool Update(int mode, double localMinutes, int sunrise, int sunset, string mapName)
        {
            var enabled = LiveWallpaperDayCycle.UsesSunlight(mode, mapName);
            if (!LiveWallpaperDayCycle.IsValidSchedule(sunrise, sunset))
            {
                sunrise = LiveWallpaperDayCycle.DefaultSunrise;
                sunset = LiveWallpaperDayCycle.DefaultSunset;
            }
            var minute = enabled && mode == 0
                ? double.IsFinite(localMinutes)
                    ? (int)(Math.Floor(((localMinutes % 1440 + 1440) % 1440) /
                        UpdateIntervalMinutes) * UpdateIntervalMinutes)
                    : 720
                : 0;
            var key = (enabled, mode, minute, sunrise, sunset);
            if (_key == key)
                return false;
            _key = key;
            Enabled = enabled;
            Value = enabled ? LiveWallpaperDayCycle.Resolve(mode, minute, sunrise, sunset) : default;
            return true;
        }
    }
}
