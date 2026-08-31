using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ
{
    public enum LiveWallpaperActorAction
    {
        Idle,
        Walk,
        Fly,
        Hidden
    }

    public readonly struct LiveWallpaperActorState
    {
        public LiveWallpaperActorState(
            float entityX, float entityY, float height,
            int direction, LiveWallpaperActorAction action)
        {
            EntityX = entityX;
            EntityY = entityY;
            Height = Math.Max(0f, height);
            Direction = Math.Clamp(direction, 0, 3);
            Action = action;
        }

        public float EntityX { get; }
        public float EntityY { get; }
        public float Height { get; }
        public int Direction { get; }
        public LiveWallpaperActorAction Action { get; }
        public bool Visible => Action != LiveWallpaperActorAction.Hidden;
        public bool BlocksMovement =>
            Visible && Action != LiveWallpaperActorAction.Fly;
    }

    /// <summary>
    /// Silent wallpaper execution of the movement portions of ObjBird, ObjDog,
    /// ObjButterfly, ObjFrog, ObjMouse, ObjBowWowSmall, and the tethered
    /// ObjBowWow. It uses their original timers, speed ranges, jump velocities,
    /// gravity, body rectangles, and chain radius.
    /// </summary>
    public static class LiveWallpaperActorSimulation
    {
        private const float OwlLeaveMilliseconds = 2000f;
        private static readonly CubicBezier OwlLeavingCurve = new(
            100, new Vector2(0.25f, 0.04f), new Vector2(0.35f, 0.11f));

        public static float ResolveFairyHeight(
            long elapsedMilliseconds, bool animated) =>
            animated
                ? 12f + MathF.Sin(elapsedMilliseconds / 1100f * MathF.PI) * 4f
                : 12f;

        public static bool IsInteraction(
            LiveWallpaperSimulatedLinkState? link, int actorIndex) =>
            link.HasValue &&
            link.Value.Action == LiveWallpaperLinkRouteAction.Interact &&
            link.Value.InteractionActorIndex == actorIndex;

        public static bool ShouldRaccoonLaugh(
            LiveWallpaperMapActor actor,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (!link.HasValue || actor.Kind != LiveWallpaperMapActorKind.Raccoon)
                return false;
            var linkX = link.Value.MapX * 16f - 4f;
            var linkY = link.Value.MapY * 16f - 10f;
            return Intersects(
                linkX, linkY, 8f, 10f,
                actor.PixelX - 64f, actor.PixelY - 48f, 64f, 32f);
        }

        public static int ResolveGrandmotherDirection(
            LiveWallpaperMapActor actor,
            int currentDirection,
            LiveWallpaperSimulatedLinkState? link)
        {
            currentDirection = currentDirection < 0 ? -1 : 1;
            if (!link.HasValue ||
                actor.Kind != LiveWallpaperMapActorKind.Grandmother)
                return currentDirection;
            var entityX = actor.PixelX + 8f;
            var entityY = actor.PixelY + 16f;
            var linkX = link.Value.MapX * 16f - 4f;
            var linkY = link.Value.MapY * 16f - 10f;
            if (Intersects(
                    linkX, linkY, 8f, 10f,
                    entityX - 16f * currentDirection - 8f,
                    entityY - 32f, 16f, 48f))
                return entityX < link.Value.MapX * 16f ? 1 : -1;
            return currentDirection;
        }

        public static Vector2 ResolveInteractionApproach(
            LiveWallpaperMapActor actor,
            LiveWallpaperActorState state,
            Vector2 fallback)
        {
            if (!Session.IsMobile(actor.Kind) ||
                !LiveWallpaperMap.TryGetLiveActorBody(actor, state, out var body))
                return fallback;
            var bodyX = body.X;
            var bodyY = body.Y;
            var centerX = bodyX + actor.BodyWidth / 2f;
            var centerY = bodyY + actor.BodyHeight / 2f;
            // The planner already uses live positions. Select an approach
            // relative to this body, not its original spawn across the room.
            var distances = new[]
            {
                Vector2.DistanceSquared(fallback,
                    new Vector2(bodyX - 8f, centerY + 5f)),
                Vector2.DistanceSquared(fallback,
                    new Vector2(bodyX + actor.BodyWidth + 8f, centerY + 5f)),
                Vector2.DistanceSquared(fallback,
                    new Vector2(centerX, bodyY - 2f)),
                Vector2.DistanceSquared(fallback,
                    new Vector2(centerX, bodyY + actor.BodyHeight + 12f))
            };
            var side = 0;
            for (var index = 1; index < distances.Length; index++)
            {
                if (distances[index] < distances[side])
                    side = index;
            }
            return side switch
            {
                0 => new Vector2(bodyX - 8f, centerY + 5f),
                1 => new Vector2(
                    bodyX + actor.BodyWidth + 8f, centerY + 5f),
                2 => new Vector2(centerX, bodyY - 2f),
                _ => new Vector2(
                    centerX, bodyY + actor.BodyHeight + 12f)
            };
        }

        private static bool Intersects(
            float x, float y, float width, float height,
            float otherX, float otherY, float otherWidth, float otherHeight) =>
            x < otherX + otherWidth && x + width > otherX &&
            y < otherY + otherHeight && y + height > otherY;

        public sealed class Session
        {
            private sealed class Runtime
            {
                public bool Initialized;
                public long LastElapsed;
                public float X;
                public float Y;
                public float Z;
                public float VelocityX;
                public float VelocityY;
                public float VelocityZ;
                public float Timer;
                public float SegmentDuration;
                public float DirectionChangeTimer;
                public float OriginX;
                public float OriginY;
                public float Rotation;
                public float DirectionChange;
                public float CurrentSpeed;
                public float LastSpeed;
                public float SpeedGoal;
                public float StartDistance;
                public int Direction;
                public uint RandomState;
                public LiveWallpaperActorAction Action;
                public bool Grounded;
            }

            private readonly Dictionary<int, Runtime> _runtime = new();
            private LiveWallpaperMap _map;

            public LiveWallpaperActorState Resolve(
                LiveWallpaperMap map,
                int actorIndex,
                long elapsedMilliseconds,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (map == null || actorIndex < 0 || actorIndex >= map.Actors.Count)
                    return default;
                if (!ReferenceEquals(_map, map))
                {
                    _map = map;
                    _runtime.Clear();
                }
                var actor = map.Actors[actorIndex];
                if (!NeedsRuntime(actor.Kind))
                    return AtSpawn(actor);
                if (!_runtime.TryGetValue(actorIndex, out var runtime))
                {
                    runtime = new Runtime();
                    _runtime.Add(actorIndex, runtime);
                }
                if (!runtime.Initialized || elapsedMilliseconds < runtime.LastElapsed)
                    Initialize(runtime, actor, actorIndex, elapsedMilliseconds);

                var remaining = Math.Clamp(
                    elapsedMilliseconds - runtime.LastElapsed, 0L, 250L);
                runtime.LastElapsed = elapsedMilliseconds;
                while (remaining > 0)
                {
                    var step = Math.Min(1000f / 60f, remaining);
                    Update(runtime, map, actor, actorIndex, step, link);
                    remaining -= (long)Math.Ceiling(step);
                }
                return new LiveWallpaperActorState(
                    runtime.X, runtime.Y, runtime.Z,
                    runtime.Direction, runtime.Action);
            }

            internal static bool IsMobile(LiveWallpaperMapActorKind kind) =>
                kind is LiveWallpaperMapActorKind.Dog or
                    LiveWallpaperMapActorKind.Butterfly or
                    LiveWallpaperMapActorKind.Bird or
                    LiveWallpaperMapActorKind.BowWow or
                    LiveWallpaperMapActorKind.Frog or
                    LiveWallpaperMapActorKind.Mouse or
                    LiveWallpaperMapActorKind.BowWowSmall or
                    LiveWallpaperMapActorKind.LetterBird;

            private static bool NeedsRuntime(LiveWallpaperMapActorKind kind) =>
                IsMobile(kind) ||
                kind is LiveWallpaperMapActorKind.Owl or
                    LiveWallpaperMapActorKind.ChickenDude or
                    LiveWallpaperMapActorKind.Hippo or
                    LiveWallpaperMapActorKind.LetterBoy or
                    LiveWallpaperMapActorKind.LetterGirl;

            private static LiveWallpaperActorState AtSpawn(
                LiveWallpaperMapActor actor)
            {
                var position = GetSpawn(actor);
                return new LiveWallpaperActorState(
                    position.X, position.Y, position.Z, 0,
                    LiveWallpaperActorAction.Idle);
            }

            private static void Initialize(
                Runtime runtime,
                LiveWallpaperMapActor actor,
                int actorIndex,
                long elapsed)
            {
                var spawn = GetSpawn(actor);
                runtime.Initialized = true;
                runtime.LastElapsed = elapsed;
                runtime.X = spawn.X;
                runtime.Y = spawn.Y;
                runtime.Z = spawn.Z;
                runtime.OriginX = actor.Kind == LiveWallpaperMapActorKind.BowWow
                    ? actor.PixelX + 8f
                    : spawn.X;
                runtime.OriginY = actor.Kind == LiveWallpaperMapActorKind.BowWow
                    ? actor.PixelY + 8f
                    : spawn.Y;
                runtime.RandomState = (uint)(actorIndex + 1) * 747796405u +
                                      2891336453u;
                runtime.Direction = 0;
                runtime.Grounded = true;
                runtime.DirectionChangeTimer = 250f;
                if (actor.Kind == LiveWallpaperMapActorKind.Owl)
                {
                    runtime.Action = LiveWallpaperActorAction.Idle;
                    runtime.Timer = 0f;
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.ChickenDude)
                {
                    runtime.Action = LiveWallpaperActorAction.Idle;
                    runtime.Timer = 250f;
                    return;
                }
                if (actor.Kind is LiveWallpaperMapActorKind.Hippo or
                    LiveWallpaperMapActorKind.LetterBoy or
                    LiveWallpaperMapActorKind.LetterGirl)
                {
                    runtime.Action = LiveWallpaperActorAction.Idle;
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.Frog)
                {
                    runtime.Direction = Next(runtime, 0, 4);
                    runtime.Action = LiveWallpaperActorAction.Idle;
                    runtime.Timer = Next(runtime, 125, 1000);
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.Butterfly)
                {
                    runtime.StartDistance = Next(runtime, 25, 100);
                    runtime.Rotation = Next(runtime, 0, 100) / 100f *
                                       MathF.PI * 2f;
                    runtime.CurrentSpeed = Next(runtime, 25, 45) / 100f;
                    runtime.LastSpeed = runtime.CurrentSpeed;
                    runtime.SpeedGoal = Next(runtime, 25, 45) / 100f;
                    BeginButterflySegment(runtime);
                    runtime.Action = LiveWallpaperActorAction.Walk;
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.LetterBird)
                {
                    BeginIdle(runtime, actor.Kind);
                    return;
                }
                var walking = Next(runtime, 0, 10) >= 5;
                if (actor.Kind == LiveWallpaperMapActorKind.BowWow)
                    walking = Next(runtime, 0, 100) >= 50;
                if (walking)
                    BeginWalking(runtime, actor.Kind);
                else
                    BeginIdle(runtime, actor.Kind);
            }

            public static Vector3 GetSpawn(LiveWallpaperMapActor actor) =>
                actor.Kind switch
                {
                    LiveWallpaperMapActorKind.Butterfly =>
                        new Vector3(actor.PixelX + 8, actor.PixelY + 23, 15),
                    LiveWallpaperMapActorKind.BowWow =>
                        new Vector3(actor.PixelX, actor.PixelY + 16, 0),
                    LiveWallpaperMapActorKind.Mouse =>
                        new Vector3(actor.PixelX + 8, actor.PixelY + 12, 0),
                    _ => new Vector3(actor.PixelX + 8, actor.PixelY + 16, 0)
                };

            private void Update(
                Runtime runtime,
                LiveWallpaperMap map,
                LiveWallpaperMapActor actor,
                int actorIndex,
                float deltaMilliseconds,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (actor.Kind == LiveWallpaperMapActorKind.Butterfly)
                {
                    UpdateButterfly(runtime, deltaMilliseconds);
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.Owl)
                {
                    UpdateOwl(runtime, actor, deltaMilliseconds, link);
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.Frog)
                {
                    UpdateFrog(runtime, map, actor, actorIndex,
                        deltaMilliseconds, link);
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.ChickenDude)
                {
                    UpdateChickenDude(runtime, deltaMilliseconds);
                    return;
                }
                if (actor.Kind == LiveWallpaperMapActorKind.Hippo)
                {
                    UpdateHippo(runtime, actor, link);
                    return;
                }
                if (actor.Kind is LiveWallpaperMapActorKind.LetterBoy or
                    LiveWallpaperMapActorKind.LetterGirl)
                {
                    UpdateLetterChild(
                        runtime, actor, deltaMilliseconds, link);
                    return;
                }
                if (actor.Kind is LiveWallpaperMapActorKind.Mouse or
                    LiveWallpaperMapActorKind.BowWowSmall or
                    LiveWallpaperMapActorKind.LetterBird)
                {
                    UpdateGroundHopper(runtime, map, actor, actorIndex,
                        deltaMilliseconds, link);
                    return;
                }

                runtime.Timer -= deltaMilliseconds;
                runtime.DirectionChangeTimer = Math.Max(
                    0f, runtime.DirectionChangeTimer - deltaMilliseconds);
                if (runtime.Timer <= 0)
                {
                    if (runtime.Action == LiveWallpaperActorAction.Idle)
                        BeginWalking(runtime, actor.Kind);
                    else
                        BeginIdle(runtime, actor.Kind);
                }
                if (runtime.Action != LiveWallpaperActorAction.Walk)
                    return;

                var frameScale = deltaMilliseconds / (1000f / 60f);
                Move(runtime, map, actor, actorIndex, link, frameScale);
                if (runtime.Grounded)
                {
                    runtime.Grounded = false;
                    runtime.VelocityZ = actor.Kind switch
                    {
                        LiveWallpaperMapActorKind.Bird => 0.65f,
                        LiveWallpaperMapActorKind.Dog or
                        LiveWallpaperMapActorKind.BowWowSmall => 0.85f,
                        LiveWallpaperMapActorKind.Mouse => 1f,
                        _ => 1.5f
                    };
                }
                runtime.Z += runtime.VelocityZ * frameScale;
                runtime.VelocityZ += actor.Kind == LiveWallpaperMapActorKind.BowWow
                    ? -0.175f * frameScale
                    : actor.Kind == LiveWallpaperMapActorKind.Bird
                        ? -0.1f * frameScale
                        : -0.15f * frameScale;
                if (runtime.Z <= 0)
                {
                    runtime.Z = 0;
                    runtime.VelocityZ = 0;
                    runtime.Grounded = true;
                }
            }

            private static void UpdateOwl(
                Runtime runtime,
                LiveWallpaperMapActor actor,
                float deltaMilliseconds,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (runtime.Action == LiveWallpaperActorAction.Hidden)
                    return;
                if (runtime.Action == LiveWallpaperActorAction.Idle)
                {
                    if (!link.HasValue || link.Value.Height > 0f)
                        return;
                    var linkX = link.Value.MapX * 16f - 4f;
                    var linkY = link.Value.MapY * 16f - 10f;
                    var triggered = actor.OwlMode == 2
                        ? Vector2.Distance(
                            new Vector2(link.Value.MapX * 16f,
                                link.Value.MapY * 16f),
                            new Vector2(runtime.X, runtime.Y)) < 64f
                        : Intersects(
                            linkX, linkY, 8f, 10f,
                            actor.TriggerX, actor.TriggerY,
                            actor.TriggerWidth, actor.TriggerHeight);
                    if (!triggered)
                        return;
                    runtime.Action = LiveWallpaperActorAction.Fly;
                    runtime.Timer = 0f;
                    runtime.Direction = actor.OwlHoverMode || actor.OwlMode == 2
                        ? 0
                        : 1;
                }

                runtime.Timer = Math.Min(
                    OwlLeaveMilliseconds, runtime.Timer + deltaMilliseconds);
                var time = runtime.Timer / OwlLeaveMilliseconds;
                var progress = OwlLeavingCurve.EvaluateX(time);
                var leaveX = runtime.OriginX +
                             (actor.OwlHoverMode || actor.OwlMode == 2
                                 ? 0f
                                 : 64f);
                var leaveY = runtime.OriginY - 64f;
                runtime.X = MathHelper.Lerp(runtime.OriginX, leaveX, progress);
                runtime.Y = MathHelper.Lerp(runtime.OriginY, leaveY, progress);
                runtime.Z = MathHelper.Lerp(0f, 64f, progress);
                if (runtime.Timer >= OwlLeaveMilliseconds)
                    runtime.Action = LiveWallpaperActorAction.Hidden;
            }

            private static void BeginIdle(
                Runtime runtime, LiveWallpaperMapActorKind kind)
            {
                runtime.Action = LiveWallpaperActorAction.Idle;
                runtime.VelocityX = 0;
                runtime.VelocityY = 0;
                // ObjMouse waits for its three 517 ms stand frames to finish;
                // ObjBowWowSmall uses AiTriggerRandomTime(500, 1500).
                runtime.Timer = kind == LiveWallpaperMapActorKind.Mouse
                    ? 1551f
                    : Next(runtime, 500, 1500);
            }

            private static void BeginWalking(
                Runtime runtime, LiveWallpaperMapActorKind kind)
            {
                runtime.Action = LiveWallpaperActorAction.Walk;
                var rotation = Next(runtime, 0, 628) / 100f;
                var speed = kind switch
                {
                    LiveWallpaperMapActorKind.Bird => Next(runtime, 25, 40) / 100f,
                    LiveWallpaperMapActorKind.Dog => Next(runtime, 40, 55) / 100f,
                    LiveWallpaperMapActorKind.Mouse or
                    LiveWallpaperMapActorKind.BowWowSmall =>
                        Next(runtime, 25, 40) / 50f,
                    LiveWallpaperMapActorKind.LetterBird =>
                        Next(runtime, 35, 55) / 100f,
                    _ => Next(runtime, 25, 40) / 25f
                };
                runtime.VelocityX = MathF.Sin(rotation) * speed;
                runtime.VelocityY = MathF.Cos(rotation) * speed;
                runtime.Direction = kind == LiveWallpaperMapActorKind.BowWow
                    ? DirectionTo(runtime.VelocityX, runtime.VelocityY)
                    : runtime.VelocityX < 0 ? 0 : 1;
                runtime.Timer = kind == LiveWallpaperMapActorKind.BowWow
                    ? Next(runtime, 500, 1000)
                    : Next(runtime, 750, 1500);
                runtime.DirectionChangeTimer = 250f;
            }

            private void Move(
                Runtime runtime,
                LiveWallpaperMap map,
                LiveWallpaperMapActor actor,
                int actorIndex,
                LiveWallpaperSimulatedLinkState? link,
                float frameScale)
            {
                var nextX = runtime.X + runtime.VelocityX * frameScale;
                if (!Blocked(map, actor, actorIndex, nextX, runtime.Y, link))
                    runtime.X = nextX;
                else if (runtime.DirectionChangeTimer <= 0)
                {
                    runtime.VelocityX = -runtime.VelocityX *
                        (actor.Kind is LiveWallpaperMapActorKind.BowWow or
                            LiveWallpaperMapActorKind.Mouse or
                            LiveWallpaperMapActorKind.BowWowSmall or
                            LiveWallpaperMapActorKind.LetterBird ? 0.5f : 1f);
                    runtime.DirectionChangeTimer = 250f;
                }
                var nextY = runtime.Y + runtime.VelocityY * frameScale;
                if (!Blocked(map, actor, actorIndex, runtime.X, nextY, link))
                    runtime.Y = nextY;
                else
                    runtime.VelocityY = -runtime.VelocityY *
                        (actor.Kind is LiveWallpaperMapActorKind.Bird or
                            LiveWallpaperMapActorKind.BowWow or
                            LiveWallpaperMapActorKind.Mouse or
                            LiveWallpaperMapActorKind.BowWowSmall or
                            LiveWallpaperMapActorKind.LetterBird ? 0.5f : 1f);

                if (actor.Kind == LiveWallpaperMapActorKind.BowWow)
                {
                    var fromOrigin = new Vector2(
                        runtime.X - runtime.OriginX,
                        runtime.Y - 4f - runtime.OriginY);
                    if (fromOrigin.LengthSquared() > 40f * 40f)
                    {
                        fromOrigin.Normalize();
                        runtime.X = runtime.OriginX + fromOrigin.X * 40f;
                        runtime.Y = runtime.OriginY + fromOrigin.Y * 40f + 4f;
                        runtime.VelocityX = 0;
                        runtime.VelocityY = 0;
                    }
                    runtime.Direction = DirectionTo(
                        runtime.VelocityX, runtime.VelocityY);
                }
                else
                    runtime.Direction = runtime.VelocityX < 0 ? 0 : 1;
            }

            private bool Blocked(
                LiveWallpaperMap map,
                LiveWallpaperMapActor actor,
                int actorIndex,
                float entityX,
                float entityY,
                LiveWallpaperSimulatedLinkState? link)
            {
                var body = actor.Kind switch
                {
                    LiveWallpaperMapActorKind.Dog => (-6f, -8f, 12f, 8f),
                    LiveWallpaperMapActorKind.Bird => (-6f, -8f, 12f, 8f),
                    LiveWallpaperMapActorKind.Frog => (-6f, -8f, 12f, 8f),
                    LiveWallpaperMapActorKind.LetterBird =>
                        (-6f, -8f, 12f, 8f),
                    LiveWallpaperMapActorKind.Mouse or
                    LiveWallpaperMapActorKind.BowWowSmall =>
                        (-5f, -8f, 10f, 8f),
                    _ => (-7f, -10f, 14f, 10f)
                };
                var x = entityX + body.Item1;
                var y = entityY + body.Item2;
                if (map.IntersectsVoid(x, y, body.Item3, body.Item4) ||
                    map.IntersectsCollision(
                        x, y, body.Item3, body.Item4, includeHoles: true) ||
                    map.IntersectsNpcWall(x, y, body.Item3, body.Item4) ||
                    IntersectsActorAtLivePosition(
                        map, x, y, body.Item3, body.Item4, actorIndex))
                    return true;
                if (link.HasValue)
                {
                    var linkX = link.Value.MapX * 16f - 4f;
                    var linkY = link.Value.MapY * 16f - 10f;
                    if (x < linkX + 8f && x + body.Item3 > linkX &&
                        y < linkY + 10f && y + body.Item4 > linkY)
                        return true;
                }
                // ObjBird and ObjDog include CollisionTypes.Field.
                if (actor.Kind is LiveWallpaperMapActorKind.Bird or
                    LiveWallpaperMapActorKind.Dog or
                    LiveWallpaperMapActorKind.Frog or
                    LiveWallpaperMapActorKind.Mouse or
                    LiveWallpaperMapActorKind.BowWowSmall)
                {
                    var fieldLeft = actor.PixelX / 160 * 160f;
                    var fieldTop = actor.PixelY / 128 * 128f;
                    if (x < fieldLeft || y < fieldTop ||
                        x + body.Item3 > fieldLeft + 160f ||
                        y + body.Item4 > fieldTop + 128f)
                        return true;
                }
                return false;
            }

            private void UpdateFrog(
                Runtime runtime,
                LiveWallpaperMap map,
                LiveWallpaperMapActor actor,
                int actorIndex,
                float deltaMilliseconds,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (runtime.Action == LiveWallpaperActorAction.Idle)
                {
                    runtime.Timer -= deltaMilliseconds;
                    if (runtime.Timer > 0)
                        return;
                    var rotation = Next(runtime, 0, 628) / 100f;
                    var speed = Next(runtime, 25, 40) / 50f;
                    runtime.VelocityX = MathF.Sin(rotation) * speed * 1.5f;
                    runtime.VelocityY = MathF.Cos(rotation) * speed * 1.5f;
                    runtime.VelocityZ = 1.75f;
                    runtime.Direction = DirectionTo(
                        runtime.VelocityX, runtime.VelocityY);
                    runtime.Action = LiveWallpaperActorAction.Walk;
                    runtime.Grounded = false;
                }

                var frameScale = deltaMilliseconds / (1000f / 60f);
                MoveFrog(runtime, map, actor, actorIndex, link, frameScale);
                runtime.VelocityZ += -0.15f * frameScale;
                runtime.Z += runtime.VelocityZ * frameScale;
                runtime.VelocityX *= MathF.Pow(0.99f, frameScale);
                runtime.VelocityY *= MathF.Pow(0.99f, frameScale);
                if (runtime.Z > 0)
                    return;
                runtime.Z = 0;
                runtime.VelocityX = 0;
                runtime.VelocityY = 0;
                runtime.VelocityZ = 0;
                runtime.Grounded = true;
                runtime.Action = LiveWallpaperActorAction.Idle;
                runtime.Timer = Next(runtime, 750, 1500);
            }

            private static void UpdateChickenDude(
                Runtime runtime, float deltaMilliseconds)
            {
                runtime.Timer -= deltaMilliseconds;
                if (runtime.Timer > 0)
                    return;
                if (runtime.Action == LiveWallpaperActorAction.Idle)
                {
                    // ObjChickenDude flips its powder direction one time in three.
                    if (Next(runtime, 0, 3) == 0)
                        runtime.Direction = runtime.Direction == 0 ? 1 : 0;
                    runtime.Action = LiveWallpaperActorAction.Walk;
                    runtime.Timer = 850f;
                }
                else
                {
                    runtime.Action = LiveWallpaperActorAction.Idle;
                    runtime.Timer = 250f;
                }
            }

            private static void UpdateHippo(
                Runtime runtime,
                LiveWallpaperMapActor actor,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (!link.HasValue)
                    return;
                var actorX = actor.PixelX + 8f;
                var actorY = actor.PixelY + 16f;
                var linkX = link.Value.MapX * 16f - 4f;
                var linkY = link.Value.MapY * 16f - 10f;
                if (runtime.Action == LiveWallpaperActorAction.Idle)
                {
                    if (Intersects(linkX, linkY, 8f, 10f,
                            actorX - 16f, actorY + 12f, 64f, 16f))
                        runtime.Action = LiveWallpaperActorAction.Walk;
                    return;
                }
                var direction = runtime.Direction == 0 ? -1f : 1f;
                if (Intersects(linkX, linkY, 8f, 10f,
                        actorX + direction * 14f - 4f,
                        actorY - 14f, 8f, 18f))
                    runtime.Direction = runtime.Direction == 0 ? 1 : 0;
            }

            private static void UpdateLetterChild(
                Runtime runtime,
                LiveWallpaperMapActor actor,
                float deltaMilliseconds,
                LiveWallpaperSimulatedLinkState? link)
            {
                var nearby = false;
                if (link.HasValue)
                {
                    var actorX = actor.PixelX + 8f;
                    var actorY = actor.PixelY + 16f;
                    var linkX = link.Value.MapX * 16f - 4f;
                    var linkY = link.Value.MapY * 16f - 10f;
                    nearby = Intersects(linkX, linkY, 8f, 10f,
                        actorX - 14f, actorY - 8f, 28f, 16f);
                    if (nearby &&
                        runtime.Action == LiveWallpaperActorAction.Idle)
                    {
                        runtime.Action = LiveWallpaperActorAction.Walk;
                        runtime.Timer = 250f;
                        runtime.Direction = actorX < link.Value.MapX * 16f
                            ? 1
                            : 0;
                    }
                }
                if (nearby || runtime.Action == LiveWallpaperActorAction.Idle)
                    return;
                runtime.Timer -= deltaMilliseconds;
                if (runtime.Timer <= 0)
                    runtime.Action = LiveWallpaperActorAction.Idle;
            }

            private void UpdateGroundHopper(
                Runtime runtime,
                LiveWallpaperMap map,
                LiveWallpaperMapActor actor,
                int actorIndex,
                float deltaMilliseconds,
                LiveWallpaperSimulatedLinkState? link)
            {
                runtime.Timer -= deltaMilliseconds;
                runtime.DirectionChangeTimer = Math.Max(
                    0f, runtime.DirectionChangeTimer - deltaMilliseconds);
                if (runtime.Timer <= 0)
                {
                    if (runtime.Action == LiveWallpaperActorAction.Idle)
                        BeginWalking(runtime, actor.Kind);
                    else
                        BeginIdle(runtime, actor.Kind);
                }

                var frameScale = deltaMilliseconds / (1000f / 60f);
                if (runtime.Action == LiveWallpaperActorAction.Walk)
                {
                    Move(runtime, map, actor, actorIndex, link, frameScale);
                    if (runtime.Grounded)
                    {
                        runtime.Grounded = false;
                        runtime.VelocityZ =
                            actor.Kind is LiveWallpaperMapActorKind.Mouse or
                                LiveWallpaperMapActorKind.LetterBird
                                ? 1f
                                : 0.85f;
                    }
                }
                if (runtime.Grounded)
                    return;

                runtime.VelocityZ += -0.15f * frameScale;
                runtime.Z += runtime.VelocityZ * frameScale;
                if (runtime.Z > 0)
                    return;
                runtime.Z = 0;
                runtime.VelocityZ = 0;
                runtime.Grounded = true;
            }

            private void MoveFrog(
                Runtime runtime,
                LiveWallpaperMap map,
                LiveWallpaperMapActor actor,
                int actorIndex,
                LiveWallpaperSimulatedLinkState? link,
                float frameScale)
            {
                var nextX = runtime.X + runtime.VelocityX * frameScale;
                if (!Blocked(map, actor, actorIndex, nextX, runtime.Y, link))
                    runtime.X = nextX;
                else
                    runtime.VelocityX *= -0.25f;

                var nextY = runtime.Y + runtime.VelocityY * frameScale;
                if (!Blocked(map, actor, actorIndex, runtime.X, nextY, link))
                    runtime.Y = nextY;
                else
                    runtime.VelocityY *= -0.25f;
            }

            private bool IntersectsActorAtLivePosition(
                LiveWallpaperMap map,
                float x,
                float y,
                float width,
                float height,
                int ignoredActorIndex)
            {
                for (var index = 0; index < map.Actors.Count; index++)
                {
                    if (index == ignoredActorIndex)
                        continue;
                    var actor = map.Actors[index];
                    if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0)
                        continue;
                    var bodyX = (float)actor.BodyX;
                    var bodyY = (float)actor.BodyY;
                    if (IsMobile(actor.Kind) &&
                        _runtime.TryGetValue(index, out var runtime) &&
                        runtime.Initialized)
                    {
                        var spawn = GetSpawn(actor);
                        bodyX = runtime.X + actor.BodyX - spawn.X;
                        bodyY = runtime.Y + actor.BodyY - spawn.Y;
                    }
                    if (x < bodyX + actor.BodyWidth && x + width > bodyX &&
                        y < bodyY + actor.BodyHeight && y + height > bodyY)
                        return true;
                }
                return false;
            }

            private static void BeginButterflySegment(Runtime runtime)
            {
                runtime.Timer = Next(runtime, 500, 1000);
                runtime.SegmentDuration = runtime.Timer;
                runtime.LastSpeed = runtime.SpeedGoal;
                runtime.SpeedGoal = Next(runtime, 25, 45) / 100f;
                var difference = new Vector2(
                    runtime.X - runtime.OriginX,
                    runtime.Y - runtime.OriginY);
                var targetRotation = MathF.Atan2(difference.Y, difference.X);
                var randomDirection = (Next(runtime, 0, 20) - 10) / 6f *
                    (MathF.PI / (60f * (runtime.Timer / 1000f)));
                var rotationDifference = targetRotation - runtime.Rotation;
                while (rotationDifference < 0)
                    rotationDifference += MathF.PI * 2f;
                rotationDifference %= MathF.PI * 2f;
                rotationDifference -= MathF.PI;
                var newRotation = rotationDifference /
                                  (60f * (runtime.Timer / 1000f));
                runtime.DirectionChange = MathHelper.Lerp(
                    randomDirection, newRotation,
                    difference.Length() / runtime.StartDistance);
            }

            private static void UpdateButterfly(
                Runtime runtime, float deltaMilliseconds)
            {
                runtime.Timer -= deltaMilliseconds;
                if (runtime.Timer < 0)
                    BeginButterflySegment(runtime);
                runtime.CurrentSpeed = MathHelper.Lerp(
                    runtime.SpeedGoal, runtime.LastSpeed,
                    Math.Clamp(runtime.Timer / Math.Max(1f,
                        runtime.SegmentDuration), 0f, 1f));
                var frameScale = deltaMilliseconds / (1000f / 60f);
                runtime.Rotation = (runtime.Rotation +
                    runtime.DirectionChange * frameScale) % (MathF.PI * 2f);
                runtime.X += MathF.Cos(runtime.Rotation) *
                             runtime.CurrentSpeed * frameScale;
                runtime.Y += MathF.Sin(runtime.Rotation) *
                             runtime.CurrentSpeed * frameScale;
            }

            private static int DirectionTo(float x, float y)
            {
                if (MathF.Abs(x) >= MathF.Abs(y))
                    return x < 0 ? 0 : 2;
                return y < 0 ? 1 : 3;
            }

            private static int Next(Runtime runtime, int minimum, int maximum)
            {
                runtime.RandomState = runtime.RandomState * 1664525u + 1013904223u;
                return minimum + (int)(runtime.RandomState %
                    (uint)Math.Max(1, maximum - minimum));
            }
        }
    }
}
