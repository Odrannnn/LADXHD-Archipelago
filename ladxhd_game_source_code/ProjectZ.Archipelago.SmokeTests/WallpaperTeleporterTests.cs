using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperTeleporterTests
{
    public static void Run()
    {
        var text = new StringBuilder("3\n0\n0\noverworld.png\n30\n10\n1\n");
        for (var row = 0; row < 10; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", 30)));
        text.Append("1\noverworldTeleporter\n2\n")
            .AppendLine("0;64;64;0")
            .AppendLine("0;320;64;1");
        Check(LiveWallpaperMap.TryLoad(
                new StringReader(text.ToString()), out var map),
            "Overworld teleporter fixture must load.");
        Check(map.Portals.Count == 2 &&
              map.Portals.All(portal => portal.IsOverworldTeleporter &&
                  portal.IsHoleTeleporter) &&
              map.IntersectsHole(68, 67, 8, 10),
            "ObjOverworldTeleporter must retain its exact inset Hole collider and ids.");
        Check(map.TryGetOtherOverworldTeleporter(0, 0, out var destination) &&
              destination.TeleporterId == 1 &&
              destination.PixelX == 320 && destination.PixelY == 64,
            "A world teleporter must select a different installed destination.");

        Check(LiveWallpaperMapViewport.TryCreateCentered(
                160, 128, map.Width, map.Height,
                72, 77, 0.5f, out var viewport),
            "Teleporter fixture viewport must be valid.");
        Check(LiveWallpaperJourneyPlanner.TryCreateOverworldTeleporterPlan(
                map, viewport, 40, 77, 3, out var plan) &&
              plan.Points.Count > 1 &&
              plan.Points[^1].Action ==
                  LiveWallpaperJourneyAction.EnterTeleporter &&
              plan.Points[^1].PixelX == 72 &&
              plan.Points[^1].PixelY == 77,
            "Autonomous world travel must deliberately enter a reachable portal instead of jumping over it.");

        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(40, 77);
        simulation.UpdateJourney(
            1, 0, 0, true, map, viewport, true,
            followLoadingZones: true, allowViewportFollow: true,
            holeFallAnimationMilliseconds: 850);
        Check(simulation.TryWalkTo(map, viewport, 72, 77),
            "A portal tap route must be accepted.");
        var sawFall = false;
        var sawRise = false;
        var sawDestinationFall = false;
        var arrived = false;
        var movedAfterArrival = false;
        var minimumDestinationDistance = float.MaxValue;
        var maximumRiseHeight = 0f;
        var maximumFallHeight = 0f;
        LiveWallpaperSimulatedLinkState lastState = default;
        for (var frame = 1; frame < 1200; frame++)
        {
            var state = simulation.UpdateJourney(
                1, 0, frame * 17L, true, map, viewport, true,
                followLoadingZones: true, allowViewportFollow: true,
                holeFallAnimationMilliseconds: 850);
            lastState = state;
            if (state.Action == LiveWallpaperLinkRouteAction.Falling)
            {
                sawFall = true;
                continue;
            }
            sawRise |= state.Action ==
                       LiveWallpaperLinkRouteAction.TeleporterUp &&
                       state.Height > 0;
            if (state.Action == LiveWallpaperLinkRouteAction.TeleporterUp)
                maximumRiseHeight = Math.Max(maximumRiseHeight, state.Height);
            sawDestinationFall |= state.Action ==
                                  LiveWallpaperLinkRouteAction.TeleporterFall &&
                                  state.Height > 0;
            if (state.Action == LiveWallpaperLinkRouteAction.TeleporterFall)
                maximumFallHeight = Math.Max(maximumFallHeight, state.Height);
            minimumDestinationDistance = Math.Min(
                minimumDestinationDistance,
                Vector2.Distance(
                    new Vector2(state.MapX * 16f, state.MapY * 16f),
                    new Vector2(328f, 102f)));
            if (sawDestinationFall && state.Height <= 0.001f &&
                Vector2.Distance(
                    new Vector2(state.MapX * 16f, state.MapY * 16f),
                    new Vector2(328f, 102f)) < 8f)
                arrived = true;
            if (arrived && Vector2.Distance(
                    new Vector2(state.MapX * 16f, state.MapY * 16f),
                    new Vector2(328f, 102f)) >= 8f)
            {
                movedAfterArrival = true;
                break;
            }
        }
        Check(sawFall && sawRise && sawDestinationFall && arrived &&
              movedAfterArrival,
            $"After the hole fall, Link must use ObjLink's rise/fade and destination-fall sequence, land at the exact world-teleport spawn, then start a fresh route away (fall={sawFall}, rise={sawRise}/{maximumRiseHeight}, destinationFall={sawDestinationFall}/{maximumFallHeight}, arrived={arrived}, moved={movedAfterArrival}, minDistance={minimumDestinationDistance}, last={lastState.Action}@{lastState.MapX * 16f},{lastState.MapY * 16f},z={lastState.Height}).");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
