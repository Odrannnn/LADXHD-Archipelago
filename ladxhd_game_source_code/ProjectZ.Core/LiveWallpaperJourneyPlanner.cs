using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public enum LiveWallpaperJourneyAction
    {
        Walk,
        Interact,
        FeatherJump,
        RoosterFly
    }

    public readonly struct LiveWallpaperJourneyPoint
    {
        public LiveWallpaperJourneyPoint(float pixelX, float pixelY,
            LiveWallpaperJourneyAction action = LiveWallpaperJourneyAction.Walk)
        {
            PixelX = pixelX;
            PixelY = pixelY;
            Action = action;
        }

        public float PixelX { get; }
        public float PixelY { get; }
        public LiveWallpaperJourneyAction Action { get; }
    }

    public sealed class LiveWallpaperJourneyPlan
    {
        public LiveWallpaperJourneyPlan(
            LiveWallpaperJourneyPoint[] points,
            int interactionPointIndex = -1,
            int interactionActorIndex = -1,
            int roosterPickupPointIndex = -1,
            int roosterLandingPointIndex = -1)
        {
            Points = points ?? [];
            InteractionPointIndex = interactionPointIndex;
            InteractionActorIndex = interactionActorIndex;
            RoosterPickupPointIndex = roosterPickupPointIndex;
            RoosterLandingPointIndex = roosterLandingPointIndex;
        }

        public IReadOnlyList<LiveWallpaperJourneyPoint> Points { get; }
        public int InteractionPointIndex { get; }
        public int InteractionActorIndex { get; }
        public int RoosterPickupPointIndex { get; }
        public int RoosterLandingPointIndex { get; }
        public bool HasInteraction => InteractionPointIndex >= 0 && InteractionActorIndex >= 0;
        public bool HasRoosterFlight => RoosterPickupPointIndex >= 0 && RoosterLandingPointIndex >= 0;
    }

    /// <summary>
    /// Builds deterministic wallpaper journeys from installed overworld collision, doors, screen
    /// edges, and NPC bodies. It deliberately has no save, dialog, audio, or gameplay side effects.
    /// </summary>
    public static class LiveWallpaperJourneyPlanner
    {
        private const int GridStep = 8;
        private const int OffscreenRouteMargin = 64;
        private const float LinkBodyOffsetX = -4f;
        private const float LinkBodyOffsetY = -10f;
        private const float LinkBodyWidth = 8f;
        private const float LinkBodyHeight = 10f;

        private readonly struct Endpoint
        {
            public Endpoint(int x, int y, int side, bool isDoor)
            {
                X = x;
                Y = y;
                Side = side;
                IsDoor = isDoor;
            }

            public int X { get; }
            public int Y { get; }
            public int Side { get; }
            public bool IsDoor { get; }
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
            bool allowIslandLife)
        {
            if (map == null || viewport.Columns <= 0 || viewport.Rows <= 0)
                return new LiveWallpaperJourneyPlan([]);

            GetBounds(map, viewport,
                out var minX, out var minY, out var maxX, out var maxY);
            var pathMinX = Snap(Math.Max(8, minX - OffscreenRouteMargin));
            var pathMinY = Snap(Math.Max(16, minY - OffscreenRouteMargin));
            var pathMaxX = Snap(Math.Min(map.Width * 16 - 8, maxX + OffscreenRouteMargin));
            var pathMaxY = Snap(Math.Min(map.Height * 16 - 8, maxY + OffscreenRouteMargin));
            var endpoints = BuildEndpoints(
                map, minX, minY, maxX, maxY,
                pathMinX, pathMinY, pathMaxX, pathMaxY);
            var pairs = BuildPairs(endpoints, minX, minY, maxX, maxY);
            if (pairs.Count == 0)
                return new LiveWallpaperJourneyPlan([]);

            var pairOffset = PositiveHash(scene, variant, 17) % Math.Min(12, pairs.Count);
            List<Point> fallbackPath = null;
            for (var attempt = 0; attempt < pairs.Count; attempt++)
            {
                var pair = pairs[(pairOffset + attempt) % pairs.Count];
                var basePath = FindPath(
                    map, pair.Start.X, pair.Start.Y, pair.End.X, pair.End.Y,
                    pathMinX, pathMinY, pathMaxX, pathMaxY, includeHoles: true);
                if (basePath.Count < 2)
                {
                    var jumpPath = FindPath(
                        map, pair.Start.X, pair.Start.Y, pair.End.X, pair.End.Y,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, includeHoles: false);
                    if (jumpPath.Count >= 2 && HasValidJumpSpans(map, jumpPath))
                        return ToJumpPlan(map, jumpPath);
                    continue;
                }

                fallbackPath ??= basePath;
                var behavior = PositiveHash(scene, variant, 71);
                if (allowIslandLife && behavior % 5 == 0)
                    return AddRoosterFlight(
                        map, basePath, pathMinX, pathMinY, pathMaxX, pathMaxY);
                if (allowIslandLife && behavior % 3 == 0 &&
                    TryAddInteraction(
                        map, basePath, pair.Start, pair.End,
                        minX, minY, maxX, maxY,
                        pathMinX, pathMinY, pathMaxX, pathMaxY, behavior,
                        out var interactionPlan))
                    return interactionPlan;
                if (allowIslandLife && behavior % 3 == 0)
                    continue;

                return ToPlan(basePath);
            }
            return fallbackPath != null
                ? ToPlan(fallbackPath)
                : new LiveWallpaperJourneyPlan([]);
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
                return ToPlan(groundPath);
            var pickupGroundIndex = Math.Clamp(groundPath.Count / 3, 1, groundPath.Count - 3);
            var landingGroundIndex = Math.Clamp(
                groundPath.Count * 2 / 3, pickupGroundIndex + 2, groundPath.Count - 2);
            var pickup = groundPath[pickupGroundIndex];
            var landing = groundPath[landingGroundIndex];
            var airPath = FindPath(
                map, pickup.X, pickup.Y, landing.X, landing.Y,
                minX, minY, maxX, maxY, includeHoles: false);
            if (airPath.Count < 2)
                return ToPlan(groundPath);

            var points = new List<LiveWallpaperJourneyPoint>();
            for (var index = 0; index <= pickupGroundIndex; index++)
                points.Add(new LiveWallpaperJourneyPoint(
                    groundPath[index].X, groundPath[index].Y));
            var pickupPointIndex = points.Count - 1;
            for (var index = 1; index < airPath.Count; index++)
                points.Add(new LiveWallpaperJourneyPoint(
                    airPath[index].X, airPath[index].Y,
                    LiveWallpaperJourneyAction.RoosterFly));
            var landingPointIndex = points.Count - 1;
            for (var index = landingGroundIndex + 1; index < groundPath.Count; index++)
                points.Add(new LiveWallpaperJourneyPoint(
                    groundPath[index].X, groundPath[index].Y));
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
                var centerX = actor.BodyX + actor.BodyWidth / 2;
                var centerY = actor.BodyY + actor.BodyHeight / 2;
                if (centerX >= minX && centerX <= maxX && centerY >= minY && centerY <= maxY)
                    actors.Add(index);
            }
            if (actors.Count == 0)
                return false;

            var actorOffset = behavior % actors.Count;
            for (var actorAttempt = 0; actorAttempt < actors.Count; actorAttempt++)
            {
                var actorIndex = actors[(actorOffset + actorAttempt) % actors.Count];
                var approaches = GetActorApproaches(map.Actors[actorIndex]);
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
                        points.Add(new LiveWallpaperJourneyPoint(
                            basePath[index].X, basePath[index].Y));
                    for (var index = 1; index < detour.Count; index++)
                        points.Add(new LiveWallpaperJourneyPoint(
                            detour[index].X, detour[index].Y));
                    var interactionPoint = points.Count - 1;
                    points[interactionPoint] = new LiveWallpaperJourneyPoint(
                        approach.X, approach.Y, LiveWallpaperJourneyAction.Interact);
                    for (var index = detour.Count - 2; index >= 0; index--)
                        points.Add(new LiveWallpaperJourneyPoint(
                            detour[index].X, detour[index].Y));
                    for (var index = detourIndex + 1; index < basePath.Count; index++)
                        points.Add(new LiveWallpaperJourneyPoint(
                            basePath[index].X, basePath[index].Y));
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

        private static Point[] GetActorApproaches(LiveWallpaperMapActor actor)
        {
            var centerX = actor.BodyX + actor.BodyWidth / 2;
            var centerY = actor.BodyY + actor.BodyHeight / 2;
            return
            [
                new Point(actor.BodyX - 8, centerY + 5),
                new Point(actor.BodyX + actor.BodyWidth + 8, centerY + 5),
                new Point(centerX, actor.BodyY - 2),
                new Point(centerX, actor.BodyY + actor.BodyHeight + 12)
            ];
        }

        private static LiveWallpaperJourneyPlan ToPlan(List<Point> path)
        {
            var points = new LiveWallpaperJourneyPoint[path.Count];
            for (var index = 0; index < path.Count; index++)
                points[index] = new LiveWallpaperJourneyPoint(path[index].X, path[index].Y);
            return new LiveWallpaperJourneyPlan(points);
        }

        private static LiveWallpaperJourneyPlan ToJumpPlan(
            LiveWallpaperMap map, List<Point> path)
        {
            var points = new LiveWallpaperJourneyPoint[path.Count];
            for (var index = 0; index < path.Count; index++)
            {
                var point = path[index];
                var jumping = IsHolePoint(map, point) ||
                              index > 0 && IsHolePoint(map, path[index - 1]) ||
                              index + 1 < path.Count && IsHolePoint(map, path[index + 1]);
                points[index] = new LiveWallpaperJourneyPoint(
                    point.X, point.Y, jumping
                        ? LiveWallpaperJourneyAction.FeatherJump
                        : LiveWallpaperJourneyAction.Walk);
            }
            return new LiveWallpaperJourneyPlan(points);
        }

        private static bool HasValidJumpSpans(
            LiveWallpaperMap map, List<Point> path)
        {
            var consecutiveHolePoints = 0;
            foreach (var point in path)
            {
                if (IsHolePoint(map, point))
                {
                    consecutiveHolePoints++;
                    if (consecutiveHolePoints > 4)
                        return false;
                }
                else
                    consecutiveHolePoints = 0;
            }
            return true;
        }

        private static bool IsHolePoint(LiveWallpaperMap map, Point point) =>
            map.IntersectsHole(
                point.X + LinkBodyOffsetX,
                point.Y + LinkBodyOffsetY,
                LinkBodyWidth,
                LinkBodyHeight);

        private static List<Endpoint> BuildEndpoints(
            LiveWallpaperMap map,
            int minX,
            int minY,
            int maxX,
            int maxY,
            int pathMinX,
            int pathMinY,
            int pathMaxX,
            int pathMaxY)
        {
            var endpoints = new List<Endpoint>();
            AddEdgeRuns(map, endpoints, side: 0, minX, minY, maxX, maxY);
            AddEdgeRuns(map, endpoints, side: 1, minX, minY, maxX, maxY);
            AddEdgeRuns(map, endpoints, side: 2, minX, minY, maxX, maxY);
            AddEdgeRuns(map, endpoints, side: 3, minX, minY, maxX, maxY);
            foreach (var portal in map.Portals)
            {
                var x = Snap((int)MathF.Round(portal.LinkTargetX));
                var y = Snap((int)MathF.Round(portal.LinkTargetY));
                if (x < pathMinX || x > pathMaxX ||
                    y < pathMinY || y > pathMaxY ||
                    !IsWalkable(map, x, y, true))
                    continue;
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
                var walkable = IsWalkable(map, x, y, true);
                if (walkable && runStart == int.MinValue)
                    runStart = value;
                if (walkable)
                    previous = value;
                if ((!walkable || value + GridStep > end) && runStart != int.MinValue)
                {
                    var middle = Snap((runStart + previous) / 2);
                    endpoints.Add(vertical
                        ? new Endpoint(fixedValue, middle, side, isDoor: false)
                        : new Endpoint(middle, fixedValue, side, isDoor: false));
                    runStart = int.MinValue;
                }
            }
        }

        private static List<Pair> BuildPairs(
            List<Endpoint> endpoints, int minX, int minY, int maxX, int maxY)
        {
            var pairs = new List<Pair>();
            var minimumDistance = Math.Min(maxX - minX, maxY - minY) * 0.55f;
            for (var first = 0; first < endpoints.Count; first++)
            {
                for (var second = first + 1; second < endpoints.Count; second++)
                {
                    var start = endpoints[first];
                    var end = endpoints[second];
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
                    pairs.Add(new Pair(start, end, score));
                    pairs.Add(new Pair(end, start, score - 1f));
                }
            }
            pairs.Sort((left, right) => right.Score.CompareTo(left.Score));
            return pairs;
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
            bool includeHoles)
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
            ReadOnlySpan<int> offsets = [-1, 0, 1, 0, -1];
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == endIndex)
                    break;
                var currentX = current % columns;
                var currentY = current / columns;
                for (var direction = 0; direction < 4; direction++)
                {
                    var nextX = currentX + offsets[direction];
                    var nextY = currentY + offsets[direction + 1];
                    if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows)
                        continue;
                    var next = nextY * columns + nextX;
                    var pixelX = minX + nextX * GridStep;
                    var pixelY = minY + nextY * GridStep;
                    if (next != endIndex && !IsWalkable(map, pixelX, pixelY, includeHoles))
                        continue;
                    var nextCost = cost[current] + 1;
                    if (nextCost >= cost[next])
                        continue;
                    cost[next] = nextCost;
                    previous[next] = current;
                    var heuristic = Math.Abs(endX - pixelX) / GridStep +
                                    Math.Abs(endY - pixelY) / GridStep;
                    queue.Enqueue(next, nextCost + heuristic);
                }
            }
            if (startIndex != endIndex && previous[endIndex] < 0)
                return [];

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
            LiveWallpaperMap map, float entityX, float entityY, bool includeHoles) =>
            !map.IntersectsCollision(
                entityX + LinkBodyOffsetX, entityY + LinkBodyOffsetY,
                LinkBodyWidth, LinkBodyHeight, includeHoles) &&
            !map.IntersectsActor(
                entityX + LinkBodyOffsetX, entityY + LinkBodyOffsetY,
                LinkBodyWidth, LinkBodyHeight);

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
