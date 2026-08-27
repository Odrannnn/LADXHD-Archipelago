using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ
{
    public readonly struct LiveWallpaperFollowerState
    {
        public LiveWallpaperFollowerState(float horizontalOffset, float height, bool facingRight)
        {
            HorizontalOffset = horizontalOffset;
            Height = Math.Max(0f, height);
            FacingRight = facingRight;
        }

        public float HorizontalOffset { get; }
        public float Height { get; }
        public bool FacingRight { get; }
    }

    /// <summary>
    /// Runs the follower-distance, chain, and hopping mechanics on the same body component used
    /// by the in-game NPCs, but without their map, dialog, save, audio, or combat dependencies.
    /// Positions are expressed in game pixels and scaled only by the Android renderer.
    /// </summary>
    public sealed class LiveWallpaperFollowerSimulation
    {
        private readonly CPosition _position = new(0, 0, 0);
        private readonly BodyComponent _body;
        private int _character = -1;
        private long? _lastElapsed;
        private bool _facingRight = true;

        public LiveWallpaperFollowerSimulation()
        {
            _body = new BodyComponent(_position, -4, -10, 8, 10, 8)
            {
                Drag = 0.85f,
                DragAir = 0.85f,
                Gravity = -0.15f
            };
        }

        public BodyComponent Body => _body;

        public LiveWallpaperFollowerState Update(
            int character, float targetOffset, long elapsedMilliseconds, bool animated)
        {
            var elapsedDelta = _lastElapsed.HasValue
                ? elapsedMilliseconds - _lastElapsed.Value
                : 0L;
            var reset = character != _character || elapsedDelta < 0 || elapsedDelta > 1000 || !animated;
            _character = character;
            _lastElapsed = elapsedMilliseconds;

            if (reset)
            {
                var initialOffset = character == 1
                    ? Math.Clamp(targetOffset, -46f, 46f)
                    : targetOffset;
                _position.Set(new Vector3(initialOffset, 0, 0));
                _body.Velocity = Vector3.Zero;
                _body.VelocityTarget = Vector2.Zero;
                _body.IsGrounded = true;
            }

            var frameScale = Math.Clamp(elapsedDelta / (1000f / 60f), 0f, 6f);
            if (!reset && frameScale > 0)
            {
                var difference = targetOffset - _position.X;
                var distance = Math.Abs(difference);
                var direction = Math.Sign(difference);
                var maxSpeed = character switch
                {
                    1 => 1.5f,
                    2 => 2f,
                    _ => 0.75f
                };
                var followDistance = character == 2 ? 18f : 0f;
                var speed = character == 2
                    ? MathHelper.Clamp((distance - followDistance) / 4f, -2f, 2f)
                    : Math.Min(maxSpeed, distance * 0.45f);
                if (character == 2 && distance > followDistance * 2f + 4f)
                    speed = MathHelper.Clamp(distance / (followDistance + 4f), -2f, 2f);
                speed = Math.Max(0f, speed);
                _body.VelocityTarget = new Vector2(direction * Math.Min(maxSpeed, speed), 0);
                var movement = _body.VelocityTarget.X * frameScale;
                if (Math.Abs(movement) > distance)
                    movement = difference;
                _position.X += movement;
                if (Math.Abs(_body.VelocityTarget.X) > 0.01f)
                    _facingRight = _body.VelocityTarget.X > 0;

                if (character is 1 or 2 && _body.IsGrounded)
                {
                    _body.IsGrounded = false;
                    _body.Velocity.Z = character == 2
                        ? MathHelper.Clamp(distance / 18f, 1f, 2f)
                        : 0.8f;
                }
                if (!_body.IsGrounded)
                {
                    var gravity = character == 2 ? -0.075f : -0.175f;
                    _position.Z += _body.Velocity.Z * frameScale;
                    _body.Velocity.Z += gravity * frameScale;
                    if (_position.Z <= 0)
                    {
                        _position.Z = 0;
                        _body.Velocity.Z = 0;
                        _body.IsGrounded = true;
                    }
                }

                // BowWow's in-game chain never permits the body farther than this radius.
                if (character == 1)
                    _position.X = Math.Clamp(_position.X, -46f, 46f);
            }

            return new LiveWallpaperFollowerState(_position.X, _position.Z, _facingRight);
        }
    }
}
