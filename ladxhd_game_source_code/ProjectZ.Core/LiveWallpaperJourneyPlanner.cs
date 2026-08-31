using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public enum LiveWallpaperJourneyAction
    {
        Walk,
        Interact,
        OpenChest,
        Attack,
        FeatherJump,
        PegasusJump,
        Swim,
        RoosterFly,
        CutBush,
        LiftStone,
        PushBlock,
        PegasusCharge,
        PegasusDash,
        Hookshot,
        Exit
    }

    public readonly struct LiveWallpaperJourneyPoint
    {
        public LiveWallpaperJourneyPoint(float pixelX, float pixelY,
            LiveWallpaperJourneyAction action = LiveWallpaperJourneyAction.Walk,
            int bushKey = -1,
            int stoneKey = -1,
            float hookshotTargetX = 0f,
            float hookshotTargetY = 0f,
            int chestKey = -1,
            string chestItemName = null,
            int moveStoneKey = -1)
        {
            PixelX = pixelX;
            PixelY = pixelY;
            Action = action;
            BushKey = bushKey;
            StoneKey = stoneKey;
            HookshotTargetX = hookshotTargetX;
            HookshotTargetY = hookshotTargetY;
            ChestKey = chestKey;
            ChestItemName = chestItemName;
            MoveStoneKey = moveStoneKey;
        }

        public float PixelX { get; }
        public float PixelY { get; }
        public LiveWallpaperJourneyAction Action { get; }
        public int BushKey { get; }
        public int StoneKey { get; }
        public float HookshotTargetX { get; }
        public float HookshotTargetY { get; }
        public int ChestKey { get; }
        public string ChestItemName { get; }
        public int MoveStoneKey { get; }
    }

    public sealed class LiveWallpaperJourneyPlan
    {
        public LiveWallpaperJourneyPlan(
            LiveWallpaperJourneyPoint[] points,
            int interactionPointIndex = -1,
            int interactionActorIndex = -1,
            int roosterPickupPointIndex = -1,
            int roosterLandingPointIndex = -1,
            int combatPointIndex = -1,
            int combatEnemyIndex = -1)
        {
            Points = points ?? [];
            InteractionPointIndex = interactionPointIndex;
            InteractionActorIndex = interactionActorIndex;
            RoosterPickupPointIndex = roosterPickupPointIndex;
            RoosterLandingPointIndex = roosterLandingPointIndex;
            CombatPointIndex = combatPointIndex;
            CombatEnemyIndex = combatEnemyIndex;
        }

        public IReadOnlyList<LiveWallpaperJourneyPoint> Points { get; }
        public int InteractionPointIndex { get; }
        public int InteractionActorIndex { get; }
        public int RoosterPickupPointIndex { get; }
        public int RoosterLandingPointIndex { get; }
        public int CombatPointIndex { get; }
        public int CombatEnemyIndex { get; }
        public bool HasInteraction => InteractionPointIndex >= 0 && InteractionActorIndex >= 0;
        public bool HasRoosterFlight => RoosterPickupPointIndex >= 0 && RoosterLandingPointIndex >= 0;
        public bool HasCombat => CombatPointIndex >= 0 && CombatEnemyIndex >= 0;
    }

    /// <summary>
    /// Builds deterministic wallpaper journeys from installed overworld collision, doors, screen
    /// edges, and NPC bodies. It deliberately has no save, dialog, audio, or gameplay side effects.
    /// </summary>
    public static class LiveWallpaperJourneyPlanner
    {
        internal const int GridStep = 8;
        private const int VisibleRouteMargin = 32;
        private const float LinkBodyOffsetX = -4f;
        private const float LinkBodyOffsetY = -10f;
        private const float LinkBodyWidth = 8f;
        private const float LinkBodyHeight = 10f;
        private static readonly Point[] FieldDirections =
        [
            new(-1, 0),
            new(0, -1),
            new(1, 0),
            new(0, 1)
        ];

        private readonly struct Endpoint
        {
            public Endpoint(
                int x, int y, int side, bool isDoor,
                int exitX = 0, int exitY = 0, bool hasExit = false)
            {
                X = x;
                Y = y;
                Side = side;
                IsDoor = isDoor;
                ExitX = exitX;
                ExitY = exitY;
                HasExit = hasExit;
            }

            public int X { get; }
            public int Y { get; }
            public int Side { get; }
            public bool IsDoor { get; }
            public int ExitX { get; }
            public int ExitY { get; }
            public bool HasExit { get; }
        }

        private readonly struct Pair
        {
            public Pair(Endpoint start, Endpoint end, float score)
            {
                Start = start;
                End = end;
                Score = score;
            }

            public Endpoint Start { get; }
            public Endpoint End { get; }
            public float Score { get; }
        }

        public static LiveWallpaperJourneyPlan Create(
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            int scene,
            int variant,
            bool allowIslandLife,
            float? continuationPixelX = null,
            float? continuationPixelY = null,
            bool edgeStartOnly = false,
            bool followLoadingZones = false,
            IReadOnlySet<int> visitedFieldKeys = null,
            string excludedPortalEntryId = null,
            IReadOnlySet<int> openedChests = null)
        {
            if (map == null || map.Is2DMap || viewport.Columns <= 0 || viewport.Rows <= 0)
                return new LiveWallpaperJourneyPlan([]);

            int minX;
            int minY;
            int maxX;
            int maxY;
            if (followLoadingZones)
                GetOverworldFieldBounds(
                    map, scene, continuationPixelX, continuationPixelY,
                    out minX, out minY, out maxX, out maxY);
            else
                GetBounds(map, viewport,
                    out minX, out minY, out maxX, out maxY);
            // Endpoints are on the crop edge, but all visible travel stays inside it. The A*
            // edge penalty below immediately pulls routes toward the interior.
            var pathMinX = minX;
            var pathMinY = minY;
            var pathMaxX = maxX;
            var pathMaxY = maxY;
            if (!followLoadingZones)
                ExpandPathBoundsForNearbyPortals(
                    map, minX, minY, maxX, maxY,
                    ref pathMinX, ref pathMinY, ref pathMaxX, ref pathMaxY);
            var endpoints = BuildEndpoints(
                map, minX, minY, maxX, maxY,
                pathMinX, pathMinY, pathMaxX, pathMaxY,
                excludedPortalEntryId);
            var pairs = continuationPixelX.HasValue && continuationPixelY.HasValue
                ? BuildContinuationPairs(
                    map, endpoints,
                    continuationPixelX.Value, continuationPixelY.Value,
                    pathMinX, pathMinY, pathMaxX, pathMaxY,
                    followLoadingZones && variant % 6 != 5)
                : BuildPairs(
                    endpoints, minX, minY, maxX, maxY, edgeStartOnly,
                    followLoadingZones && variant % 6 != 5);
            if (pairs.Count == 0)
                return continuationPixelX.HasValue && continuationPixelY.HasValue
                    ? CreateReachableFallback(
                        map, continuationPixelX.Value, continuationPixelY.Value,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, variant)
                    : new LiveWallpaperJourneyPlan([]);

            var orderedPreferredExits = false;
            if (followLoadingZones && variant % 6 == 5)
            {
                var doorPairs = pairs.FindAll(pair => pair.End.IsDoor);
                if (doorPairs.Count > 0)
                {
                    var doorOffset = PositiveHash(scene, variant, 59) % doorPairs.Count;
                    var orderedPairs = new List<Pair>(pairs.Count);
                    for (var index = 0; index < doorPairs.Count; index++)
                        orderedPairs.Add(doorPairs[(doorOffset + index) % doorPairs.Count]);
                    foreach (var pair in pairs)
                    {
                        if (!doorPairs.Contains(pair))
                            orderedPairs.Add(pair);
                    }
                    pairs = orderedPairs;
                    orderedPreferredExits = true;
                }
            }
            if (!orderedPreferredExits && followLoadingZones &&
                visitedFieldKeys?.Count > 0)
            {
                var currentPixelX = continuationPixelX ??
                                    (minX + maxX) * 0.5f;
                var currentPixelY = continuationPixelY ??
                                    (minY + maxY) * 0.5f;
                var hasCoverageStep = TryGetNextCoverageFieldKey(
                    map, currentPixelX, currentPixelY,
                    visitedFieldKeys, out var coverageFieldKey);
                var newAreaPairs = pairs.FindAll(pair =>
                    pair.End.HasExit &&
                    HasTraversableLoadingZoneExit(
                        map, pair.End, includeHoles: true) &&
                    (hasCoverageStep
                        ? GetOverworldFieldKey(
                            pair.End.ExitX, pair.End.ExitY) == coverageFieldKey
                        : !visitedFieldKeys.Contains(GetOverworldFieldKey(
                            pair.End.ExitX, pair.End.ExitY))));
                if (newAreaPairs.Count > 0)
                {
                    // Try every new-field exit before a visited fallback. Merely
                    // prepending them was insufficient because the later hash
                    // offset could begin inside the fallback portion.
                    var preferredOffset = PositiveHash(
                        scene, variant, 53) % newAreaPairs.Count;
                    var orderedPairs = new List<Pair>(pairs.Count);
                    for (var index = 0; index < newAreaPairs.Count; index++)
                        orderedPairs.Add(newAreaPairs[
                            (preferredOffset + index) % newAreaPairs.Count]);
                    foreach (var pair in pairs)
                    {
                        if (!newAreaPairs.Contains(pair))
                            orderedPairs.Add(pair);
                    }
                    pairs = orderedPairs;
                    orderedPreferredExits = true;
                }
            }

            var pairOffset = orderedPreferredExits
                ? 0
                : PositiveHash(scene, variant, 17) % pairs.Count;
            List<Point> fallbackPath = null;
            for (var attempt = 0; attempt < pairs.Count; attempt++)
            {
                var pair = pairs[(pairOffset + attempt) % pairs.Count];
                // A loading-zone journey is only complete when Link can actually
                // stand in the neighbouring overworld field. Previously every edge
                // was marked Exit even when the tile across it was blocked, which
                // hid Link and replanned inside the same field forever.
                if (followLoadingZones && pair.End.HasExit &&
                    !HasTraversableLoadingZoneExit(map, pair.End, includeHoles: true))
                    continue;
                // A field at the edge of the installed overworld can be a real
                // dead end: its only traversable loading zone is the point Link
                // just entered. Keep that endpoint as an immediate return path
                // instead of producing an empty plan and falling back forever.
                if (followLoadingZones && pair.End.HasExit &&
                    pair.Start.X == pair.End.X &&
                    pair.Start.Y == pair.End.Y)
                {
                    var returnPath = new List<Point>
                    {
                        new(pair.Start.X, pair.Start.Y),
                        new(pair.End.ExitX, pair.End.ExitY)
                    };
                    return MarkLoadingZoneExit(ToPlan(map, returnPath));
                }
                var basePath = FindPath(
                    map, pair.Start.X, pair.Start.Y, pair.End.X, pair.End.Y,
                    pathMinX, pathMinY, pathMaxX, pathMaxY,
                    includeHoles: true, includeBushes: false,
                    includeStones: false, includeMoveStones: false);
                if (basePath.Count < 2)
                {
                    var jumpPath = FindPath(
                        map, pair.Start.X, pair.Start.Y, pair.End.X, pair.End.Y,
                        pathMinX, pathMinY, pathMaxX, pathMaxY,
                        includeHoles: false, includeBushes: false,
                        includeStones: false, includeMoveStones: false);
                    if (jumpPath.Count >= 2 && HasValidJumpSpans(map, jumpPath))
                    {
                        var jumpCrossedLoadingZone = TryAppendLoadingZoneExit(
                            map, jumpPath, pair.End, includeHoles: false);
                        return jumpCrossedLoadingZone
                            ? MarkLoadingZoneExit(ToJumpPlan(map, jumpPath))
                            : ToJumpPlan(map, jumpPath);
                    }
                    if (TryCreateHookshotPlan(
                            map, pair.Start, pair.End,
                            pathMinX, pathMinY, pathMaxX, pathMaxY,
                            PositiveHash(scene, variant, 97),
                            out var hookshotPlan))
                        return hookshotPlan;
                    continue;
                }

                var behavior = PositiveHash(scene, variant, 71);
                if (!followLoadingZones && behavior % 2 == 0 && TryAddScenicDetour(
                        map, basePath, pair.Start, pair.End,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, behavior,
                        out var scenicPath))
                    basePath = scenicPath;
                var crossedLoadingZone = TryAppendLoadingZoneExit(
                    map, basePath, pair.End, includeHoles: true);
                fallbackPath ??= basePath;
                if (TryCreateTraversableObjectPlan(
                        map, basePath, out var objectPlan))
                {
                    objectPlan = AddPegasusDash(objectPlan);
                    return crossedLoadingZone
                        ? MarkLoadingZoneExit(objectPlan)
                        : objectPlan;
                }
                if (!followLoadingZones && allowIslandLife &&
                    behavior % 5 != 0 && TryAddCombat(
                        map, basePath, minX, minY, maxX, maxY,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, behavior,
                        out var combatPlan))
                    return crossedLoadingZone
                        ? MarkLoadingZoneExit(combatPlan)
                        : combatPlan;
                if (!followLoadingZones && allowIslandLife && behavior % 7 == 0)
                {
                    var roosterPlan = AddRoosterFlight(
                        map, basePath, pathMinX, pathMinY, pathMaxX, pathMaxY);
                    return crossedLoadingZone
                        ? MarkLoadingZoneExit(roosterPlan)
                        : roosterPlan;
                }
                if (!followLoadingZones && allowIslandLife && behavior % 5 == 0 &&
                    TryAddChest(
                        map, basePath, minX, minY, maxX, maxY,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, behavior,
                        openedChests, out var chestPlan))
                    return crossedLoadingZone
                        ? MarkLoadingZoneExit(chestPlan)
                        : chestPlan;
                if (!followLoadingZones && allowIslandLife && behavior % 5 == 0 &&
                    TryAddInteraction(
                        map, basePath, pair.Start, pair.End,
                        minX, minY, maxX, maxY,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, behavior,
                        out var interactionPlan))
                    return crossedLoadingZone
                        ? MarkLoadingZoneExit(interactionPlan)
                        : interactionPlan;
                var plan = AddPegasusDash(ToPlan(map, basePath));
                return crossedLoadingZone ? MarkLoadingZoneExit(plan) : plan;
            }
            return fallbackPath != null
                ? AddPegasusDash(ToPlan(map, fallbackPath))
                : continuationPixelX.HasValue && continuationPixelY.HasValue
                    ? CreateReachableFallback(
                        map, continuationPixelX.Value, continuationPixelY.Value,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, variant)
                    : new LiveWallpaperJourneyPlan([]);
        }

        private static LiveWallpaperJourneyPlan CreateReachableFallback(
            LiveWallpaperMap map,
            float currentPixelX,
            float currentPixelY,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int variant)
        {
            var startX = Snap(Math.Clamp(
                (int)MathF.Round(currentPixelX), minX, maxX));
            var startY = Snap(Math.Clamp(
                (int)MathF.Round(currentPixelY), minY, maxY));
            var candidates = new List<Point>();
            for (var y = minY; y <= maxY; y += GridStep * 2)
            for (var x = minX; x <= maxX; x += GridStep * 2)
            {
                if ((x == startX && y == startY) ||
                    !IsWalkable(
                        map, x, y, includeHoles: true,
                        includeBushes: false, includeStones: false,
                        includeMoveStones: false))
                    continue;
                candidates.Add(new Point(x, y));
            }
            candidates.Sort((left, right) =>
            {
                var leftDistance =
                    (left.X - startX) * (left.X - startX) +
                    (left.Y - startY) * (left.Y - startY);
                var rightDistance =
                    (right.X - startX) * (right.X - startX) +
                    (right.Y - startY) * (right.Y - startY);
                return rightDistance.CompareTo(leftDistance);
            });
            if (candidates.Count == 0)
                return new LiveWallpaperJourneyPlan([]);
            var offset = PositiveHash(startX, variant, 113) % candidates.Count;
            var reachable = new HashSet<Point>();
            var filteredToReachable = false;
            for (var attempt = 0;
                 attempt < Math.Min(candidates.Count, 48);
                 attempt++)
            {
                var candidate = candidates[(offset + attempt) % candidates.Count];
                var path = FindPath(
                    map, startX, startY, candidate.X, candidate.Y,
                    minX, minY, maxX, maxY,
                    includeHoles: true, includeBushes: false,
                    includeStones: false, includeMoveStones: false,
                    allowDiagonal: true,
                    penalizeVisibleEdges: true,
                    reachableWhenNoPath: reachable);
                if (path.Count < 2)
                {
                    // A failed A* has already exhausted Link's connected
                    // component. Reuse it instead of trying dozens of targets
                    // in other rooms and potentially never trying his own.
                    if (!filteredToReachable && reachable.Count > 0)
                    {
                        candidates.RemoveAll(point => !reachable.Contains(point));
                        if (candidates.Count == 0) break;
                        offset = PositiveHash(startX, variant, 113) % candidates.Count;
                        attempt = -1;
                        filteredToReachable = true;
                    }
                    continue;
                }
                return AddPegasusDash(
                    TryCreateTraversableObjectPlan(
                        map, path, out var objectPlan)
                        ? objectPlan
                        : ToPlan(map, path));
            }
            return new LiveWallpaperJourneyPlan([]);
        }

        public static LiveWallpaperJourneyPlan CreateToPoint(
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            float startPixelX,
            float startPixelY,
            float targetPixelX,
            float targetPixelY)
        {
            if (map == null || map.Is2DMap || viewport.Columns <= 0 || viewport.Rows <= 0)
                return new LiveWallpaperJourneyPlan([]);
            GetBounds(map, viewport,
                out var minX, out var minY, out var maxX, out var maxY);
            ExpandPathBoundsForNearbyPortals(
                map, minX, minY, maxX, maxY,
                ref minX, ref minY, ref maxX, ref maxY);
            var startX = Snap(Math.Clamp((int)MathF.Round(startPixelX), minX, maxX));
            var startY = Snap(Math.Clamp((int)MathF.Round(startPixelY), minY, maxY));
            var targetX = Snap(Math.Clamp((int)MathF.Round(targetPixelX), minX, maxX));
            var targetY = Snap(Math.Clamp((int)MathF.Round(targetPixelY), minY, maxY));
            var tappedHole = map.IntersectsHole(
                targetPixelX + LinkBodyOffsetX,
                targetPixelY + LinkBodyOffsetY,
                LinkBodyWidth, LinkBodyHeight);

            // A deliberate tap on a hole walks into the canonical fall. For a
            // safe destination, the shortest route may cross a valid pit span,
            // but that span is converted to the normal feather/Pegasus jump.
            // An exhausted search has already visited every reachable position.
            // Reuse that result for this tap instead of searching the same
            // disconnected area again for each of the 289 nearby candidates.
            var reachableWithHoles = new HashSet<Point>();
            var reachableWithoutHoles = new HashSet<Point>();
            var attemptedCandidates = new HashSet<Point>();
            const int maximumTapRadius = GridStep * 8;
            for (var radius = 0; radius <= maximumTapRadius; radius += GridStep)
            {
                for (var offsetY = -radius; offsetY <= radius; offsetY += GridStep)
                for (var offsetX = -radius; offsetX <= radius; offsetX += GridStep)
                {
                    if (radius > 0 && Math.Abs(offsetX) != radius &&
                        Math.Abs(offsetY) != radius)
                        continue;
                    var candidateX = Snap(Math.Clamp(targetX + offsetX, minX, maxX));
                    var candidateY = Snap(Math.Clamp(targetY + offsetY, minY, maxY));
                    var candidate = new Point(candidateX, candidateY);
                    if (!attemptedCandidates.Add(candidate) ||
                        reachableWithHoles.Count > 0 && !reachableWithHoles.Contains(candidate))
                        continue;
                    if (!IsWalkable(
                            map, candidateX, candidateY,
                            includeHoles: !tappedHole, includeBushes: false,
                            includeStones: false, includeMoveStones: false))
                        continue;
                    var path = FindPath(
                        map, startX, startY, candidateX, candidateY,
                        minX, minY, maxX, maxY,
                        includeHoles: false, includeBushes: false,
                        includeStones: false, includeMoveStones: false,
                        allowDiagonal: true,
                        penalizeVisibleEdges: false,
                        reachableWhenNoPath: reachableWithHoles);
                    if (path.Count < 2)
                        continue;
                    if (tappedHole)
                        return TryCreateTraversableObjectPlan(
                            map, path, out var fallObjectPlan)
                            ? fallObjectPlan
                            : ToPlan(map, path);

                    if (path.Exists(point => IsHolePoint(map, point)))
                    {
                        if (HasValidJumpSpans(map, path))
                        {
                            var jumpPlan = TryCreateTraversableObjectPlan(
                                map, path, out var jumpObjectPlan)
                                ? ApplyJumpActions(map, path, jumpObjectPlan)
                                : ToJumpPlan(map, path);
                            return AddPegasusDash(jumpPlan);
                        }
                        if (reachableWithoutHoles.Count > 0 &&
                            !reachableWithoutHoles.Contains(candidate))
                            continue;
                        path = FindPath(
                            map, startX, startY, candidateX, candidateY,
                            minX, minY, maxX, maxY,
                            includeHoles: true, includeBushes: false,
                            includeStones: false, includeMoveStones: false,
                            allowDiagonal: true,
                            penalizeVisibleEdges: false,
                            reachableWhenNoPath: reachableWithoutHoles);
                        if (path.Count < 2)
                            continue;
                    }
                    return AddPegasusDash(
                        TryCreateTraversableObjectPlan(
                            map, path, out var objectPlan)
                            ? objectPlan
                            : ToPlan(map, path));
                }
            }
            return new LiveWallpaperJourneyPlan([]);
        }

        private static bool TryAddScenicDetour(
            LiveWallpaperMap map,
            List<Point> basePath,
            Endpoint start,
            Endpoint end,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int behavior,
            out List<Point> scenicPath)
        {
            scenicPath = null;
            var candidates = new List<Point>();
            const int candidateStep = GridStep * 3;
            for (var y = minY + candidateStep; y <= maxY - candidateStep; y += candidateStep)
            {
                for (var x = minX + candidateStep; x <= maxX - candidateStep; x += candidateStep)
                {
                    if (!IsWalkable(map, x, y, true) ||
                        DistanceToSegmentSquared(x, y, start.X, start.Y, end.X, end.Y) <
                        32f * 32f)
                        continue;
                    candidates.Add(new Point(x, y));
                }
            }
            if (candidates.Count == 0)
                return false;

            var candidateOffset = behavior % candidates.Count;
            for (var attempt = 0; attempt < Math.Min(24, candidates.Count); attempt++)
            {
                var waypoint = candidates[(candidateOffset + attempt * 7) % candidates.Count];
                var first = FindPath(
                    map, start.X, start.Y, waypoint.X, waypoint.Y,
                    minX, minY, maxX, maxY, includeHoles: true);
                if (first.Count < 2)
                    continue;
                var second = FindPath(
                    map, waypoint.X, waypoint.Y, end.X, end.Y,
                    minX, minY, maxX, maxY, includeHoles: true);
                if (second.Count < 2)
                    continue;
                var combinedCount = first.Count + second.Count - 1;
                if (combinedCount < basePath.Count + 5 ||
                    combinedCount > Math.Max(basePath.Count * 3, basePath.Count + 80))
                    continue;
                first.AddRange(second.GetRange(1, second.Count - 1));
                scenicPath = first;
                return true;
            }
            return false;
        }

        private static bool TryAddChest(
            LiveWallpaperMap map,
            List<Point> basePath,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int pathMinX,
            int pathMinY,
            int pathMaxX,
            int pathMaxY,
            int behavior,
            IReadOnlySet<int> openedChests,
            out LiveWallpaperJourneyPlan plan)
        {
            plan = null;
            var chests = new List<LiveWallpaperMapObject>();
            foreach (var mapObject in map.Objects)
            {
                if (!string.Equals(
                        mapObject.Template, "chest", StringComparison.Ordinal) ||
                    mapObject.Arguments.Count == 0 ||
                    !LiveWallpaperChestItem.TryResolve(
                        mapObject.Arguments[0], out _) ||
                    mapObject.Arguments.Count > 4 &&
                    bool.TryParse(mapObject.Arguments[4], out var hitMode) &&
                    hitMode)
                    continue;
                var chestKey = map.GetChestKey(
                    mapObject.PixelX, mapObject.PixelY);
                if (openedChests?.Contains(chestKey) == true)
                    continue;
                var centerX = mapObject.PixelX + 8;
                var centerY = mapObject.PixelY + 8;
                if (centerX >= minX && centerX <= maxX &&
                    centerY >= minY && centerY <= maxY)
                    chests.Add(mapObject);
            }
            if (chests.Count == 0)
                return false;

            var chestOffset = behavior % chests.Count;
            for (var chestAttempt = 0; chestAttempt < chests.Count; chestAttempt++)
            {
                var chest = chests[(chestOffset + chestAttempt) % chests.Count];
                // ObjChest.Interact only opens while Link faces up. With ObjLink's
                // (-4,-10,8,10) body, this is the nearest non-overlapping point
                // directly below the chest's (x,y+3,16,11) collision box.
                var approach = new Point(chest.PixelX + 8, chest.PixelY + 24);
                approach = new Point(
                    Snap(Math.Clamp(approach.X, minX, maxX)),
                    Snap(Math.Clamp(approach.Y, minY, maxY)));
                if (!IsWalkable(map, approach.X, approach.Y, true))
                    continue;
                var detourIndex = FindNearestPointIndex(basePath, approach);
                var detourStart = basePath[detourIndex];
                var detour = FindPath(
                    map, detourStart.X, detourStart.Y, approach.X, approach.Y,
                    pathMinX, pathMinY, pathMaxX, pathMaxY, includeHoles: true);
                if (detour.Count < 2)
                    continue;

                var points = new List<LiveWallpaperJourneyPoint>();
                for (var index = 0; index <= detourIndex; index++)
                    points.Add(ToJourneyPoint(map, basePath[index]));
                for (var index = 1; index < detour.Count; index++)
                    points.Add(ToJourneyPoint(map, detour[index]));
                var chestPoint = points.Count - 1;
                points[chestPoint] = new LiveWallpaperJourneyPoint(
                    approach.X, approach.Y,
                    LiveWallpaperJourneyAction.OpenChest,
                    chestKey: map.GetChestKey(chest.PixelX, chest.PixelY),
                    chestItemName: chest.Arguments[0]);
                for (var index = detour.Count - 2; index >= 0; index--)
                    points.Add(ToJourneyPoint(map, detour[index]));
                for (var index = detourIndex + 1; index < basePath.Count; index++)
                    points.Add(ToJourneyPoint(map, basePath[index]));
                plan = new LiveWallpaperJourneyPlan(points.ToArray());
                return true;
            }
            return false;
        }

        private static float DistanceToSegmentSquared(
            float x, float y, float startX, float startY, float endX, float endY)
        {
            var segmentX = endX - startX;
            var segmentY = endY - startY;
            var lengthSquared = segmentX * segmentX + segmentY * segmentY;
            if (lengthSquared <= 0.001f)
            {
                var deltaX = x - startX;
                var deltaY = y - startY;
                return deltaX * deltaX + deltaY * deltaY;
            }
            var progress = Math.Clamp(
                ((x - startX) * segmentX + (y - startY) * segmentY) /
                lengthSquared, 0f, 1f);
            var nearestX = startX + segmentX * progress;
            var nearestY = startY + segmentY * progress;
            var nearestDeltaX = x - nearestX;
            var nearestDeltaY = y - nearestY;
            return nearestDeltaX * nearestDeltaX + nearestDeltaY * nearestDeltaY;
        }

        private static bool TryAddCombat(
            LiveWallpaperMap map,
            List<Point> basePath,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int pathMinX,
            int pathMinY,
            int pathMaxX,
            int pathMaxY,
            int behavior,
            out LiveWallpaperJourneyPlan plan)
        {
            plan = null;
            foreach (var point in basePath)
            {
                if (IsWaterPoint(map, point))
                    return false;
            }
            var enemies = new List<int>();
            for (var index = 0; index < map.Enemies.Count; index++)
            {
                if (!map.TryGetEnemyBody(index, out var enemy)) continue;
                var centerX = enemy.X + enemy.Width / 2;
                var centerY = enemy.Y + enemy.Height / 2;
                if (centerX >= minX && centerX <= maxX &&
                    centerY >= minY && centerY <= maxY)
                    enemies.Add(index);
            }
            if (enemies.Count == 0)
                return false;

            var enemyOffset = behavior % enemies.Count;
            for (var enemyAttempt = 0; enemyAttempt < enemies.Count; enemyAttempt++)
            {
                var enemyIndex = enemies[(enemyOffset + enemyAttempt) % enemies.Count];
                if (!map.TryGetEnemyBody(enemyIndex, out var enemyBody)) continue;
                var approaches = GetEnemyApproaches(enemyBody);
                for (var approachAttempt = 0; approachAttempt < approaches.Length;
                     approachAttempt++)
                {
                    var approach = approaches[(approachAttempt + behavior) % approaches.Length];
                    approach = new Point(
                        Snap(Math.Clamp(approach.X, minX, maxX)),
                        Snap(Math.Clamp(approach.Y, minY, maxY)));
                    if (!IsWalkable(map, approach.X, approach.Y, true) ||
                        IsWaterPoint(map, approach))
                        continue;
                    var detourIndex = FindNearestPointIndex(basePath, approach);
                    var detourStart = basePath[detourIndex];
                    var detour = FindPath(
                        map, detourStart.X, detourStart.Y, approach.X, approach.Y,
                        pathMinX, pathMinY, pathMaxX, pathMaxY,
                        includeHoles: true);
                    if (detour.Count < 2)
                        continue;

                    var points = new List<LiveWallpaperJourneyPoint>();
                    for (var index = 0; index <= detourIndex; index++)
                        points.Add(ToJourneyPoint(map, basePath[index]));
                    for (var index = 1; index < detour.Count; index++)
                        points.Add(ToJourneyPoint(map, detour[index]));
                    var combatPoint = points.Count - 1;
                    points[combatPoint] = new LiveWallpaperJourneyPoint(
                        approach.X, approach.Y, LiveWallpaperJourneyAction.Attack);
                    for (var index = detour.Count - 2; index >= 0; index--)
                        points.Add(ToJourneyPoint(map, detour[index]));
                    for (var index = detourIndex + 1; index < basePath.Count; index++)
                        points.Add(ToJourneyPoint(map, basePath[index]));
                    plan = new LiveWallpaperJourneyPlan(
                        points.ToArray(), combatPointIndex: combatPoint,
                        combatEnemyIndex: enemyIndex);
                    return true;
                }
            }
            return false;
        }

        private static Point[] GetEnemyApproaches(LiveWallpaperCollisionBounds body)
        {
            var enemy = new Rectangle((int)MathF.Round(body.X), (int)MathF.Round(body.Y),
                (int)body.Width, (int)body.Height);
            var centerX = enemy.X + enemy.Width / 2;
            var centerY = enemy.Y + enemy.Height / 2;
            return
            [
                new Point(enemy.X - 12, centerY + 5),
                new Point(enemy.Right + 12, centerY + 5),
                new Point(centerX, enemy.Y - 6),
                new Point(centerX, enemy.Bottom + 14)
            ];
        }

        private static LiveWallpaperJourneyPlan AddRoosterFlight(
            LiveWallpaperMap map,
            List<Point> groundPath,
            int minX,
            int minY,
            int maxX,
            int maxY)
        {
            if (groundPath.Count < 7)
                return ToPlan(map, groundPath);
            var pickupGroundIndex = Math.Clamp(groundPath.Count / 3, 1, groundPath.Count - 3);
            var landingGroundIndex = Math.Clamp(
                groundPath.Count * 2 / 3, pickupGroundIndex + 2, groundPath.Count - 2);
            var pickup = groundPath[pickupGroundIndex];
            var landing = groundPath[landingGroundIndex];
            var airPath = FindPath(
                map, pickup.X, pickup.Y, landing.X, landing.Y,
                minX, minY, maxX, maxY, includeHoles: false);
            if (airPath.Count < 2)
                return ToPlan(map, groundPath);

            var points = new List<LiveWallpaperJourneyPoint>();
            for (var index = 0; index <= pickupGroundIndex; index++)
                points.Add(ToJourneyPoint(map, groundPath[index]));
            var pickupPointIndex = points.Count - 1;
            for (var index = 1; index < airPath.Count; index++)
                points.Add(new LiveWallpaperJourneyPoint(
                    airPath[index].X, airPath[index].Y,
                    LiveWallpaperJourneyAction.RoosterFly));
            var landingPointIndex = points.Count - 1;
            for (var index = landingGroundIndex + 1; index < groundPath.Count; index++)
                points.Add(ToJourneyPoint(map, groundPath[index]));
            return new LiveWallpaperJourneyPlan(
                points.ToArray(), roosterPickupPointIndex: pickupPointIndex,
                roosterLandingPointIndex: landingPointIndex);
        }

        private static bool TryAddInteraction(
            LiveWallpaperMap map,
            List<Point> basePath,
            Endpoint start,
            Endpoint end,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int pathMinX,
            int pathMinY,
            int pathMaxX,
            int pathMaxY,
            int behavior,
            out LiveWallpaperJourneyPlan plan)
        {
            plan = null;
            var actors = new List<int>();
            for (var index = 0; index < map.Actors.Count; index++)
            {
                var actor = map.Actors[index];
                if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0 ||
                    actor.Kind is LiveWallpaperMapActorKind.Butterfly or
                        LiveWallpaperMapActorKind.Owl)
                    continue;
                if (!map.TryGetActorBody(index, out var actorBody)) continue;
                var centerX = actorBody.X + actorBody.Width / 2;
                var centerY = actorBody.Y + actorBody.Height / 2;
                if (centerX >= minX && centerX <= maxX && centerY >= minY && centerY <= maxY)
                    actors.Add(index);
            }
            if (actors.Count == 0)
                return false;

            var actorOffset = behavior % actors.Count;
            for (var actorAttempt = 0; actorAttempt < actors.Count; actorAttempt++)
            {
                var actorIndex = actors[(actorOffset + actorAttempt) % actors.Count];
                if (!map.TryGetActorBody(actorIndex, out var actorBody)) continue;
                var approaches = GetActorApproaches(actorBody);
                for (var approachAttempt = 0; approachAttempt < approaches.Length; approachAttempt++)
                {
                    var approach = approaches[(approachAttempt + behavior) % approaches.Length];
                    approach = new Point(
                        Snap(Math.Clamp(approach.X, minX, maxX)),
                        Snap(Math.Clamp(approach.Y, minY, maxY)));
                    if (!IsWalkable(map, approach.X, approach.Y, true))
                        continue;
                    var detourIndex = FindNearestPointIndex(basePath, approach);
                    var detourStart = basePath[detourIndex];
                    var detour = FindPath(
                        map, detourStart.X, detourStart.Y, approach.X, approach.Y,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, includeHoles: true);
                    if (detour.Count < 2)
                        continue;

                    var points = new List<LiveWallpaperJourneyPoint>();
                    for (var index = 0; index <= detourIndex; index++)
                        points.Add(ToJourneyPoint(map, basePath[index]));
                    for (var index = 1; index < detour.Count; index++)
                        points.Add(ToJourneyPoint(map, detour[index]));
                    var interactionPoint = points.Count - 1;
                    points[interactionPoint] = new LiveWallpaperJourneyPoint(
                        approach.X, approach.Y, LiveWallpaperJourneyAction.Interact);
                    for (var index = detour.Count - 2; index >= 0; index--)
                        points.Add(ToJourneyPoint(map, detour[index]));
                    for (var index = detourIndex + 1; index < basePath.Count; index++)
                        points.Add(ToJourneyPoint(map, basePath[index]));
                    plan = new LiveWallpaperJourneyPlan(
                        points.ToArray(), interactionPoint, actorIndex);
                    return true;
                }
            }
            return false;
        }

        private static int FindNearestPointIndex(List<Point> path, Point target)
        {
            var nearestIndex = 0;
            var nearestDistance = long.MaxValue;
            for (var index = 0; index < path.Count; index++)
            {
                var deltaX = path[index].X - target.X;
                var deltaY = path[index].Y - target.Y;
                var distance = (long)deltaX * deltaX + (long)deltaY * deltaY;
                if (distance >= nearestDistance)
                    continue;
                nearestDistance = distance;
                nearestIndex = index;
            }
            return nearestIndex;
        }

        private static Point[] GetActorApproaches(LiveWallpaperCollisionBounds body)
        {
            var actor = new Rectangle((int)MathF.Round(body.X), (int)MathF.Round(body.Y),
                (int)body.Width, (int)body.Height);
            var centerX = actor.X + actor.Width / 2;
            var centerY = actor.Y + actor.Height / 2;
            return
            [
                new Point(actor.X - 8, centerY + 5),
                new Point(actor.Right + 8, centerY + 5),
                new Point(centerX, actor.Y - 2),
                new Point(centerX, actor.Bottom + 12)
            ];
        }

        private static LiveWallpaperJourneyPlan ToPlan(
            LiveWallpaperMap map, List<Point> path)
        {
            var points = new LiveWallpaperJourneyPoint[path.Count];
            for (var index = 0; index < path.Count; index++)
                points[index] = ToJourneyPoint(map, path[index]);
            return new LiveWallpaperJourneyPlan(points);
        }

        private static LiveWallpaperJourneyPlan AddPegasusDash(
            LiveWallpaperJourneyPlan plan)
        {
            if (plan == null || plan.Points.Count < 9)
                return plan;
            var bestStart = -1;
            var bestEnd = -1;
            var bestDistance = 0f;
            for (var start = 0; start < plan.Points.Count - 1;
                 start++)
            {
                if (plan.Points[start].Action != LiveWallpaperJourneyAction.Walk ||
                    plan.Points[start + 1].Action !=
                        LiveWallpaperJourneyAction.Walk)
                    continue;
                var firstDeltaX = plan.Points[start + 1].PixelX -
                                  plan.Points[start].PixelX;
                var firstDeltaY = plan.Points[start + 1].PixelY -
                                  plan.Points[start].PixelY;
                if (firstDeltaX != 0f && firstDeltaY != 0f ||
                    firstDeltaX == 0f && firstDeltaY == 0f)
                    continue;
                var end = start + 1;
                var distance = MathF.Abs(firstDeltaX) + MathF.Abs(firstDeltaY);
                while (end + 1 < plan.Points.Count &&
                       plan.Points[end].Action ==
                           LiveWallpaperJourneyAction.Walk &&
                       plan.Points[end + 1].Action ==
                           LiveWallpaperJourneyAction.Walk)
                {
                    var deltaX = plan.Points[end + 1].PixelX -
                                 plan.Points[end].PixelX;
                    var deltaY = plan.Points[end + 1].PixelY -
                                 plan.Points[end].PixelY;
                    if (MathF.Sign(deltaX) != MathF.Sign(firstDeltaX) ||
                        MathF.Sign(deltaY) != MathF.Sign(firstDeltaY) ||
                        deltaX != 0f && deltaY != 0f)
                        break;
                    distance += MathF.Abs(deltaX) + MathF.Abs(deltaY);
                    end++;
                }
                if (distance > bestDistance)
                {
                    bestStart = start;
                    bestEnd = end;
                    bestDistance = distance;
                }
                start = Math.Max(start, end - 1);
            }
            // ObjLink needs enough open ground for its charge to be worthwhile.
            // Eight 8-pixel planner steps give one exact 64-pixel dash.
            if (bestStart < 0 || bestDistance < GridStep * 8)
                return plan;
            var points = new LiveWallpaperJourneyPoint[plan.Points.Count];
            for (var index = 0; index < points.Length; index++)
                points[index] = plan.Points[index];
            var charge = points[bestStart];
            points[bestStart] = new LiveWallpaperJourneyPoint(
                charge.PixelX, charge.PixelY,
                LiveWallpaperJourneyAction.PegasusCharge,
                charge.BushKey, charge.StoneKey,
                moveStoneKey: charge.MoveStoneKey);
            for (var index = bestStart + 1; index <= bestEnd; index++)
            {
                var dash = points[index];
                points[index] = new LiveWallpaperJourneyPoint(
                    dash.PixelX, dash.PixelY,
                    LiveWallpaperJourneyAction.PegasusDash,
                    dash.BushKey, dash.StoneKey,
                    moveStoneKey: dash.MoveStoneKey);
            }
            return new LiveWallpaperJourneyPlan(
                points,
                plan.InteractionPointIndex,
                plan.InteractionActorIndex,
                plan.RoosterPickupPointIndex,
                plan.RoosterLandingPointIndex,
                plan.CombatPointIndex,
                plan.CombatEnemyIndex);
        }

        private static bool TryCreateHookshotPlan(
            LiveWallpaperMap map,
            Endpoint startEndpoint,
            Endpoint endEndpoint,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int behavior,
            out LiveWallpaperJourneyPlan plan)
        {
            plan = null;
            if (map?.HookshotTargets == null || map.HookshotTargets.Count == 0)
                return false;

            var targetOffset = behavior % map.HookshotTargets.Count;
            for (var targetAttempt = 0;
                 targetAttempt < map.HookshotTargets.Count;
                 targetAttempt++)
            {
                var target = map.HookshotTargets[
                    (targetOffset + targetAttempt) % map.HookshotTargets.Count];
                if (target.X + target.Width < minX || target.X > maxX ||
                    target.Y + target.Height < minY || target.Y > maxY)
                    continue;

                for (var directionAttempt = 0; directionAttempt < 4;
                     directionAttempt++)
                {
                    var direction = (behavior + directionAttempt) & 3;
                    for (var distance = 112; distance >= 40; distance -= GridStep)
                    {
                        Point shot;
                        Point landing;
                        Point departure;
                        Vector2 hookStart;
                        Vector2 hookContact;
                        switch (direction)
                        {
                            // ObjLink direction 0: fire left from (-5,-4).
                            case 0:
                                shot = new Point(
                                    target.X + target.Width + distance,
                                    target.Y + target.Height / 2);
                                landing = new Point(
                                    target.X + target.Width + 4, shot.Y);
                                departure = new Point(
                                    target.X + target.Width + 12, shot.Y);
                                hookStart = new Vector2(shot.X - 5, shot.Y - 4);
                                hookContact = new Vector2(
                                    target.X + target.Width, shot.Y - 4);
                                break;
                            // ObjLink direction 1: fire up from (-3,-12).
                            case 1:
                                shot = new Point(
                                    target.X + target.Width / 2,
                                    target.Y + target.Height + distance);
                                landing = new Point(
                                    shot.X, target.Y + target.Height + 10);
                                departure = new Point(
                                    shot.X, target.Y + target.Height + 16);
                                hookStart = new Vector2(shot.X - 3, shot.Y - 12);
                                hookContact = new Vector2(
                                    shot.X - 3, target.Y + target.Height);
                                break;
                            // ObjLink direction 2: fire right from (+5,-4).
                            case 2:
                                shot = new Point(
                                    target.X - distance,
                                    target.Y + target.Height / 2);
                                landing = new Point(target.X - 4, shot.Y);
                                departure = new Point(target.X - 12, shot.Y);
                                hookStart = new Vector2(shot.X + 5, shot.Y - 4);
                                hookContact = new Vector2(target.X, shot.Y - 4);
                                break;
                            // ObjLink direction 3: fire down from (+3,0).
                            default:
                                shot = new Point(
                                    target.X + target.Width / 2,
                                    target.Y - distance);
                                landing = new Point(shot.X, target.Y);
                                departure = new Point(shot.X, target.Y - 8);
                                hookStart = new Vector2(shot.X + 3, shot.Y);
                                hookContact = new Vector2(shot.X + 3, target.Y);
                                break;
                        }

                        if (!IsInside(shot, minX, minY, maxX, maxY) ||
                            !IsInside(departure, minX, minY, maxX, maxY) ||
                            Vector2.Distance(hookStart, hookContact) >
                                LinkGameplayMotion.HookshotMaximumDistance ||
                            !IsWalkable(map, shot.X, shot.Y, includeHoles: true) ||
                            !IsWalkable(
                                map, landing.X, landing.Y, includeHoles: false) ||
                            !IsWalkable(
                                map, departure.X, departure.Y,
                                includeHoles: true) ||
                            !HasClearHookshotTravel(
                                map, hookStart, hookContact, shot, landing))
                            continue;

                        var before = FindPath(
                            map, startEndpoint.X, startEndpoint.Y,
                            shot.X, shot.Y, minX, minY, maxX, maxY,
                            includeHoles: true, includeBushes: false,
                            includeStones: false, includeMoveStones: false);
                        if (before.Count < 2)
                            continue;
                        var after = FindPath(
                            map, departure.X, departure.Y,
                            endEndpoint.X, endEndpoint.Y,
                            minX, minY, maxX, maxY,
                            includeHoles: true, includeBushes: false,
                            includeStones: false, includeMoveStones: false);
                        if (after.Count < 2)
                            continue;

                        var points = new List<LiveWallpaperJourneyPoint>(
                            before.Count + after.Count + 2);
                        foreach (var point in before)
                            points.Add(ToJourneyPoint(map, point));
                        points.Add(new LiveWallpaperJourneyPoint(
                            landing.X, landing.Y,
                            LiveWallpaperJourneyAction.Hookshot,
                            hookshotTargetX: hookContact.X,
                            hookshotTargetY: hookContact.Y));
                        points.Add(new LiveWallpaperJourneyPoint(
                            departure.X, departure.Y));
                        for (var index = 1; index < after.Count; index++)
                            points.Add(ToJourneyPoint(map, after[index]));
                        if (endEndpoint.HasExit)
                        {
                            points.Add(new LiveWallpaperJourneyPoint(
                                endEndpoint.ExitX, endEndpoint.ExitY,
                                LiveWallpaperJourneyAction.Exit));
                        }
                        plan = new LiveWallpaperJourneyPlan(points.ToArray());
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsInside(
            Point point, int minX, int minY, int maxX, int maxY) =>
            point.X >= minX && point.X <= maxX &&
            point.Y >= minY && point.Y <= maxY;

        private static bool HasClearHookshotTravel(
            LiveWallpaperMap map,
            Vector2 hookStart,
            Vector2 hookContact,
            Point linkStart,
            Point linkLanding)
        {
            var hookDelta = hookContact - hookStart;
            var hookDistance = hookDelta.Length();
            if (hookDistance <= 0f)
                return false;
            var hookDirection = hookDelta / hookDistance;
            for (var distance = 0f; distance + 3f < hookDistance;
                 distance += LinkGameplayMotion.HookshotSpeed)
            {
                var position = hookStart + hookDirection * distance;
                if (map.IntersectsVoid(position.X - 2, position.Y - 2, 4, 4) ||
                    map.IntersectsCollision(
                        position.X - 2, position.Y - 2, 4, 4,
                        includeHoles: false))
                    return false;
            }

            var linkStartVector = new Vector2(linkStart.X, linkStart.Y);
            var linkLandingVector = new Vector2(linkLanding.X, linkLanding.Y);
            var linkDelta = linkLandingVector - linkStartVector;
            var linkDistance = linkDelta.Length();
            if (linkDistance <= 0f)
                return false;
            var linkDirection = linkDelta / linkDistance;
            for (var distance = 0f; distance < linkDistance;
                 distance += LinkGameplayMotion.HookshotSpeed)
            {
                var position = linkStartVector + linkDirection * distance;
                if (!IsWalkable(
                        map, position.X, position.Y, includeHoles: false))
                    return false;
            }
            return true;
        }

        private static bool TryCreateTraversableObjectPlan(
            LiveWallpaperMap map, List<Point> path,
            out LiveWallpaperJourneyPlan plan)
        {
            plan = null;
            if (map == null || path == null || path.Count < 2)
                return false;
            var points = new LiveWallpaperJourneyPoint[path.Count];
            for (var index = 0; index < path.Count; index++)
                points[index] = ToJourneyPoint(map, path[index]);
            var foundObject = false;
            var scheduledBushes = new HashSet<int>();
            var scheduledStones = new HashSet<int>();
            var scheduledMoveStones = new HashSet<int>();
            for (var index = 0; index < path.Count; index++)
            {
                var point = path[index];
                if (map.TryGetCuttableVegetationKey(
                        point.X + LinkBodyOffsetX,
                        point.Y + LinkBodyOffsetY,
                        LinkBodyWidth, LinkBodyHeight,
                        out var bushKey) && scheduledBushes.Add(bushKey))
                {
                    var cutIndex = Math.Max(0, index - 1);
                    var approach = points[cutIndex];
                    points[cutIndex] = new LiveWallpaperJourneyPoint(
                        approach.PixelX, approach.PixelY,
                        LiveWallpaperJourneyAction.CutBush, bushKey);
                    foundObject = true;
                }
                if (map.TryGetStoneKey(
                        point.X + LinkBodyOffsetX,
                        point.Y + LinkBodyOffsetY,
                        LinkBodyWidth, LinkBodyHeight,
                        out var stoneKey) && scheduledStones.Add(stoneKey))
                {
                    var liftIndex = Math.Max(0, index - 1);
                    var approach = points[liftIndex];
                    points[liftIndex] = new LiveWallpaperJourneyPoint(
                        approach.PixelX, approach.PixelY,
                        LiveWallpaperJourneyAction.LiftStone,
                        stoneKey: stoneKey);
                    foundObject = true;
                }
                if (map.TryGetMoveStoneAt(
                        point.X + LinkBodyOffsetX,
                        point.Y + LinkBodyOffsetY,
                        LinkBodyWidth, LinkBodyHeight,
                        out var moveStoneKey) &&
                    scheduledMoveStones.Add(moveStoneKey))
                {
                    var pushIndex = Math.Max(0, index - 1);
                    var approach = points[pushIndex];
                    points[pushIndex] = new LiveWallpaperJourneyPoint(
                        approach.PixelX, approach.PixelY,
                        LiveWallpaperJourneyAction.PushBlock,
                        moveStoneKey: moveStoneKey);
                    foundObject = true;
                }
            }
            if (!foundObject)
                return false;
            plan = new LiveWallpaperJourneyPlan(points);
            return true;
        }

        private static LiveWallpaperJourneyPoint ToJourneyPoint(
            LiveWallpaperMap map, Point point) =>
            new(point.X, point.Y,
                IsWaterPoint(map, point)
                    ? LiveWallpaperJourneyAction.Swim
                    : LiveWallpaperJourneyAction.Walk);

        private static LiveWallpaperJourneyPlan ToJumpPlan(
            LiveWallpaperMap map, List<Point> path) =>
            ApplyJumpActions(map, path, ToPlan(map, path));

        private static LiveWallpaperJourneyPlan ApplyJumpActions(
            LiveWallpaperMap map, List<Point> path,
            LiveWallpaperJourneyPlan plan)
        {
            var points = new LiveWallpaperJourneyPoint[path.Count];
            for (var index = 0; index < path.Count; index++)
                points[index] = plan.Points[index];
            for (var index = 0; index < path.Count; index++)
            {
                if (!IsHolePoint(map, path[index]) ||
                    index > 0 && IsHolePoint(map, path[index - 1]))
                    continue;
                var end = index;
                while (end + 1 < path.Count && IsHolePoint(map, path[end + 1]))
                    end++;
                var jumpDistance = GetJumpTravelDistance(
                    path, Math.Max(0, index - 1),
                    Math.Min(path.Count - 1, end + 1));
                var pegasusJump = jumpDistance >
                    GetMaximumJumpDistance(LinkGameplayMotion.WalkSpeed);
                // Targeting the first hole node presses the feather while Link
                // still stands on the preceding safe node. Wider spans first use
                // the real Pegasus charge, then preserve its running-jump speed.
                if (pegasusJump && index > 0)
                {
                    var charge = points[index - 1];
                    points[index - 1] = new LiveWallpaperJourneyPoint(
                        charge.PixelX, charge.PixelY,
                        LiveWallpaperJourneyAction.PegasusCharge,
                        charge.BushKey, charge.StoneKey,
                        moveStoneKey: charge.MoveStoneKey);
                }
                var jumpPoint = points[index];
                points[index] = new LiveWallpaperJourneyPoint(
                    jumpPoint.PixelX, jumpPoint.PixelY,
                    pegasusJump
                        ? LiveWallpaperJourneyAction.PegasusJump
                        : LiveWallpaperJourneyAction.FeatherJump,
                    jumpPoint.BushKey, jumpPoint.StoneKey,
                    moveStoneKey: jumpPoint.MoveStoneKey);
                index = end;
            }
            return new LiveWallpaperJourneyPlan(
                points,
                plan.InteractionPointIndex,
                plan.InteractionActorIndex,
                plan.RoosterPickupPointIndex,
                plan.RoosterLandingPointIndex,
                plan.CombatPointIndex,
                plan.CombatEnemyIndex);
        }

        private static bool HasValidJumpSpans(
            LiveWallpaperMap map, List<Point> path)
        {
            for (var index = 0; index < path.Count; index++)
            {
                if (!IsHolePoint(map, path[index]) ||
                    index > 0 && IsHolePoint(map, path[index - 1]))
                    continue;
                var end = index;
                while (end + 1 < path.Count && IsHolePoint(map, path[end + 1]))
                    end++;
                if (index == 0 || end + 1 >= path.Count)
                    return false;

                var takeoff = path[index - 1];
                var landing = path[end + 1];
                var stepX = path[index].X - takeoff.X;
                var stepY = path[index].Y - takeoff.Y;
                var jumpDistance = GetJumpTravelDistance(
                    path, index - 1, end + 1);
                var pegasusJump = jumpDistance >
                    GetMaximumJumpDistance(LinkGameplayMotion.WalkSpeed);
                // Pegasus running locks its dominant axis. Only a straight,
                // cardinal installed-grid span is a deterministic running jump.
                if (pegasusJump &&
                    Math.Abs(stepX) + Math.Abs(stepY) != GridStep)
                    return false;
                for (var pointIndex = index;
                     pointIndex <= end + 1;
                     pointIndex++)
                {
                    var expectedSteps = pointIndex - index + 1;
                    var point = path[pointIndex];
                    if (pegasusJump &&
                        (point.X != takeoff.X + stepX * expectedSteps ||
                         point.Y != takeoff.Y + stepY * expectedSteps) ||
                        // Holes are ignored during the airborne clearance test;
                        // every other installed collider, actor, and enemy is not.
                        !IsWalkable(
                            map, point.X, point.Y, includeHoles: false,
                            includeBushes: true, includeStones: true))
                        return false;
                }
                if (jumpDistance > GetMaximumJumpDistance(
                        LinkGameplayMotion.PegasusBootsSpeed) + 0.001f)
                    return false;
                index = end;
            }
            return true;
        }

        private static float GetMaximumJumpDistance(float speed) =>
            LinkGameplayMotion.FeatherTravelFramesAt60Fps * speed;

        private static float GetJumpTravelDistance(
            List<Point> path, int startIndex, int endIndex)
        {
            var distance = 0f;
            for (var index = startIndex + 1; index <= endIndex; index++)
                distance += Vector2.Distance(
                    new Vector2(path[index - 1].X, path[index - 1].Y),
                    new Vector2(path[index].X, path[index].Y));
            return distance;
        }

        private static bool IsHolePoint(LiveWallpaperMap map, Point point) =>
            map.IntersectsHole(
                point.X + LinkBodyOffsetX,
                point.Y + LinkBodyOffsetY,
                LinkBodyWidth,
                LinkBodyHeight);

        private static bool IsWaterPoint(LiveWallpaperMap map, Point point) =>
            map.IsWaterAt(point.X, point.Y - LinkBodyHeight * 0.5f);

        private static void ExpandPathBoundsForNearbyPortals(
            LiveWallpaperMap map,
            int minX,
            int minY,
            int maxX,
            int maxY,
            ref int pathMinX,
            ref int pathMinY,
            ref int pathMaxX,
            ref int pathMaxY)
        {
            foreach (var portal in map.Portals)
            {
                if (!portal.HasDestination || portal.Mode is not (0 or 1))
                    continue;
                var targetX = Snap((int)MathF.Round(portal.LinkTargetX));
                var targetY = Snap((int)MathF.Round(portal.LinkTargetY));
                // ObjDoor may deliberately finish its walk exactly one grid step
                // beyond the ordinary 8px body inset (house exits are at the
                // bottom map boundary). Admit only those adjacent canonical
                // targets; distant doors remain outside the current crop.
                if (targetX < minX - GridStep || targetX > maxX + GridStep ||
                    targetY < minY - GridStep || targetY > maxY + GridStep ||
                    !IsWalkable(map, targetX, targetY, includeHoles: true))
                    continue;
                pathMinX = Math.Min(pathMinX, targetX);
                pathMinY = Math.Min(pathMinY, targetY);
                pathMaxX = Math.Max(pathMaxX, targetX);
                pathMaxY = Math.Max(pathMaxY, targetY);
            }
        }

        private static List<Endpoint> BuildEndpoints(
            LiveWallpaperMap map,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int pathMinX,
            int pathMinY,
            int pathMaxX,
            int pathMaxY,
            string excludedPortalEntryId)
        {
            var endpoints = new List<Endpoint>();
            AddEdgeRuns(map, endpoints, side: 0, minX, minY, maxX, maxY);
            AddEdgeRuns(map, endpoints, side: 1, minX, minY, maxX, maxY);
            AddEdgeRuns(map, endpoints, side: 2, minX, minY, maxX, maxY);
            AddEdgeRuns(map, endpoints, side: 3, minX, minY, maxX, maxY);
            if (!string.IsNullOrWhiteSpace(excludedPortalEntryId))
            {
                foreach (var portal in map.Portals)
                {
                    if (!string.Equals(
                            portal.EntryId, excludedPortalEntryId,
                            StringComparison.Ordinal))
                        continue;
                    endpoints.RemoveAll(endpoint =>
                    {
                        var deltaX = endpoint.X - portal.LinkTargetX;
                        var deltaY = endpoint.Y - portal.LinkTargetY;
                        return deltaX * deltaX + deltaY * deltaY <= 2.25f;
                    });
                }
            }
            foreach (var portal in map.Portals)
            {
                if (portal.IsHoleTeleporter)
                    continue;
                // ObjDoor modes 0 and 1 are the only ordinary transitions which
                // walk Link to MapTransitionEnd. Falling, swimming, and no-walk
                // transition modes are not valid wallpaper walking endpoints.
                if (portal.Mode is not (0 or 1) || !portal.HasDestination)
                    continue;
                if (string.Equals(
                        portal.EntryId, excludedPortalEntryId,
                        StringComparison.Ordinal))
                    continue;
                var x = Snap((int)MathF.Round(portal.LinkTargetX));
                var y = Snap((int)MathF.Round(portal.LinkTargetY));
                if (x < pathMinX || x > pathMaxX ||
                    y < pathMinY || y > pathMaxY ||
                    !IsWalkable(map, x, y, true))
                    continue;
                // LinkTarget is ObjDoor's exact transition end. Once reached,
                // the renderer hides Link as the real map transition would.
                endpoints.Add(new Endpoint(x, y, 4, isDoor: true));
            }
            return endpoints;
        }

        private static void AddEdgeRuns(
            LiveWallpaperMap map,
            List<Endpoint> endpoints,
            int side,
            int minX,
            int minY,
            int maxX,
            int maxY)
        {
            var vertical = side is 0 or 2;
            var fixedValue = side switch
            {
                0 => minX,
                1 => minY,
                2 => maxX,
                _ => maxY
            };
            var start = vertical ? minY : minX;
            var end = vertical ? maxY : maxX;
            var runStart = int.MinValue;
            var previous = int.MinValue;
            for (var value = start; value <= end; value += GridStep)
            {
                var x = vertical ? fixedValue : value;
                var y = vertical ? value : fixedValue;
                var exitX = x + (side == 0 ? -16 : side == 2 ? 16 : 0);
                var exitY = y + (side == 1 ? -16 : side == 3 ? 16 : 0);
                var walkable = IsWalkable(map, x, y, includeHoles: true);
                if (walkable && runStart == int.MinValue)
                    runStart = value;
                if (walkable)
                    previous = value;
                if ((!walkable || value + GridStep > end) && runStart != int.MinValue)
                {
                    var runLength = previous - runStart;
                    AddEdgeEndpoint(endpoints, vertical, fixedValue,
                        Snap((runStart + previous) / 2), side);
                    if (runLength >= GridStep * 6)
                    {
                        AddEdgeEndpoint(endpoints, vertical, fixedValue,
                            Snap(runStart + runLength / 4), side);
                        AddEdgeEndpoint(endpoints, vertical, fixedValue,
                            Snap(runStart + runLength * 3 / 4), side);
                    }
                    runStart = int.MinValue;
                }
            }
        }

        private static void AddEdgeEndpoint(
            List<Endpoint> endpoints,
            bool vertical,
            int fixedValue,
            int variableValue,
            int side)
        {
            var x = vertical ? fixedValue : variableValue;
            var y = vertical ? variableValue : fixedValue;
            foreach (var endpoint in endpoints)
            {
                if (endpoint.X == x && endpoint.Y == y && endpoint.Side == side)
                    return;
            }
            endpoints.Add(new Endpoint(x, y, side, isDoor: false));
            var exitX = x + (side == 0 ? -16 : side == 2 ? 16 : 0);
            var exitY = y + (side == 1 ? -16 : side == 3 ? 16 : 0);
            endpoints[^1] = new Endpoint(
                x, y, side, isDoor: false,
                exitX, exitY, hasExit: true);
        }

        private static bool TryAppendLoadingZoneExit(
            LiveWallpaperMap map,
            List<Point> path,
            Endpoint endpoint,
            bool includeHoles)
        {
            if (!endpoint.HasExit || path.Count == 0 ||
                !IsWalkable(
                    map, endpoint.ExitX, endpoint.ExitY, includeHoles,
                    includeBushes: false, includeStones: false,
                    includeMoveStones: false))
                return false;
            var last = path[^1];
            if (last.X == endpoint.ExitX && last.Y == endpoint.ExitY)
                return true;
            path.Add(new Point(endpoint.ExitX, endpoint.ExitY));
            return true;
        }

        private static bool HasTraversableLoadingZoneExit(
            LiveWallpaperMap map, Endpoint endpoint, bool includeHoles) =>
            endpoint.HasExit &&
            IsWalkable(
                map, endpoint.ExitX, endpoint.ExitY, includeHoles,
                includeBushes: false, includeStones: false,
                includeMoveStones: false);

        public static int GetOverworldFieldKey(float pixelX, float pixelY)
        {
            var fieldX = (int)MathF.Floor(pixelX / 160f);
            var fieldY = (int)MathF.Floor(pixelY / 128f);
            return (fieldY << 16) ^ (fieldX & 0xffff);
        }

        public static bool TryGetNextCoverageFieldKey(
            LiveWallpaperMap map,
            float currentPixelX,
            float currentPixelY,
            IReadOnlySet<int> visitedFieldKeys,
            out int nextFieldKey)
        {
            nextFieldKey = -1;
            if (map == null || visitedFieldKeys == null)
                return false;
            var startKey = GetOverworldFieldKey(
                currentPixelX, currentPixelY);
            var queue = new Queue<int>();
            var seen = new HashSet<int> { startKey };
            var firstSteps = new Dictionary<int, int>
            {
                [startKey] = startKey
            };
            queue.Enqueue(startKey);
            while (queue.Count > 0)
            {
                var currentKey = queue.Dequeue();
                foreach (var neighborKey in GetTraversableFieldNeighbors(
                             map, currentKey))
                {
                    if (!seen.Add(neighborKey))
                        continue;
                    var firstStep = currentKey == startKey
                        ? neighborKey
                        : firstSteps[currentKey];
                    firstSteps[neighborKey] = firstStep;
                    if (!visitedFieldKeys.Contains(neighborKey))
                    {
                        nextFieldKey = firstStep;
                        return true;
                    }
                    queue.Enqueue(neighborKey);
                }
            }
            return false;
        }

        public static IReadOnlySet<int> GetReachableOverworldFieldKeys(
            LiveWallpaperMap map, float startPixelX, float startPixelY)
        {
            var reachable = new HashSet<int>();
            if (map == null)
                return reachable;
            var startKey = GetOverworldFieldKey(startPixelX, startPixelY);
            var queue = new Queue<int>();
            reachable.Add(startKey);
            queue.Enqueue(startKey);
            while (queue.Count > 0)
            {
                foreach (var neighbor in GetTraversableFieldNeighbors(
                             map, queue.Dequeue()))
                {
                    if (!reachable.Add(neighbor))
                        continue;
                    queue.Enqueue(neighbor);
                }
            }
            return reachable;
        }

        private static IEnumerable<int> GetTraversableFieldNeighbors(
            LiveWallpaperMap map, int fieldKey)
        {
            var fieldX = fieldKey & 0xffff;
            var fieldY = fieldKey >> 16;
            var fieldColumns = (map.Width * 16 + 159) / 160;
            var fieldRows = (map.Height * 16 + 127) / 128;
            foreach (var direction in FieldDirections)
            {
                var neighborX = fieldX + direction.X;
                var neighborY = fieldY + direction.Y;
                if (neighborX < 0 || neighborX >= fieldColumns ||
                    neighborY < 0 || neighborY >= fieldRows ||
                    !HasTraversableFieldBoundary(
                        map, fieldX, fieldY,
                        direction.X, direction.Y))
                    continue;
                yield return (neighborY << 16) ^ (neighborX & 0xffff);
            }
        }

        private static bool HasTraversableFieldBoundary(
            LiveWallpaperMap map,
            int fieldX,
            int fieldY,
            int directionX,
            int directionY)
        {
            var originX = fieldX * 160;
            var originY = fieldY * 128;
            if (directionX != 0)
            {
                var currentX = directionX < 0
                    ? originX + 8
                    : Math.Min(originX + 152, map.Width * 16 - 8);
                var exitX = currentX + directionX * 16;
                var startY = originY + 8;
                var endY = Math.Min(originY + 120, map.Height * 16 - 8);
                for (var y = startY; y <= endY; y += GridStep)
                {
                    if (IsCoverageBoundaryPoint(map, currentX, y) &&
                        IsCoverageBoundaryPoint(map, exitX, y))
                        return true;
                }
                return false;
            }

            var currentY = directionY < 0
                ? originY + 8
                : Math.Min(originY + 120, map.Height * 16 - 8);
            var exitY = currentY + directionY * 16;
            var startX = originX + 8;
            var endX = Math.Min(originX + 152, map.Width * 16 - 8);
            for (var x = startX; x <= endX; x += GridStep)
            {
                if (IsCoverageBoundaryPoint(map, x, currentY) &&
                    IsCoverageBoundaryPoint(map, x, exitY))
                    return true;
            }
            return false;
        }

        private static bool IsCoverageBoundaryPoint(
            LiveWallpaperMap map, int pixelX, int pixelY) =>
            IsWalkable(
                map, pixelX, pixelY, includeHoles: true,
                includeBushes: false, includeStones: false,
                includeMoveStones: false);

        private static LiveWallpaperJourneyPlan MarkLoadingZoneExit(
            LiveWallpaperJourneyPlan plan)
        {
            if (plan == null || plan.Points.Count == 0)
                return plan;
            var points = new LiveWallpaperJourneyPoint[plan.Points.Count];
            for (var index = 0; index < points.Length; index++)
                points[index] = plan.Points[index];
            var last = points[^1];
            points[^1] = new LiveWallpaperJourneyPoint(
                last.PixelX, last.PixelY, LiveWallpaperJourneyAction.Exit);
            return new LiveWallpaperJourneyPlan(
                points,
                plan.InteractionPointIndex,
                plan.InteractionActorIndex,
                plan.RoosterPickupPointIndex,
                plan.RoosterLandingPointIndex,
                plan.CombatPointIndex,
                plan.CombatEnemyIndex);
        }

        private static List<Pair> BuildPairs(
            List<Endpoint> endpoints,
            int minX,
            int minY,
            int maxX,
            int maxY,
            bool edgeStartOnly,
            bool edgeEndpointsOnly)
        {
            var pairs = new List<Pair>();
            var minimumDistance = Math.Min(maxX - minX, maxY - minY) * 0.55f;
            for (var first = 0; first < endpoints.Count; first++)
            {
                for (var second = first + 1; second < endpoints.Count; second++)
                {
                    var start = endpoints[first];
                    var end = endpoints[second];
                    if (edgeEndpointsOnly && (start.IsDoor || end.IsDoor))
                        continue;
                    if (start.Side == end.Side && start.Side != 4)
                        continue;
                    var deltaX = end.X - start.X;
                    var deltaY = end.Y - start.Y;
                    var distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
                    if (distance < minimumDistance)
                        continue;
                    var oppositeEdges = start.Side < 4 && end.Side < 4 &&
                                        Math.Abs(start.Side - end.Side) == 2;
                    var score = distance + (oppositeEdges ? 300f : 0f) +
                                (start.IsDoor || end.IsDoor ? 90f : 0f);
                    if (!edgeStartOnly || !start.IsDoor)
                        pairs.Add(new Pair(start, end, score));
                    if (!edgeStartOnly || !end.IsDoor)
                        pairs.Add(new Pair(end, start, score - 1f));
                }
            }
            pairs.Sort((left, right) => right.Score.CompareTo(left.Score));
            return pairs;
        }

        private static List<Pair> BuildContinuationPairs(
            LiveWallpaperMap map,
            List<Endpoint> endpoints,
            float currentPixelX,
            float currentPixelY,
            int minX,
            int minY,
            int maxX,
            int maxY,
            bool edgeEndpointsOnly)
        {
            if (!TryResolveContinuationStart(
                    map, currentPixelX, currentPixelY,
                    minX, minY, maxX, maxY,
                    out var startX, out var startY))
                return [];

            var start = new Endpoint(startX, startY, side: 4, isDoor: false);
            var pairs = new List<Pair>();
            foreach (var endpoint in endpoints)
            {
                if (edgeEndpointsOnly && endpoint.IsDoor)
                    continue;
                var deltaX = endpoint.X - startX;
                var deltaY = endpoint.Y - startY;
                var distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
                pairs.Add(new Pair(
                    start, endpoint,
                    distance + (endpoint.IsDoor ? 90f : 0f)));
            }
            pairs.Sort((left, right) => right.Score.CompareTo(left.Score));
            return pairs;
        }

        private static bool TryResolveContinuationStart(
            LiveWallpaperMap map,
            float currentPixelX,
            float currentPixelY,
            int minX,
            int minY,
            int maxX,
            int maxY,
            out int startX,
            out int startY)
        {
            startX = Snap(Math.Clamp(
                (int)MathF.Round(currentPixelX), minX, maxX));
            startY = Snap(Math.Clamp(
                (int)MathF.Round(currentPixelY), minY, maxY));
            // This is Link's physical position, not a destination. Loading-zone
            // boundaries and rounding can leave his body overlapping a collider.
            // FindPath deliberately permits its start node and finds a valid
            // cardinal step out; replacing it with a nearby clean node makes
            // runtime walk straight through the collider to reach that node.
            return true;
        }

        private static List<Point> FindPath(
            LiveWallpaperMap map,
            int startX,
            int startY,
            int endX,
            int endY,
            int minX,
            int minY,
            int maxX,
            int maxY,
            bool includeHoles,
            int ignoredEnemyIndex = -1,
            bool includeBushes = true,
            bool includeStones = true,
            bool includeMoveStones = true,
            bool allowDiagonal = false,
            bool penalizeVisibleEdges = true,
            HashSet<Point> reachableWhenNoPath = null)
        {
            startX = Snap(Math.Clamp(startX, minX, maxX));
            startY = Snap(Math.Clamp(startY, minY, maxY));
            endX = Snap(Math.Clamp(endX, minX, maxX));
            endY = Snap(Math.Clamp(endY, minY, maxY));
            var columns = (maxX - minX) / GridStep + 1;
            var rows = (maxY - minY) / GridStep + 1;
            if (columns <= 0 || rows <= 0 || columns * rows > 20_000)
                return [];

            var startIndex = ToIndex(startX, startY, minX, minY, columns);
            var endIndex = ToIndex(endX, endY, minX, minY, columns);
            var previous = new int[columns * rows];
            var cost = new int[columns * rows];
            Array.Fill(previous, -1);
            Array.Fill(cost, int.MaxValue);
            var queue = new PriorityQueue<int, int>();
            cost[startIndex] = 0;
            queue.Enqueue(startIndex, 0);
            ReadOnlySpan<int> directionX = allowDiagonal
                ? [-1, 0, 1, 0, -1, 1, 1, -1]
                : [-1, 0, 1, 0];
            ReadOnlySpan<int> directionY = allowDiagonal
                ? [0, -1, 0, 1, -1, -1, 1, 1]
                : [0, -1, 0, 1];
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == endIndex)
                    break;
                var currentX = current % columns;
                var currentY = current / columns;
                for (var direction = 0; direction < directionX.Length; direction++)
                {
                    var offsetX = directionX[direction];
                    var offsetY = directionY[direction];
                    var nextX = currentX + offsetX;
                    var nextY = currentY + offsetY;
                    if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows)
                        continue;
                    var next = nextY * columns + nextX;
                    var pixelX = minX + nextX * GridStep;
                    var pixelY = minY + nextY * GridStep;
                    if (!IsWalkable(
                            map, pixelX, pixelY, includeHoles, ignoredEnemyIndex,
                            includeBushes, includeStones, includeMoveStones))
                        continue;
                    if (!includeMoveStones && map.TryGetMoveStoneAt(
                            pixelX + LinkBodyOffsetX, pixelY + LinkBodyOffsetY,
                            LinkBodyWidth, LinkBodyHeight, out var blockKey) &&
                        (offsetX != 0 && offsetY != 0 || !map.CanPushMoveStone(
                            blockKey, offsetX < 0 ? 0 : offsetX > 0 ? 2 : offsetY < 0 ? 1 : 3)))
                        continue;
                    if (offsetX != 0 && offsetY != 0)
                    {
                        var horizontalPixelX = minX + nextX * GridStep;
                        var horizontalPixelY = minY + currentY * GridStep;
                        var verticalPixelX = minX + currentX * GridStep;
                        var verticalPixelY = minY + nextY * GridStep;
                        if (!IsWalkable(
                                map, horizontalPixelX, horizontalPixelY,
                                includeHoles, ignoredEnemyIndex, includeBushes,
                                includeStones, includeMoveStones) ||
                            !IsWalkable(
                                map, verticalPixelX, verticalPixelY,
                                includeHoles, ignoredEnemyIndex, includeBushes,
                                includeStones, includeMoveStones))
                            continue;
                    }
                    var stepCost = allowDiagonal && offsetX != 0 && offsetY != 0
                        ? 14
                        : allowDiagonal ? 10 : 1;
                    var edgePenalty = penalizeVisibleEdges
                        ? GetVisibleEdgePenalty(
                            pixelX, pixelY, minX, minY, maxX, maxY,
                            next == endIndex) * (allowDiagonal ? 10 : 1)
                        : 0;
                    // A recently stalled step is less attractive, not solid.
                    // Keep a sole valid passage available and retain all of
                    // the ordinary collision/item checks above.
                    var failurePenalty = map.GetNavigationStepPenalty(
                        new Point(minX + currentX * GridStep, minY + currentY * GridStep),
                        new Point(pixelX, pixelY)) * stepCost;
                    var nextCost = cost[current] + stepCost + edgePenalty + failurePenalty;
                    if (nextCost >= cost[next])
                        continue;
                    cost[next] = nextCost;
                    previous[next] = current;
                    var distanceX = Math.Abs(endX - pixelX) / GridStep;
                    var distanceY = Math.Abs(endY - pixelY) / GridStep;
                    var heuristic = allowDiagonal
                        ? 10 * Math.Max(distanceX, distanceY) +
                          4 * Math.Min(distanceX, distanceY)
                        : distanceX + distanceY;
                    queue.Enqueue(next, nextCost + heuristic);
                }
            }
            if (startIndex != endIndex && previous[endIndex] < 0)
            {
                if (reachableWhenNoPath != null)
                {
                    for (var index = 0; index < cost.Length; index++)
                        if (cost[index] < int.MaxValue)
                            reachableWhenNoPath.Add(new Point(
                                minX + index % columns * GridStep,
                                minY + index / columns * GridStep));
                }
                return [];
            }

            var result = new List<Point>();
            for (var current = endIndex; current >= 0; current = previous[current])
            {
                var x = minX + current % columns * GridStep;
                var y = minY + current / columns * GridStep;
                result.Add(new Point(x, y));
                if (current == startIndex)
                    break;
            }
            result.Reverse();
            return result;
        }

        private static bool IsWalkable(
            LiveWallpaperMap map, float entityX, float entityY, bool includeHoles,
            int ignoredEnemyIndex = -1,
            bool includeBushes = true,
            bool includeStones = true,
            bool includeMoveStones = true) =>
            !map.IntersectsVoid(
                entityX + LinkBodyOffsetX, entityY + LinkBodyOffsetY,
                LinkBodyWidth, LinkBodyHeight) &&
            !map.IntersectsCollision(
                entityX + LinkBodyOffsetX, entityY + LinkBodyOffsetY,
                LinkBodyWidth, LinkBodyHeight, includeHoles, includeBushes,
                includeStones: includeStones,
                includeMoveStones: includeMoveStones) &&
            !map.IntersectsActor(
                entityX + LinkBodyOffsetX, entityY + LinkBodyOffsetY,
                LinkBodyWidth, LinkBodyHeight, ignoreOwl: true) &&
            !map.IntersectsEnemy(
                entityX + LinkBodyOffsetX, entityY + LinkBodyOffsetY,
                LinkBodyWidth, LinkBodyHeight, ignoredEnemyIndex);

        private static int GetVisibleEdgePenalty(
            int x, int y, int minX, int minY, int maxX, int maxY, bool endpoint)
        {
            if (endpoint)
                return 0;
            var distance = Math.Min(
                Math.Min(x - minX, maxX - x),
                Math.Min(y - minY, maxY - y));
            if (distance >= VisibleRouteMargin)
                return 0;
            return Math.Max(0, (VisibleRouteMargin - distance) / GridStep) * 6;
        }

        private static int ToIndex(
            int pixelX, int pixelY, int minX, int minY, int columns) =>
            (pixelY - minY) / GridStep * columns + (pixelX - minX) / GridStep;

        private static void GetBounds(
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            out int minX,
            out int minY,
            out int maxX,
            out int maxY)
        {
            minX = Snap(Math.Clamp(
                (viewport.OriginX * 16) + 8,
                8, map.Width * 16 - 8));
            minY = Snap(Math.Clamp(
                (viewport.OriginY * 16) + 8,
                16, map.Height * 16 - 8));
            maxX = Snap(Math.Clamp(
                (viewport.OriginX + viewport.Columns) * 16 - 8,
                minX, map.Width * 16 - 8));
            maxY = Snap(Math.Clamp(
                (viewport.OriginY + viewport.Rows) * 16 - 8,
                minY, map.Height * 16 - 8));
        }

        private static void GetOverworldFieldBounds(
            LiveWallpaperMap map,
            int scene,
            float? continuationPixelX,
            float? continuationPixelY,
            out int minX,
            out int minY,
            out int maxX,
            out int maxY)
        {
            const int fieldWidth = 160;
            const int fieldHeight = 128;
            float anchorX;
            float anchorY;
            if (continuationPixelX.HasValue && continuationPixelY.HasValue)
            {
                anchorX = continuationPixelX.Value;
                anchorY = continuationPixelY.Value;
            }
            else if (LiveWallpaperSceneSelection.TryGetTileOrigin(
                         scene, out var tileX, out var tileY))
            {
                anchorX = tileX * 16f + fieldWidth * 0.5f;
                anchorY = tileY * 16f + fieldHeight * 0.5f;
            }
            else
            {
                anchorX = fieldWidth * 0.5f;
                anchorY = fieldHeight * 0.5f;
            }

            var fieldX = (int)MathF.Floor(
                Math.Clamp(anchorX, 0f, map.Width * 16f - 1f) / fieldWidth) *
                fieldWidth;
            var fieldY = (int)MathF.Floor(
                Math.Clamp(anchorY, 0f, map.Height * 16f - 1f) / fieldHeight) *
                fieldHeight;
            minX = Snap(Math.Clamp(fieldX + 8, 8, map.Width * 16 - 8));
            minY = Snap(Math.Clamp(fieldY + 8, 8, map.Height * 16 - 8));
            maxX = Snap(Math.Clamp(
                fieldX + fieldWidth - 8, minX, map.Width * 16 - 8));
            maxY = Snap(Math.Clamp(
                fieldY + fieldHeight - 8, minY, map.Height * 16 - 8));
        }

        private static int Snap(int value) =>
            (int)MathF.Round(value / (float)GridStep) * GridStep;

        private static int PositiveHash(int first, int second, int salt)
        {
            unchecked
            {
                var value = first * 73856093 ^ second * 19349663 ^ salt * 83492791;
                return value == int.MinValue ? int.MaxValue : Math.Abs(value);
            }
        }
    }
}
