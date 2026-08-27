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
            long elapsedMilliseconds, bool animated)
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
            }

            var frameScale = Math.Clamp(elapsedDelta / (1000f / 60f), 0f, 6f);
            var difference = target - _position.Position;
            var move = activity.Walking && difference.LengthSquared() > 0.0001f
                ? Vector2.Normalize(difference)
                : Vector2.Zero;
            var featherPressed = animated &&
                route.Action == LiveWallpaperLinkRouteAction.FeatherJump &&
                _lastAction != LiveWallpaperLinkRouteAction.FeatherJump;
            var input = new LiveWallpaperLinkInput(move, featherPressed);

            if (!reset && frameScale > 0)
            {
                _body.VelocityTarget = input.Move * WalkSpeedPerFrame;
                var movement = _body.VelocityTarget * frameScale;
                if (movement.LengthSquared() > difference.LengthSquared())
                    movement = difference;
                _position.Offset(movement);

                if (input.FeatherPressed && _body.IsGrounded)
                {
                    _body.IsGrounded = false;
                    _body.Velocity.Z = 2.35f;
                }
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

            _lastAction = route.Action;
            return new LiveWallpaperSimulatedLinkState(
                _position.X / TileSize, _position.Y / TileSize, _position.Z,
                route.Direction, route.Action, input);
        }
    }
}
