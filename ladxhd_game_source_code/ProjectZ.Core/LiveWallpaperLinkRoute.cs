using System;

namespace ProjectZ
{
    public enum LiveWallpaperLinkRouteAction
    {
        Stand,
        Walk,
        FeatherJump
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
            [new Segment(23.5f, 81.5f, 29.5f, 81.5f)];
        private static readonly Segment[] ToronboRoute =
            [new Segment(10.5f, 115.5f, 18.5f, 115.5f)];
        private static readonly Segment[] ForestRoute =
        [
            new Segment(14.5f, 36.5f, 17f, 36.5f),
            new Segment(17f, 36.5f, 19f, 36.5f,
                LiveWallpaperLinkRouteAction.FeatherJump)
        ];
        private static readonly Segment[] CastleRoute =
            [new Segment(94.5f, 46.5f, 100.5f, 46.5f)];
        private static readonly Segment[] AnimalVillageRoute =
            [new Segment(129.5f, 104.5f, 138.5f, 104.5f)];
        private static readonly Segment[] EggRoute =
            [new Segment(66.5f, 22.5f, 66.5f, 12.5f)];

        public static LiveWallpaperLinkRouteState Resolve(
            int scene, float journey, bool walking)
        {
            var segments = scene switch
            {
                2 => ToronboRoute,
                3 => ForestRoute,
                5 => CastleRoute,
                6 => AnimalVillageRoute,
                7 => EggRoute,
                _ => MabeRoute
            };
            var clampedJourney = Math.Clamp(journey, 0f, 1f);
            var reversing = clampedJourney > 0.5f;
            var routeProgress = reversing
                ? (1f - clampedJourney) * 2f
                : clampedJourney * 2f;
            var scaledProgress = routeProgress * segments.Length;
            var index = Math.Min(segments.Length - 1, (int)scaledProgress);
            var localProgress = Math.Clamp(scaledProgress - index, 0f, 1f);
            var segment = segments[index];
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

        private static int ResolveDirection(float deltaX, float deltaY, bool reversing)
        {
            if (reversing)
            {
                deltaX = -deltaX;
                deltaY = -deltaY;
            }
            if (MathF.Abs(deltaX) >= MathF.Abs(deltaY))
                return deltaX < 0f ? 2 : 3;
            return deltaY < 0f ? 1 : 0;
        }

        private static float Lerp(float start, float end, float amount) =>
            start + (end - start) * amount;
    }
}
