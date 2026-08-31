using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperSideViewTests
{
    public static void Run()
    {
        CheckSharedMotionParity();

        var map = LoadSyntheticMap(includeCeiling: true);
        CheckGravityAndNoVerticalWalk(map);
        CheckLaddersAndOneWayTop(map);
        CheckJumpArcAndCeiling(map);
        CheckPortalTriggerAndArrivalLatch(map);
        CheckPlannerBoundedTargets(map);
        CheckRuntimeCadence(map);
        CheckCurrentTapRecovery(map);
        CheckUnreachableTapRecovery(map);
        CheckNoTopDownFallback(map);
        CheckInstalledDungeonRoutes();
    }

    private static void CheckSharedMotionParity()
    {
        Check(SideViewGameplayMotion.Gravity == .1f &&
              SideViewGameplayMotion.ClimbSpeed == .7f &&
              SideViewGameplayMotion.SwimSpeed == .5f,
            "Side-view wallpaper movement must share ObjLink2d gravity, climb, and swim constants.");
        Check(SideViewGameplayMotion.FeatherVelocity(true, false) == -1.5f &&
              SideViewGameplayMotion.FeatherVelocity(false, false) == -1.95f &&
              SideViewGameplayMotion.FeatherVelocity(false, true) == -2.1f,
            "Side-view wallpaper feather takeoff must share ObjLink2d's three launch velocities.");
        Check(SideViewGameplayMotion.LadderBounds(64, 80, false) == new Rectangle(68, 80, 8, 16) &&
              SideViewGameplayMotion.LadderBounds(64, 80, true) == new Rectangle(64, 80, 16, 16) &&
              SideViewGameplayMotion.LadderCollides(false, 1) &&
              !SideViewGameplayMotion.LadderCollides(true, 1) &&
              SideViewGameplayMotion.LadderCollides(true, 3),
            "Ladder bodies and tops must retain ObjLadder's exact collision geometry and direction rule.");
        var releaseVelocity = -.9f;
        var variableJump = true;
        SideViewGameplayMotion.ReleaseFeather(ref releaseVelocity, ref variableJump, held: false);
        Check(releaseVelocity == -.5f && !variableJump,
            "Releasing Feather early must retain the canonical variable-height jump cut.");
        var air = SideViewGameplayMotion.AirMovement(Vector2.UnitX, -Vector2.UnitX, 1, 1);
        Check(Math.Abs(air.X - .95f) < .0001f && air.Y == 0,
            "Air steering must retain ObjLink2d's bounded 0.05 velocity correction.");
        var swim = SideViewGameplayMotion.SwimMovement(Vector2.Zero, Vector2.UnitX, .5f, 1);
        Check(Math.Abs(swim.X - .0225f) < .0001f && swim.Y == 0,
            "Swimming acceleration must retain ObjLink2d's bounded 0.0225 correction.");
    }

    private static void CheckGravityAndNoVerticalWalk(LiveWallpaperMap map)
    {
        var grounded = LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(32, 176));
        var groundY = grounded.Position.Y;
        for (var frame = 0; frame < 20; frame++)
            Check(LiveWallpaperSideViewPhysics.Step(map, ref grounded,
                    new SideViewInput(-Vector2.UnitY)),
                "Grounded side-view movement must remain in the map.");
        Check(Math.Abs(grounded.Position.Y - groundY) < .001f,
            "Vertical input on ordinary ground must not become arbitrary top-down walking.");

        var falling = LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(200, 48));
        var startY = falling.Position.Y;
        for (var frame = 0; frame < 100; frame++)
            Check(LiveWallpaperSideViewPhysics.Step(map, ref falling, default),
                "Gravity test body must remain in its enclosed synthetic map.");
        Check(falling.Position.Y > startY + 100 && falling.Grounded &&
              Math.Abs(falling.Position.Y - 176) < .001f,
            "Side-view Link must fall under gravity and land on the actual floor.");
    }

    private static void CheckLaddersAndOneWayTop(LiveWallpaperMap map)
    {
        var climber = LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(88, 156));
        for (var frame = 0; frame < 24; frame++)
            Check(LiveWallpaperSideViewPhysics.Step(map, ref climber,
                    new SideViewInput(-Vector2.UnitY)),
                "Ladder ascent must remain in bounds.");
        Check(climber.Position.Y < 140 && climber.Climbing,
            "Ladder input must climb rather than fall or top-down walk.");
        for (var frame = 0; frame < 90; frame++)
            Check(LiveWallpaperSideViewPhysics.Step(map, ref climber,
                    new SideViewInput(Vector2.UnitY)),
                "Ladder descent must remain in bounds.");
        Check(climber.Grounded && !climber.Climbing &&
              Math.Abs(climber.Position.Y - 176) < .001f,
            "Descending a ladder onto floor collision must stop grounded rather than continue through it.");

        var walkoff = new SideViewBody
        {
            Position = new Vector2(108, 120),
            Movement = new Vector2(0, -SideViewGameplayMotion.ClimbSpeed),
            Climbing = true,
            JumpAge = 12,
            Direction = 1
        };
        Check(LiveWallpaperSideViewPhysics.Step(map, ref walkoff, default),
            "Walking off a ladder must remain within the synthetic map.");
        Check(!walkoff.Climbing && walkoff.Movement.Y == 0,
            "Leaving a ladder must discard vertical climb carry before normal gravity resumes.");

        Check(!map.SideViewCollision(new Vector2(88, 90), 1, true, out _) &&
              map.SideViewCollision(new Vector2(88, 90), 3, true, out _),
            "A ladder top must be passable upward and support downward movement exactly like ObjLadderTop.");

        var leavingTop = new SideViewBody
        {
            Position = new Vector2(88, 89.6f),
            JumpAge = 12,
            Direction = 2
        };
        Check(LiveWallpaperSideViewPhysics.Step(map, ref leavingTop,
                new SideViewInput(Vector2.UnitX)),
            "Leaving the lower edge of a ladder top must remain in bounds.");
        Check(!leavingTop.Grounded,
            "A collision already overlapping Link's old ladder-top body must not create a false grounded state.");
    }

    private static void CheckJumpArcAndCeiling(LiveWallpaperMap map)
    {
        var jumper = LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(40, 176));
        var minimumY = jumper.Position.Y;
        for (var frame = 0; frame < 120; frame++)
        {
            var held = frame < 10;
            Check(LiveWallpaperSideViewPhysics.Step(map, ref jumper,
                    new SideViewInput(Vector2.Zero, held)),
                "Jump fixture must remain in bounds.");
            minimumY = Math.Min(minimumY, jumper.Position.Y);
        }
        Check(minimumY < 176 && minimumY >= 150 && jumper.Grounded &&
              Math.Abs(jumper.Position.Y - 176) < .001f,
            "A Feather jump must arc upward and land on the floor.");

        var ceiling = new SideViewBody
        {
            Position = new Vector2(40, 124),
            FallVelocity = -1.95f,
            JumpAge = 12,
            Direction = 1
        };
        var ceilingVelocityCleared = false;
        for (var frame = 0; frame < 4; frame++)
        {
            Check(LiveWallpaperSideViewPhysics.Step(map, ref ceiling, default),
                "The direct ceiling-collision fixture must remain in bounds.");
            ceilingVelocityCleared |= ceiling.Position.Y >= 122 && ceiling.FallVelocity == 0;
        }
        Check(ceilingVelocityCleared,
            "A ceiling collision must clear upward FallVelocity after the native gravity step.");
    }

    private static void CheckPortalTriggerAndArrivalLatch(LiveWallpaperMap map)
    {
        var source = map.Portals.Single(portal => portal.EntryId == "source");
        var destination = map.Portals.Single(portal => portal.EntryId == "destination");
        Check(!source.ShouldActivateAt(40, 176, 0, 2, true, true) &&
              source.ShouldActivateAt(40, 176, -1, 1, true, true) &&
              !source.ShouldActivateAt(40, 176, -1, 1, true, false),
            "door2d must require the real narrow collider, upward intent, and grounded Link.");
        Check(destination.ShouldActivateAt(149, 176, -1, 1, true, true) &&
              Math.Abs(149 - destination.LinkTargetX) > 2,
            "A side-view exit must activate on body overlap rather than only at its centre target.");

        var simulation = new LiveWallpaperSideViewSimulation(map, new Vector2(40, 176), "source");
        Check(!simulation.CanActivate(source) && simulation.CanActivate(destination),
            "Arrival latch must suppress only the portal Link spawned within.");
        var planner = new LiveWallpaperSideViewPlanner(
            map, LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(40, 176)), "source", true);
        AdvanceToComplete(planner, "The side-view arrival-latch route must finish bounded planning.");
        Check(planner.ReachedGoal && planner.Route.Count > 0,
            "A latched arrival must route to another valid side-view exit before returning.");
    }

    private static void CheckPlannerBoundedTargets(LiveWallpaperMap map)
    {
        var start = LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(40, 176));
        var reachable = new LiveWallpaperSideViewPlanner(map, start, null, false,
            new Vector2(136, 176));
        AdvanceToComplete(reachable, "Reachable manual side-view target must finish bounded planning.");
        Check(reachable.ReachedGoal && reachable.Route.Count > 0 && reachable.ExpandedNodes <= 8000,
            "A reachable side-view tap must produce a finite physics route.");

        var unreachable = new LiveWallpaperSideViewPlanner(map, start, null, false,
            new Vector2(-64, -64));
        AdvanceToComplete(unreachable, "Unreachable manual side-view target must finish bounded planning.");
        Check(!unreachable.ReachedGoal && unreachable.ExpandedNodes <= 8000,
            "An unreachable side-view tap must remain bounded and never invent a top-down path.");
    }

    private static void CheckRuntimeCadence(LiveWallpaperMap map)
    {
        var plan = new LiveWallpaperSideViewPlanner(map,
            LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(40, 176)), null, false,
            new Vector2(136, 176));
        AdvanceToComplete(plan, "Cadence fixture must first produce a bounded manual route.");
        Check(plan.ReachedGoal, "Cadence fixture requires a reachable manual route.");

        Vector2 RunAt(int hertz)
        {
            var body = LiveWallpaperSideViewPhysics.Spawn(map, new Vector2(40, 176));
            var remainder = 0d;
            var routeIndex = 0;
            for (var frame = 0; frame <= hertz; frame++)
            {
                remainder += 1000d / hertz;
                var ticks = Math.Min(6, (int)((remainder + .0001d) / (1000d / 60)));
                remainder -= ticks * (1000d / 60);
                for (var tick = 0; tick < ticks; tick++)
                {
                    var input = routeIndex < plan.Route.Count ? plan.Route[routeIndex++] : default;
                    Check(LiveWallpaperSideViewPhysics.Step(map, ref body, input),
                        "Cadence replay must remain inside the synthetic map.");
                }
            }
            return body.Position;
        }

        var at15 = RunAt(15);
        var at30 = RunAt(30);
        var at60 = RunAt(60);
        Check(Vector2.Distance(new Vector2(40, 176), at30) > 1 &&
              Vector2.Distance(at15, at30) <= 2 && Vector2.Distance(at30, at60) <= 2,
            "The first second of side-view replay must move and retain the same 60Hz physics pace at 15/30/60Hz render cadence.");
    }

    private static void CheckNoTopDownFallback(LiveWallpaperMap map)
    {
        Check(LiveWallpaperMapViewport.TryCreateCentered(1600, 900, map.Width, map.Height,
                40, 48, .5f, out var viewport),
            "Side-view integration viewport must load.");
        var simulation = new LiveWallpaperLinkSimulation();
        simulation.EnterMap(40, 48, "source");
        simulation.UpdateJourney(1, 0, 0, true, map, viewport, false);
        var state = simulation.UpdateJourney(1, 0, 17, true, map, viewport, false);
        Check(state.Action == LiveWallpaperLinkRouteAction.SideViewFall,
            "An airborne 2D map must enter side-view gravity, never the top-down journey fallback.");
    }

    private static void CheckInstalledDungeonRoutes()
    {
        var dataRoot = Environment.GetEnvironmentVariable("LADXHD_TEST_GAME_DATA");
        if (string.IsNullOrWhiteSpace(dataRoot) || !Directory.Exists(dataRoot))
            return;

        var dungeonThree = LoadInstalledMap(dataRoot, "dungeon3_2d.map");
        Check(dungeonThree.Is2DMap && dungeonThree.Portals.Count(portal => portal.HasDestination) == 2,
            "Installed Dungeon 3 fixture must retain both side-view exits.");
        var failures = new List<string>();
        foreach (var entry in dungeonThree.Portals.Where(portal => portal.HasDestination))
        {
            var failure = CheckInstalledRoute(dungeonThree, entry);
            if (failure != null) failures.Add(failure);
        }
        CheckInstalledReverseRuntime(dungeonThree);
        CheckInstalledManualTargetRuntime(dungeonThree);

        var dungeonSeven = LoadInstalledMap(dataRoot, "dungeon7_2d.map");
        var specialDoor = dungeonSeven.Portals.Single(portal => portal.Is2DDoor);
        Check(specialDoor.ShouldActivateAt(specialDoor.LinkTargetX, specialDoor.LinkTargetY,
                -1, 1, true, true) &&
              !specialDoor.ShouldActivateAt(specialDoor.LinkTargetX, specialDoor.LinkTargetY,
                  0, 2, true, true),
            "Installed Dungeon 7 door2d must preserve its grounded upward-input trigger.");
        var dungeonSevenFailure = CheckInstalledRoute(dungeonSeven, specialDoor);
        if (dungeonSevenFailure != null) failures.Add(dungeonSevenFailure);
        Check(failures.Count == 0, string.Join(" | ", failures));
    }

    private static void CheckInstalledReverseRuntime(LiveWallpaperMap map)
    {
        var entry = map.Portals.Single(portal => portal.EntryId == "d3_2d_2");
        var goal = map.Portals.Single(portal => portal.EntryId == "d3_2d_1");
        var simulation = new LiveWallpaperSideViewSimulation(map,
            new Vector2(entry.GetLinkSpawnX(true), entry.GetLinkSpawnY(true)), entry.EntryId);
        var reachedGoal = false;
        for (var frame = 0; frame <= 30 * 120; frame++)
        {
            var state = simulation.Update((long)Math.Round(frame * 1000d / 30), true);
            var body = simulation.Body;
            if (goal.ShouldActivateAt(body.Position.X, body.Position.Y, state.Input.Move.Y,
                    state.Input.Move.Y < 0 ? 1 : body.Direction, true, body.Grounded))
            {
                reachedGoal = true;
                break;
            }
        }
        Check(reachedGoal && simulation.CanActivate(goal),
            "The 30Hz side-view runtime must complete the bounded D3 reverse replans and activate d3_2d_1 within 120 seconds.");
    }

    private static void CheckInstalledManualTargetRuntime(LiveWallpaperMap map)
    {
        var entry = map.Portals.Single(portal => portal.EntryId == "d3_2d_2");
        var target = new Vector2(40, 48);
        var start = LiveWallpaperSideViewPhysics.Spawn(map,
            new Vector2(entry.GetLinkSpawnX(true), entry.GetLinkSpawnY(true)));
        var firstSearch = new LiveWallpaperSideViewPlanner(map, start, entry.EntryId, true, target);
        AdvanceToComplete(firstSearch,
            "The installed D3 manual-target first search must remain bounded.");
        Check(!firstSearch.ReachedGoal && firstSearch.Route.Count > 0,
            "The D3 manual-target fixture must require a bounded intermediate route before the upper ladder target.");

        var simulation = new LiveWallpaperSideViewSimulation(map, start.Position, entry.EntryId);
        simulation.WalkTo(target);
        var reachedTarget = false;
        for (var frame = 0; frame <= 30 * 120; frame++)
        {
            simulation.Update((long)Math.Round(frame * 1000d / 30), true);
            var body = simulation.Body;
            if (Vector2.DistanceSquared(body.Position, target) <= 9 &&
                (body.Grounded || body.Climbing || body.Swimming))
            {
                reachedTarget = true;
                break;
            }
        }
        Check(reachedTarget,
            "The 30Hz side-view runtime must retain a multi-chunk manual target until Link reaches the upper ladder.");
    }

    private static void CheckCurrentTapRecovery(LiveWallpaperMap map)
    {
        var source = map.Portals.Single(portal => portal.EntryId == "source");
        var destination = map.Portals.Single(portal => portal.EntryId == "destination");
        var spawn = new Vector2(40, 176);
        var simulation = new LiveWallpaperSideViewSimulation(map, spawn, source.EntryId);
        simulation.WalkTo(spawn);
        for (var frame = 0; frame <= 30; frame++)
            simulation.Update((long)Math.Round(frame * 1000d / 30), true);
        Check(Vector2.DistanceSquared(simulation.Body.Position, spawn) < .001f,
            "An exact current-position side-view tap must not invent movement.");

        var resumedAutonomousRoute = false;
        for (var frame = 31; frame <= 30 * 90; frame++)
        {
            var state = simulation.Update((long)Math.Round(frame * 1000d / 30), true);
            var body = simulation.Body;
            if (destination.ShouldActivateAt(body.Position.X, body.Position.Y, state.Input.Move.Y,
                    state.Input.Move.Y < 0 ? 1 : body.Direction, true, body.Grounded))
            {
                resumedAutonomousRoute = true;
                break;
            }
        }
        Check(resumedAutonomousRoute,
            "A completed current-position tap must clear the manual target and allow autonomous side-view routing to resume.");
    }

    private static void CheckUnreachableTapRecovery(LiveWallpaperMap map)
    {
        var source = map.Portals.Single(portal => portal.EntryId == "source");
        var destination = map.Portals.Single(portal => portal.EntryId == "destination");
        var simulation = new LiveWallpaperSideViewSimulation(map, new Vector2(40, 176), source.EntryId);
        simulation.WalkTo(new Vector2(-64, -64));
        var resumedAutonomousRoute = false;
        for (var frame = 0; frame <= 30 * 120; frame++)
        {
            var state = simulation.Update((long)Math.Round(frame * 1000d / 30), true);
            var body = simulation.Body;
            Check(map.SideViewPositionInBounds(body.Position),
                "An unreachable side-view tap must not teleport Link outside the physical map.");
            if (destination.ShouldActivateAt(body.Position.X, body.Position.Y, state.Input.Move.Y,
                    state.Input.Move.Y < 0 ? 1 : body.Direction, true, body.Grounded))
            {
                resumedAutonomousRoute = true;
                break;
            }
        }
        Check(resumedAutonomousRoute,
            "An unreachable side-view tap must clear its failed manual route and eventually resume autonomous navigation.");
    }

    private static string CheckInstalledRoute(LiveWallpaperMap map, LiveWallpaperMapPortal entry)
    {
        var body = LiveWallpaperSideViewPhysics.Spawn(map,
            new Vector2(entry.GetLinkSpawnX(true), entry.GetLinkSpawnY(true)));
        var goal = map.Portals.FirstOrDefault(portal =>
            portal.HasDestination && portal.EntryId != entry.EntryId);
        if (!goal.HasDestination) goal = entry;
        var entryLatched = true;
        var totalExpansions = 0;
        for (var chunk = 1; chunk <= 8; chunk++)
        {
            var planner = new LiveWallpaperSideViewPlanner(map, body, entry.EntryId, entryLatched);
            AdvanceToComplete(planner, $"Installed {entry.EntryId} side-view planner chunk {chunk} must finish bounded search.");
            totalExpansions += planner.ExpandedNodes;
            if (planner.Route.Count == 0)
                return $"Installed {entry.EntryId} chunk {chunk} made no physical progress (expanded {planner.ExpandedNodes}).";
            foreach (var input in planner.Route)
            {
                var previous = body;
                if (!LiveWallpaperSideViewPhysics.Step(map, ref body, input))
                    return $"Installed {entry.EntryId} replay left bounds in chunk {chunk}.";
                if (map.SideViewMovementCollision(ref previous, body.Position, -1, false, out _))
                    return $"Installed {entry.EntryId} replay entered a new normal solid in chunk {chunk}.";
                if (entryLatched && !entry.TouchesSideViewTrigger(body.Position.X, body.Position.Y)) entryLatched = false;
                foreach (var portal in map.Portals.Where(portal => portal.HasDestination))
                {
                    if (entryLatched && portal.EntryId == entry.EntryId) continue;
                    if (!portal.ShouldActivateAt(body.Position.X, body.Position.Y, input.Move.Y,
                            input.Move.Y < 0 ? 1 : body.Direction, true, body.Grounded)) continue;
                    if (portal.EntryId != goal.EntryId)
                        return $"Installed {entry.EntryId} reached non-goal exit {portal.EntryId} in chunk {chunk}.";
                    Console.WriteLine($"{entry.EntryId}->{goal.EntryId}: chunks={chunk}, expansions={totalExpansions}, frames={planner.Route.Count} final.");
                    return null;
                }
            }
        }
        return $"Installed {entry.EntryId} did not reach {goal.EntryId} within 8 bounded planner chunks (expanded {totalExpansions}).";
    }

    private static void AdvanceToComplete(LiveWallpaperSideViewPlanner planner, string message)
    {
        for (var iteration = 0; iteration < 400 && !planner.Complete; iteration++)
            planner.Advance(32);
        Check(planner.Complete && planner.ExpandedNodes <= 8000, message);
    }

    private static LiveWallpaperMap LoadSyntheticMap(bool includeCeiling)
    {
        const int width = 20, height = 12;
        var text = new StringBuilder($"3\n0\n0\nsideview.png\n{width}\n{height}\n1\n");
        for (var row = 0; row < height; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", width)));
        text.AppendLine("7");
        text.AppendLine("link2dspawner");
        text.AppendLine("c1");
        text.AppendLine("dungeonLadder");
        text.AppendLine("dungeonLadderTop");
        text.AppendLine("oneWayFlatTop");
        text.AppendLine("door2d");
        text.AppendLine("door");
        var objects = new List<string> { "0;0;0" };
        for (var x = 0; x < width * 16; x += 16) objects.Add($"1;{x};176");
        if (includeCeiling)
            for (var x = 16; x <= 64; x += 16) objects.Add($"1;{x};96");
        foreach (var y in new[] { 96, 112, 128, 144 }) objects.Add($"2;80;{y}");
        objects.Add("3;80;80");
        objects.Add("4;80;80");
        objects.Add("5;32;160;16;16;source;synthetic.map;source_exit");
        objects.Add("5;144;160;16;16;destination;synthetic.map;destination_exit");
        text.AppendLine(objects.Count.ToString());
        foreach (var line in objects) text.AppendLine(line);
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map) && map.Is2DMap,
            "Synthetic side-view fixture must parse as an Is2DMap.");
        return map;
    }

    private static LiveWallpaperMap LoadInstalledMap(string dataRoot, string fileName)
    {
        using var reader = File.OpenText(Path.Combine(dataRoot, "Maps", fileName));
        Check(LiveWallpaperMap.TryLoad(reader, out var map),
            $"Installed {fileName} fixture must parse.");
        return map;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
