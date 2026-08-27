using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ
{
    public readonly struct LiveWallpaperLinkInput
    {
        public LiveWallpaperLinkInput(Vector2 move, bool featherPressed)
        {
            Move = move;
            FeatherPressed = featherPressed;
        }

        public Vector2 Move { get; }
        public bool FeatherPressed { get; }
    }

    public readonly struct LiveWallpaperSimulatedLinkState
    {
        public LiveWallpaperSimulatedLinkState(
            float mapX, float mapY, float height, int direction,
            LiveWallpaperLinkRouteAction action, LiveWallpaperLinkInput input)
        {
            MapX = mapX;
            MapY = mapY;
            Height = Math.Max(0f, height);
            Direction = Math.Clamp(direction, 0, 3);
            Action = action;
            Input = input;
        }

        public float MapX { get; }
        public float MapY { get; }
        public float Height { get; }
        public int Direction { get; }
        public LiveWallpaperLinkRouteAction Action { get; }
        public LiveWallpaperLinkInput Input { get; }
    }

    /// <summary>
    /// Silent wallpaper locomotion backed by the same position and body components used by
    /// gameplay. The wallpaper supplies directional and feather inputs but never creates saves,
    /// audio, map events, or Archipelago sessions.
    /// </summary>
    public sealed class LiveWallpaperLinkSimulation
    {
        private const float TileSize = 16f;
        private const float WalkSpeedPerFrame = 1f;
        private readonly CPosition _position = new(0, 0, 0);
        private readonly BodyComponent _body;
        private int _scene = -1;
        private long? _lastElapsed;
        private LiveWallpaperLinkRouteAction _lastAction;
        private Vector2 _detourMove;
        private Vector2 _blockedMove;

        public LiveWallpaperLinkSimulation()
        {
            _body = new BodyComponent(_position, -4, -10, 8, 10, 8)
            {
                MaxJumpHeight = 3,
                Drag = 0.72f,
                DragAir = 0.72f,
                Gravity = -0.15f
            };
        }

        public BodyComponent Body => _body;

        public LiveWallpaperSimulatedLinkState Update(
            int scene, LiveWallpaperLinkState activity,
            long elapsedMilliseconds, bool animated,
            LiveWallpaperMap map = null)
        {
            var route = LiveWallpaperLinkRoute.Resolve(scene, activity.Journey, activity.Walking);
            var target = new Vector2(route.MapX * TileSize, route.MapY * TileSize);
            var elapsedDelta = _lastElapsed.HasValue
                ? elapsedMilliseconds - _lastElapsed.Value
                : 0L;
            var reset = _scene != scene || elapsedDelta < 0 || elapsedDelta > 1000 || !animated;
            _scene = scene;
            _lastElapsed = elapsedMilliseconds;

            if (reset)
            {
                _position.Set(new Vector3(target, route.JumpHeight * TileSize));
                _body.Velocity = Vector3.Zero;
                _body.IsGrounded = _position.Z <= 0;
                _lastAction = route.Action;
                _detourMove = Vector2.Zero;
                _blockedMove = Vector2.Zero;
            }

            var frameScale = Math.Clamp(elapsedDelta / (1000f / 60f), 0f, 6f);
            var difference = target - _position.Position;
            var desiredMove = activity.Walking && difference.LengthSquared() > 0.0001f
                ? Vector2.Normalize(difference)
                : Vector2.Zero;
            var featherPressed = animated &&
                route.Action == LiveWallpaperLinkRouteAction.FeatherJump &&
                _lastAction != LiveWallpaperLinkRouteAction.FeatherJump;
            var inputMove = desiredMove;

            if (!reset && frameScale > 0)
            {
                if (featherPressed && _body.IsGrounded)
                {
                    _body.IsGrounded = false;
                    _body.Velocity.Z = 2.35f;
                }

                inputMove = ResolveSteeredMove(map, desiredMove, frameScale);
                _body.VelocityTarget = inputMove * WalkSpeedPerFrame;

                var movement = _body.VelocityTarget * frameScale;
                if (movement.LengthSquared() > difference.LengthSquared())
                    movement = difference;
                ApplyMapConstrainedMovement(map, movement);

                if (!_body.IsGrounded)
                {
                    _position.Z += _body.Velocity.Z * frameScale;
                    _body.Velocity.Z += _body.Gravity * frameScale;
                    if (_position.Z <= 0)
                    {
                        _position.Z = 0;
                        _body.Velocity.Z = 0;
                        _body.IsGrounded = true;
                    }
                }
            }

            var input = new LiveWallpaperLinkInput(inputMove, featherPressed);
            _lastAction = route.Action;
            return new LiveWallpaperSimulatedLinkState(
                _position.X / TileSize, _position.Y / TileSize, _position.Z,
                ResolveDirection(input.Move, route.Direction), route.Action, input);
        }

        private Vector2 ResolveSteeredMove(
            LiveWallpaperMap map, Vector2 desiredMove, float frameScale)
        {
            if (map == null || desiredMove.LengthSquared() <= 0.0001f)
            {
                _detourMove = Vector2.Zero;
                _blockedMove = Vector2.Zero;
                return desiredMove;
            }

            var primaryMove = DominantCardinal(desiredMove);
            var probeDistance = Math.Max(1f, WalkSpeedPerFrame * frameScale);
            if (_detourMove != Vector2.Zero)
            {
                if (Vector2.Dot(primaryMove, _blockedMove) <= 0 ||
                    CanMove(map, _blockedMove * probeDistance))
                {
                    _detourMove = Vector2.Zero;
                    _blockedMove = Vector2.Zero;
                    return desiredMove;
                }
                if (CanMove(map, _detourMove * probeDistance))
                    return _detourMove;
                _detourMove = -_detourMove;
                if (CanMove(map, _detourMove * probeDistance))
                    return _detourMove;
                _detourMove = Vector2.Zero;
                _blockedMove = Vector2.Zero;
                return Vector2.Zero;
            }

            if (CanMove(map, desiredMove * probeDistance))
                return desiredMove;

            var firstSide = primaryMove.X != 0 ? -Vector2.UnitY : -Vector2.UnitX;
            var secondSide = -firstSide;
            var firstDistance = FindDetourDistance(
                map, primaryMove, firstSide, probeDistance);
            var secondDistance = FindDetourDistance(
                map, primaryMove, secondSide, probeDistance);
            if (firstDistance == int.MaxValue && secondDistance == int.MaxValue)
                return Vector2.Zero;

            _blockedMove = primaryMove;
            _detourMove = firstDistance <= secondDistance ? firstSide : secondSide;
            return _detourMove;
        }

        private int FindDetourDistance(
            LiveWallpaperMap map, Vector2 forward, Vector2 side, float probeDistance)
        {
            const int maximumDetourPixels = 32;
            for (var distance = 1; distance <= maximumDetourPixels; distance++)
            {
                var offset = side * distance;
                if (IntersectsMap(
                        map, _position.X + offset.X, _position.Y + offset.Y))
                    return int.MaxValue;
                if (!IntersectsMap(
                        map, _position.X + offset.X + forward.X * probeDistance,
                        _position.Y + offset.Y + forward.Y * probeDistance))
                    return distance;
            }
            return int.MaxValue;
        }

        private bool CanMove(LiveWallpaperMap map, Vector2 move) =>
            !IntersectsMap(map, _position.X + move.X, _position.Y + move.Y);

        private static Vector2 DominantCardinal(Vector2 move)
        {
            if (MathF.Abs(move.X) >= MathF.Abs(move.Y))
                return new Vector2(MathF.Sign(move.X), 0);
            return new Vector2(0, MathF.Sign(move.Y));
        }

        private static int ResolveDirection(Vector2 move, int fallback)
        {
            if (move.LengthSquared() <= 0.0001f)
                return fallback;
            if (MathF.Abs(move.X) >= MathF.Abs(move.Y))
                return move.X < 0 ? 2 : 3;
            return move.Y < 0 ? 1 : 0;
        }

        private void ApplyMapConstrainedMovement(
            LiveWallpaperMap map, Vector2 movement)
        {
            if (map == null)
            {
                _position.Offset(movement);
                return;
            }

            if (movement.X != 0)
            {
                var nextX = _position.X + movement.X;
                if (!IntersectsMap(map, nextX, _position.Y))
                    _position.X = nextX;
                else
                    _body.VelocityTarget.X = 0;
            }
            if (movement.Y != 0)
            {
                var nextY = _position.Y + movement.Y;
                if (!IntersectsMap(map, _position.X, nextY))
                    _position.Y = nextY;
                else
                    _body.VelocityTarget.Y = 0;
            }
        }

        private bool IntersectsMap(
            LiveWallpaperMap map, float positionX, float positionY) =>
            map.IntersectsCollision(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height,
                includeHoles: _body.IsGrounded);
    }
}
