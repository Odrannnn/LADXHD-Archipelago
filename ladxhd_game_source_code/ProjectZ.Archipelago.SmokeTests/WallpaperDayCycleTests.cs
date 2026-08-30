using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperDayCycleTests
{
    public static void Run()
    {
        static void Check(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(message);
        }
        var dawn = LiveWallpaperDayCycle.Resolve(4, 0);
        var noon = LiveWallpaperDayCycle.Resolve(1, 0);
        var dusk = LiveWallpaperDayCycle.Resolve(2, 0);
        var night = LiveWallpaperDayCycle.Resolve(3, 0);
        Check(dawn.ShadowRotationOffset < 0 && dusk.ShadowRotationOffset > 0 &&
            Math.Abs(noon.ShadowRotationOffset) < 0.001f &&
            dawn.ShadowHeightMultiplier > noon.ShadowHeightMultiplier &&
            dusk.ShadowHeightMultiplier > noon.ShadowHeightMultiplier,
            "Sunrise/sunset must cast longer shadows in opposite directions, with short noon shadows.");
        Check(!night.HasDirectLight && night.AtOcclusion(0) == night.AtOcclusion(1) &&
            dawn.HasDirectLight && noon.HasDirectLight && dusk.HasDirectLight,
            "Solar shadows must stop affecting illumination at night, leaving ambient and local lights.");
        var lit = dawn.AtOcclusion(0);
        var shaded = dawn.AtOcclusion(GameSceneEffects.ShadowOpacity);
        Check(lit != shaded && lit.R > shaded.R &&
            shaded.B * lit.R > lit.B * shaded.R &&
            dawn.AtOcclusion(1) == new Color(dawn.Ambient),
            "Sunrise lighting must be spatial: cast shadows remove warm direct light, not the cool ambient component.");
        // Canvas draws the ambient-colored shadow mask over unoccluded light.
        // Verify that this blend agrees with the shared per-pixel equation.
        for (var i = 0; i <= 10; i++)
        {
            var coverage = i / 10f;
            var composed = Color.Lerp(dawn.AtOcclusion(0), dawn.AtOcclusion(1), coverage);
            var expected = dawn.AtOcclusion(coverage);
            Check(Math.Abs(composed.R - expected.R) <= 1 &&
                Math.Abs(composed.G - expected.G) <= 1 && Math.Abs(composed.B - expected.B) <= 1,
                "Ambient-colored alpha-mask composition must match ambient + direct*(1-occlusion).");
        }
        var lampColor = new Color(255, 200, 200);
        var nearLamp = Color.Lerp(shaded, lampColor, 0.8f);
        Check(nearLamp.R > shaded.R && nearLamp.R > nearLamp.B,
            "A local lamp blended after solar occlusion must illuminate shaded pixels independently of sunlight.");
        Check(LiveWallpaperDayCycle.Resolve(0, 370.01) == LiveWallpaperDayCycle.Resolve(0, 370.99) &&
            LiveWallpaperDayCycle.Resolve(0, 370) != LiveWallpaperDayCycle.Resolve(0, 371) &&
            LiveWallpaperDayCycle.Resolve(0, 0) == LiveWallpaperDayCycle.Resolve(0, 1440) &&
            LiveWallpaperDayCycle.Resolve(0, -1) == LiveWallpaperDayCycle.Resolve(0, 1439),
            "The sun cache must be minute-stable, update with the local clock and wrap midnight correctly.");
        Check(LiveWallpaperDayCycle.Resolve(0, 6 * 60, 7 * 60, 20 * 60).HasDirectLight == false &&
            LiveWallpaperDayCycle.Resolve(4, 0, 7 * 60, 20 * 60) ==
                LiveWallpaperDayCycle.Resolve(0, 7 * 60 + 15, 7 * 60, 20 * 60),
            "Custom sunrise/sunset times and fixed previews must use the same cycle.");
        Check(LiveWallpaperDayCycle.IsValidSchedule(360, 1140) &&
            !LiveWallpaperDayCycle.IsValidSchedule(1140, 360) &&
            !LiveWallpaperDayCycle.IsValidSchedule(360, 400) &&
            !LiveWallpaperDayCycle.IsValidSchedule(0, 1439) &&
            LiveWallpaperDayCycle.Resolve(0, 370, 1140, 360) == LiveWallpaperDayCycle.Resolve(0, 370) &&
            LiveWallpaperDayCycle.Resolve(0, double.NaN) == LiveWallpaperDayCycle.Resolve(0, 720),
            "Invalid schedules and clock inputs must fall back safely without non-finite shader parameters.");
        Check(LiveWallpaperDayCycle.UsesSunlight(0, "overworld.map") &&
            !LiveWallpaperDayCycle.UsesSunlight(0, "house.map") &&
            !LiveWallpaperDayCycle.UsesSunlight(0, "dungeon1.map") &&
            !LiveWallpaperDayCycle.UsesSunlight(5, "overworld.map") &&
            !LiveWallpaperDayCycle.UsesSunlight(0, null),
            "Interiors and Original map lighting mode must retain canonical lighting without a solar overlay.");
        var cache = new LiveWallpaperSunlightCache();
        bool Update(double minute, int mode = 0, int sunrise = 360, int sunset = 1140,
            string map = "overworld.map") => cache.Update(mode, minute, sunrise, sunset, map);
        Check(Update(370.01) && cache.Enabled && cache.Value == LiveWallpaperDayCycle.Resolve(0, 370),
            "The first visible frame must initialize the ten-minute sun sample.");
        var heldSunlight = cache.Value;
        for (var second = 1; second < 600; second++)
            Check(!Update(370 + second / 60.0) && cache.Value == heldSunlight,
                "Repeated frames must reuse the sun calculation throughout the ten-minute bucket.");
        Check(Update(380) && cache.Value == LiveWallpaperDayCycle.Resolve(0, 380) &&
            cache.Value != heldSunlight && !Update(389.999),
            "Crossing the next ten-minute boundary must calculate sunlight exactly once.");
        Check(Update(379) && cache.Value == heldSunlight &&
            Update(600) && cache.Value == LiveWallpaperDayCycle.Resolve(0, 600),
            "Clock corrections and resume after hidden time must select the current sun sample.");
        Check(Update(1439.9) && !Update(-0.1) && Update(1440) && !Update(0),
            "Sun buckets must wrap midnight and negative clock inputs consistently.");
        Check(Update(720, mode: 4) && !Update(900, mode: 4) && cache.Value == dawn &&
            Update(720, mode: 2) && cache.Value == dusk,
            "Preview modes must update immediately on selection but never recalculate as the clock advances.");
        Check(Update(370, sunrise: 420, sunset: 1200) &&
            cache.Value == LiveWallpaperDayCycle.Resolve(0, 370, 420, 1200) &&
            Update(370) && cache.Value == heldSunlight,
            "Changing sunrise/sunset must invalidate the cache within the same ten-minute bucket.");
        Check(Update(370, map: "house.map") && !cache.Enabled && cache.Value == default &&
            !Update(500, map: "cave.map") && Update(370) && cache.Value == heldSunlight &&
            Update(370, mode: 5) && !cache.Enabled && Update(370) && cache.Enabled,
            "Interiors and Original map lighting must disable sunlight, then restore it upon returning outdoors.");
        Check(!Update(370, sunrise: 1140, sunset: 360) && Update(double.NaN) &&
            cache.Value == LiveWallpaperDayCycle.Resolve(0, 720) && !Update(double.PositiveInfinity),
            "Invalid schedules and clock values must use stable default cache keys.");
        for (var i = 0; i < 100; i++) Update(720); // Warm up before checking allocations.
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) Update(720);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated,
            "The unchanged solar-cache frame path must not allocate.");
        // Includes twilight spanning midnight for very early/late custom hours.
        foreach (var schedule in new[] { (360, 1140), (30, 780), (660, 1410) })
        {
            var previous = LiveWallpaperDayCycle.Resolve(0, 1439, schedule.Item1, schedule.Item2);
            for (var minute = 0; minute < 1440; minute++)
            {
                var current = LiveWallpaperDayCycle.Resolve(0, minute, schedule.Item1, schedule.Item2);
                Check(float.IsFinite(current.ShadowHeightMultiplier) &&
                    float.IsFinite(current.ShadowRotationOffset) &&
                    current.ShadowHeightMultiplier >= 0.449f && current.ShadowHeightMultiplier <= 1.601f &&
                    Vector3.Distance(previous.Ambient + previous.Direct, current.Ambient + current.Direct) < 0.04f,
                    "The cycle must remain finite and change smoothly at dawn, dusk and midnight.");
                previous = current;
            }
        }
    }
}
