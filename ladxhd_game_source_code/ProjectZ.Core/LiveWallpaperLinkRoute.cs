using System;

namespace ProjectZ
{
    public enum LiveWallpaperLinkRouteAction
    {
        Stand,
        Walk,
        FeatherJump,
        Interact,
        Attack,
        RoosterFly
    }

    public readonly struct LiveWallpaperLinkRouteState
    {
        public LiveWallpaperLinkRouteState(
            float mapX,
            float mapY,
            int direction,
            float jumpHeight,
            LiveWallpaperLinkRouteAction action)
        {
            MapX = mapX;
            MapY = mapY;
            Direction = Math.Clamp(direction, 0, 3);
            JumpHeight = Math.Clamp(jumpHeight, 0f, 1f);
            Action = action;
        }

        public float MapX { get; }
        public float MapY { get; }
        public int Direction { get; }
        public float JumpHeight { get; }
        public LiveWallpaperLinkRouteAction Action { get; }
    }

    public static class LiveWallpaperLinkRoute
    {
        private readonly struct Segment
        {
            public Segment(
                float startX, float startY, float endX, float endY,
                LiveWallpaperLinkRouteAction action = LiveWallpaperLinkRouteAction.Walk)
            {
                StartX = startX;
                StartY = startY;
                EndX = endX;
                EndY = endY;
                Action = action;
            }

            public float StartX { get; }
            public float StartY { get; }
            public float EndX { get; }
            public float EndY { get; }
            public LiveWallpaperLinkRouteAction Action { get; }
        }

        private static readonly Segment[] MabeRoute =
            [new Segment(23.5f, 77.5f, 29.5f, 77.5f)];
        private static readonly Segment[] ToronboRoute =
            [new Segment(15.5f, 118f, 19.5f, 118f)];
        private static readonly Segment[] ForestRoute =
        [
            new Segment(14.5f, 38f, 17.5f, 38f),
            new Segment(17.5f, 38f, 17.5f, 37f),
            new Segment(17.5f, 37f, 19.5f, 37f,
                LiveWallpaperLinkRouteAction.FeatherJump)
        ];
        private static readonly Segment[] CastleRoute =
            [new Segment(95.5f, 48f, 97.5f, 48f)];
        private static readonly Segment[] AnimalVillageRoute =
            [new Segment(130f, 105f, 137.5f, 105f)];
        private static readonly Segment[] EggRoute =
            [new Segment(66.5f, 16.5f, 66.5f, 18.5f)];

        public static LiveWallpaperLinkRouteState Resolve(
            int scene, float journey, bool walking)
        {
            var segments = GetSegments(scene);
            var clampedJourney = Math.Clamp(journey, 0f, 1f);
            var reversing = clampedJourney > 0.5f;
            var routeProgress = reversing
                ? (1f - clampedJourney) * 2f
                : clampedJourney * 2f;
            var totalLength = GetLengthTiles(segments);
            var remainingDistance = routeProgress * totalLength;
            var index = 0;
            while (index < segments.Length - 1)
            {
                var segmentLength = GetLengthTiles(segments[index]);
                if (remainingDistance <= segmentLength)
                    break;
                remainingDistance -= segmentLength;
                index++;
            }
            var segment = segments[index];
            var localProgress = Math.Clamp(
                remainingDistance / Math.Max(0.001f, GetLengthTiles(segment)), 0f, 1f);
            var mapX = Lerp(segment.StartX, segment.EndX, localProgress);
            var mapY = Lerp(segment.StartY, segment.EndY, localProgress);
            var direction = ResolveDirection(
                segment.EndX - segment.StartX, segment.EndY - segment.StartY, reversing);
            var action = walking ? segment.Action : LiveWallpaperLinkRouteAction.Stand;
            var jumpHeight = action == LiveWallpaperLinkRouteAction.FeatherJump
                ? MathF.Sin(localProgress * MathF.PI)
                : 0f;
            return new LiveWallpaperLinkRouteState(
                mapX, mapY, direction, jumpHeight, action);
        }

        public static float GetPathLengthPixels(int scene) =>
            GetLengthTiles(GetSegments(scene)) * 16f;

        private static Segment[] GetSegments(int scene) => scene switch
        {
            2 => ToronboRoute,
            3 => ForestRoute,
            5 => CastleRoute,
            6 => AnimalVillageRoute,
            7 => EggRoute,
            _ => MabeRoute
        };

        private static float GetLengthTiles(Segment[] segments)
        {
            var length = 0f;
            foreach (var segment in segments)
                length += GetLengthTiles(segment);
            return length;
        }

        private static float GetLengthTiles(Segment segment)
        {
            var deltaX = segment.EndX - segment.StartX;
            var deltaY = segment.EndY - segment.StartY;
            return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        private static int ResolveDirection(float deltaX, float deltaY, bool reversing)
        {
            if (reversing)
            {
                deltaX = -deltaX;
                deltaY = -deltaY;
            }
            if (MathF.Abs(deltaX) >= MathF.Abs(deltaY))
                return deltaX < 0f ? 0 : 2;
            return deltaY < 0f ? 1 : 3;
        }

        private static float Lerp(float start, float end, float amount) =>
            start + (end - start) * amount;
    }
}
