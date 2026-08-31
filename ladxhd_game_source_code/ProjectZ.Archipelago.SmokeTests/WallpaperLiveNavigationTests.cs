using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperLiveNavigationTests
{
    public static void Run()
    {
        LiveActorBodiesMoveAndStopBlocking();
        MouseBodyUsesItsCanonicalEntityOrigin();
        LiveEnemyBodiesMoveAndDisappear();
        MovedBodiesKeepTheirNearestApproachSide();
        SimulationObservesLateLiveStateWithoutReplacingNavigationView();
        FailedStepsAreBidirectionalBoundedAndExpire();
    }

    private static void LiveActorBodiesMoveAndStopBlocking()
    {
        var map = LoadMap();
        Check(map.TryGetActorBody(0, out var staticBody), "Fixture must expose its static dog body.");
        var actors = new Dictionary<int, LiveWallpaperActorState>
        {
            [0] = new(224, 144, 0, 2, LiveWallpaperActorAction.Walk)
        };
        var navigation = map.WithNavigationState(new Dictionary<int, Vector2>(), new HashSet<int>(),
            actors, null, null);
        Check(navigation.TryGetActorBody(0, out var moved) &&
              !Overlaps(moved, staticBody) && moved.X > staticBody.X,
            "A live moved actor must free its old footprint and block at its new body.");

        actors[0] = new LiveWallpaperActorState(224, 144, 0, 2, LiveWallpaperActorAction.Fly);
        Check(!navigation.TryGetActorBody(0, out _),
            "A live nonblocking actor state must remove the stale navigation body.");
    }

    private static void LiveEnemyBodiesMoveAndDisappear()
    {
        var map = LoadMap();
        Check(map.TryGetEnemyBody(0, out var staticBody), "Fixture must expose its static enemy body.");
        var enemies = new Dictionary<int, LiveWallpaperEnemyState>
        {
            [0] = new(240, 160, 2, LiveWallpaperEnemyAction.Walk)
        };
        var navigation = map.WithNavigationState(new Dictionary<int, Vector2>(), new HashSet<int>(),
            null, enemies, null);
        Check(navigation.TryGetEnemyBody(0, out var moved) &&
              !Overlaps(moved, staticBody) && moved.X > staticBody.X,
            "A live enemy state must use its current body instead of its spawn rectangle.");

        enemies[0] = new LiveWallpaperEnemyState(240, 160, 2, LiveWallpaperEnemyAction.Hidden);
        Check(!navigation.TryGetEnemyBody(0, out _),
            "A dead or hidden live enemy must remove its stale body rather than fall back to spawn.");
    }

    private static void MouseBodyUsesItsCanonicalEntityOrigin()
    {
        var map = LoadMouseMap();
        Check(map.TryGetActorBody(0, out var spawn),
            "Mouse fixture must expose its canonical static body.");
        var actors = new Dictionary<int, LiveWallpaperActorState>
        {
            [0] = new(176, 172, 0, 2, LiveWallpaperActorAction.Walk)
        };
        var navigation = map.WithNavigationState(new Dictionary<int, Vector2>(), new HashSet<int>(),
            actors, null, null);
        Check(navigation.TryGetActorBody(0, out var live) &&
              live.X == spawn.X + 40 && live.Y == spawn.Y + 32,
            "Mouse live navigation must translate from its canonical +8,+12 entity origin.");
    }

    private static void MovedBodiesKeepTheirNearestApproachSide()
    {
        var map = LoadMap();
        var dogState = new LiveWallpaperActorState(224, 144, 0, 2, LiveWallpaperActorAction.Walk);
        Check(map.TryGetActorBody(0, out var dogSpawn),
            "Moved-dog approach fixture must resolve its canonical spawn body.");
        Check(LiveWallpaperMap.TryGetLiveActorBody(map.Actors[0], dogState, out var dogLive),
            "Moved-dog approach fixture must resolve both canonical bodies.");
        var dogApproach = LiveWallpaperActorSimulation.ResolveInteractionApproach(
            map.Actors[0], dogState, new Vector2(dogLive.X - 24, dogLive.Y + 5));
        Check(dogApproach.X < dogLive.X && dogApproach.X > dogSpawn.Right,
            "A moved dog approached from its left must use the left side of its live body, not spawn geometry.");

        var simulation = new LiveWallpaperLinkSimulation();
        simulation.UpdateLiveEnemyState(map, 0,
            new LiveWallpaperEnemyState(240, 160, 2, LiveWallpaperEnemyAction.Walk));
        var resolveEnemy = typeof(LiveWallpaperLinkSimulation).GetMethod("ResolveLiveEnemyApproach",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var enemyApproach = (Vector2)resolveEnemy.Invoke(simulation,
            [map, 0, new Vector2(208, 158)])!;
        var navigation = map.WithNavigationState(new Dictionary<int, Vector2>(), new HashSet<int>(), null,
            new Dictionary<int, LiveWallpaperEnemyState>
            {
                [0] = new(240, 160, 2, LiveWallpaperEnemyAction.Walk)
            }, null);
        Check(navigation.TryGetEnemyBody(0, out var enemyLive) &&
              enemyApproach.X < enemyLive.X,
            "A moved enemy approached from its left must use the left side of its live body.");
    }

    private static void SimulationObservesLateLiveStateWithoutReplacingNavigationView()
    {
        var map = LoadMap();
        var viewport = CreateViewport(map);
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(64, 144);
        simulation.UpdateJourney(1, 0, 0, true, map, viewport, false);
        simulation.UpdateLiveActorState(map, 0,
            new LiveWallpaperActorState(224, 144, 0, 2, LiveWallpaperActorAction.Walk));
        simulation.UpdateLiveEnemyState(map, 0,
            new LiveWallpaperEnemyState(240, 160, 2, LiveWallpaperEnemyAction.Hidden));
        Check(simulation.TryWalkTo(map, viewport, 304, 144),
            "A late first live-state frame must invalidate static navigation and still plan a route.");
        var navigationField = typeof(LiveWallpaperLinkSimulation).GetField("_navigationMap",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var navigation = (LiveWallpaperMap)navigationField.GetValue(simulation)!;
        Check(navigation.TryGetActorBody(0, out var liveActor) &&
              !navigation.IntersectsActor(130, 136, 12, 8),
            "The first live actor state must invalidate the static navigation view and free its old footprint.");

        var vacatedPlan = LiveWallpaperJourneyPlanner.CreateToPoint(
            navigation, viewport, 96, 144, 176, 144);
        Check(vacatedPlan.Points.Count > 1 && vacatedPlan.Points.Any(point =>
                Overlaps(new LiveWallpaperCollisionBounds(point.PixelX - 4, point.PixelY - 10, 8, 10),
                    new LiveWallpaperCollisionBounds(130, 136, 12, 8))),
            "A route must be able to use the actor's vacated spawn footprint.");
        var blockedPlan = LiveWallpaperJourneyPlanner.CreateToPoint(
            navigation, viewport, 192, 144, 280, 144);
        Check(blockedPlan.Points.Count > 1 && blockedPlan.Points.All(point =>
                !Overlaps(new LiveWallpaperCollisionBounds(point.PixelX - 4, point.PixelY - 10, 8, 10),
                    liveActor)),
            "A route must avoid the actor's newly reported live body.");

        simulation.UpdateLiveActorState(map, 0,
            new LiveWallpaperActorState(240, 144, 0, 2, LiveWallpaperActorAction.Walk));
        Check(ReferenceEquals(navigation, navigationField.GetValue(simulation)),
            "Subsequent live-state updates for the same map must reuse the navigation view.");
        Check(navigation.TryGetActorBody(0, out var shiftedActor) &&
              shiftedActor.X == liveActor.X + 16 &&
              !navigation.IntersectsActor(liveActor.X, liveActor.Y, liveActor.Width, liveActor.Height) &&
              navigation.IntersectsActor(
                  shiftedActor.X, shiftedActor.Y, shiftedActor.Width, shiftedActor.Height),
            "The shared live dictionaries must move the cached navigation body without retaining its old footprint.");
    }

    private static void FailedStepsAreBidirectionalBoundedAndExpire()
    {
        var map = LoadMap();
        var failed = new Dictionary<(Point From, Point To), long>
        {
            [(new Point(64, 128), new Point(72, 128))] = 15_100
        };
        var navigation = map.WithNavigationState(new Dictionary<int, Vector2>(), new HashSet<int>(),
            null, null, failed);
        Check(navigation.GetNavigationStepPenalty(new Point(64, 128), new Point(72, 128)) > 0 &&
              navigation.GetNavigationStepPenalty(new Point(72, 128), new Point(64, 128)) > 0,
            "A remembered failed step must penalize both route directions without becoming a wall.");

        var detour = LiveWallpaperJourneyPlanner.CreateToPoint(
            navigation, CreateViewport(map), 64, 128, 96, 128);
        Check(detour.Points.Count > 2 && !detour.Points.Any(point =>
                point.PixelX == 72 && point.PixelY == 128),
            "A failed edge must make an equally traversable detour preferable.");

        var solePassage = LoadMap(corridor: true).WithNavigationState(
            new Dictionary<int, Vector2>(), new HashSet<int>(), null, null,
            new Dictionary<(Point From, Point To), long>
            {
                [(new Point(64, 128), new Point(72, 128))] = 15_100
            });
        var solePlan = LiveWallpaperJourneyPlanner.CreateToPoint(
            solePassage, CreateViewport(solePassage), 64, 128, 96, 128);
        Check(solePlan.Points.Count > 1 && solePlan.Points.Any(point =>
                point.PixelX == 72 && point.PixelY == 128),
            "A failed edge must remain traversable when it is the sole corridor passage.");

        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(100, 100);
        var remember = typeof(LiveWallpaperLinkSimulation).GetMethod("RememberFailedJourneyStep",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var expire = typeof(LiveWallpaperLinkSimulation).GetMethod("ExpireFailedJourneySteps",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        remember.Invoke(simulation, [new Vector2(99, 100), 0L]);
        var stored = (Dictionary<(Point From, Point To), long>)typeof(LiveWallpaperLinkSimulation)
            .GetField("_failedJourneySteps", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(simulation)!;
        Check(stored.ContainsKey((new Point(104, 96), new Point(96, 96))),
            "A subgrid stall must retain its incoming grid edge instead of rounding it away.");
        for (var index = 1; index < 10; index++)
        {
            var x = 100 + index * 16;
            simulation.Body.Position.Set(new Vector2(x, 100));
            remember.Invoke(simulation, [new Vector2(x - 1, 100), (long)index]);
        }
        Check(stored.Count == 8 && !stored.ContainsKey((new Point(104, 96), new Point(96, 96))) &&
              stored.ContainsKey((new Point(248, 96), new Point(240, 96))),
            "Failed-step memory must keep eight distinct recent edges and evict the oldest.");
        expire.Invoke(simulation, [15_009L]);
        Check(stored.Count == 0,
            "Failed-step penalties must expire after their bounded lifetime.");
        remember.Invoke(simulation, [new Vector2(92, 100), 1L]);
        simulation.EnterMap(200, 200);
        Check(stored.Count == 0,
            "Entering a map must clear failed-step memory rather than carry routing bias across maps.");
    }

    private static LiveWallpaperMap LoadMap(bool corridor = false)
    {
        const int width = 24, height = 20;
        var text = new StringBuilder($"3\n0\n0\nlive-navigation.png\n{width}\n{height}\n1\n");
        for (var row = 0; row < height; row++) text.AppendLine(string.Join(',', Enumerable.Repeat("0", width)));
        text.AppendLine("3");
        text.AppendLine("c1");
        text.AppendLine("dogo");
        text.AppendLine("e2");
        text.AppendLine((corridor ? 50 : 2).ToString());
        if (corridor)
            for (var x = 0; x < width * 16; x += 16)
            {
                text.AppendLine($"0;{x};96");
                text.AppendLine($"0;{x};128");
            }
        text.AppendLine("1;128;128");
        text.AppendLine("2;160;128");
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map),
            "Live-navigation fixture map must parse.");
        return map;
    }

    private static LiveWallpaperMapViewport CreateViewport(LiveWallpaperMap map)
    {
        Check(LiveWallpaperMapViewport.TryCreateCentered(1080, 2400, map.Width, map.Height,
                96, 128, .5f, out var viewport), "Live-navigation fixture viewport must parse.");
        return viewport;
    }

    private static LiveWallpaperMap LoadMouseMap()
    {
        const int width = 16, height = 16;
        var text = new StringBuilder($"3\n0\n0\nlive-mouse.png\n{width}\n{height}\n1\n");
        for (var row = 0; row < height; row++) text.AppendLine(string.Join(',', Enumerable.Repeat("0", width)));
        text.AppendLine("1");
        text.AppendLine("mouse");
        text.AppendLine("1");
        text.AppendLine("0;128;128");
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map),
            "Mouse live-navigation fixture map must parse.");
        return map;
    }

    private static bool Overlaps(LiveWallpaperCollisionBounds left, LiveWallpaperCollisionBounds right) =>
        left.X < right.Right && left.Right > right.X && left.Y < right.Bottom && left.Bottom > right.Y;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
