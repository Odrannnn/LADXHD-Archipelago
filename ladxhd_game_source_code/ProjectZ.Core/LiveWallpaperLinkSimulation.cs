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
            LiveWallpaperLinkRouteAction action, LiveWallpaperLinkInput input,
            int interactionActorIndex = -1,
            bool roosterVisible = false,
            bool carryingRooster = false,
            float roosterMapX = 0,
            float roosterMapY = 0,
            float roosterHeight = 0)
        {
            MapX = mapX;
            MapY = mapY;
            Height = Math.Max(0f, height);
            Direction = Math.Clamp(direction, 0, 3);
            Action = action;
            Input = input;
            InteractionActorIndex = interactionActorIndex;
            RoosterVisible = roosterVisible;
            CarryingRooster = carryingRooster;
            RoosterMapX = roosterMapX;
            RoosterMapY = roosterMapY;
            RoosterHeight = Math.Max(0f, roosterHeight);
        }

        public float MapX { get; }
        public float MapY { get; }
        public float Height { get; }
        public int Direction { get; }
        public LiveWallpaperLinkRouteAction Action { get; }
        public LiveWallpaperLinkInput Input { get; }
        public int InteractionActorIndex { get; }
        public bool RoosterVisible { get; }
        public bool CarryingRooster { get; }
        public float RoosterMapX { get; }
        public float RoosterMapY { get; }
        public float RoosterHeight { get; }
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
        private int _lastRouteDirection;
        private Vector2 _detourMove;
        private Vector2 _blockedMove;
        private Vector2 _committedJumpMove;
        private float _committedJumpRemaining;
        private LiveWallpaperJourneyPlan _journeyPlan;
        private int _journeyPointIndex;
        private int _journeyVariant;
        private long _nextJourneyAt;
        private long _pauseUntil;
        private bool _interactionPauseStarted;
        private bool _roosterPickupPauseStarted;
        private bool _journeyIslandLife;
        private int _journeyOriginX = -1;
        private int _journeyOriginY = -1;
        private int _journeyColumns = -1;
        private int _journeyRows = -1;

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

        public LiveWallpaperSimulatedLinkState UpdateJourney(
            int scene,
            int activityMode,
            long elapsedMilliseconds,
            bool animated,
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            bool allowIslandLife)
        {
            var elapsedDelta = _lastElapsed.HasValue
                ? elapsedMilliseconds - _lastElapsed.Value
                : 0L;
            var reset = _scene != scene || elapsedDelta < 0 || elapsedDelta > 1000 ||
                        _journeyPlan == null || _journeyIslandLife != allowIslandLife ||
                        _journeyOriginX != viewport.OriginX ||
                        _journeyOriginY != viewport.OriginY ||
                        _journeyColumns != viewport.Columns ||
                        _journeyRows != viewport.Rows;
            _scene = scene;
            _lastElapsed = elapsedMilliseconds;
            if (reset)
            {
                _journeyVariant = (int)Math.Max(0, elapsedMilliseconds / 20_000L);
                StartJourney(map, viewport, scene, allowIslandLife, elapsedMilliseconds);
                if (!animated || activityMode == 1)
                    PlaceAtJourneyRestPoint(viewport);
                elapsedDelta = 0;
            }

            if (_journeyPlan == null || _journeyPlan.Points.Count == 0)
            {
                var fallback = LiveWallpaperLinkActivity.ResolveForScene(
                    activityMode, scene, elapsedMilliseconds, animated);
                return Update(scene, fallback, elapsedMilliseconds, animated, map);
            }

            if (_journeyPointIndex >= _journeyPlan.Points.Count)
            {
                if (_nextJourneyAt <= 0)
                    _nextJourneyAt = elapsedMilliseconds + (activityMode == 2 ? 4_000L : 650L);
                if (animated && activityMode != 1 && elapsedMilliseconds >= _nextJourneyAt)
                {
                    _journeyVariant++;
                    StartJourney(map, viewport, scene, allowIslandLife, elapsedMilliseconds);
                }
            }

            var canMove = animated && activityMode != 1 &&
                          elapsedMilliseconds >= _pauseUntil &&
                          _journeyPointIndex < _journeyPlan.Points.Count;
            var frameScale = Math.Clamp(elapsedDelta / (1000f / 60f), 0f, 6f);
            var inputMove = Vector2.Zero;
            var interactionActor = -1;
            var action = LiveWallpaperLinkRouteAction.Stand;
            var targetJourneyAction = _journeyPointIndex < _journeyPlan.Points.Count
                ? _journeyPlan.Points[_journeyPointIndex].Action
                : LiveWallpaperJourneyAction.Walk;
            if (canMove && frameScale > 0)
            {
                var targetPoint = _journeyPlan.Points[_journeyPointIndex];
                var target = new Vector2(targetPoint.PixelX, targetPoint.PixelY);
                var difference = target - _position.Position;
                var carrying = IsCarryingRooster();
                var speed = carrying ? 0.5f : WalkSpeedPerFrame;
                var maximumMovement = speed * frameScale;
                if (difference.LengthSquared() <= maximumMovement * maximumMovement)
                {
                    _position.X = target.X;
                    _position.Y = target.Y;
                    OnJourneyPointReached(elapsedMilliseconds);
                }
                else
                {
                    inputMove = Vector2.Normalize(difference);
                    var movement = inputMove * maximumMovement;
                    var jumping = targetJourneyAction ==
                                  LiveWallpaperJourneyAction.FeatherJump ||
                                  !_body.IsGrounded;
                    if (jumping && _body.IsGrounded)
                    {
                        _body.IsGrounded = false;
                        _body.Velocity.Z = 2.35f;
                    }
                    ApplyJourneyConstrainedMovement(
                        map, movement, includeHoles: !carrying && !jumping);
                }
            }

            var carryingRooster = IsCarryingRooster();
            if (carryingRooster)
            {
                // ObjCock holds itself at Z=36 and lifts Link by its real CarryHeight (14).
                _position.Z = 22f + MathF.Sin(
                    elapsedMilliseconds / 450f * MathF.PI * 2f) * 1.5f;
                _body.IsGrounded = false;
                _body.Velocity.Z = 0;
                action = LiveWallpaperLinkRouteAction.RoosterFly;
            }
            else
            {
                if (!_body.IsGrounded && frameScale > 0)
                {
                    _position.Z += _body.Velocity.Z * frameScale;
                    _body.Velocity.Z += _body.Gravity * frameScale;
                    if (_position.Z <= 0)
                    {
                        _position.Z = 0;
                        _body.Velocity.Z = 0;
                        _body.IsGrounded = true;
                    }
                    else
                        action = LiveWallpaperLinkRouteAction.FeatherJump;
                }
                else if (_body.IsGrounded)
                    _position.Z = 0;
                if (_pauseUntil > elapsedMilliseconds &&
                    _journeyPlan.HasInteraction && _interactionPauseStarted &&
                    _journeyPointIndex == _journeyPlan.InteractionPointIndex)
                {
                    action = LiveWallpaperLinkRouteAction.Interact;
                    interactionActor = _journeyPlan.InteractionActorIndex;
                    inputMove = FaceActor(map, interactionActor);
                }
                else if (inputMove != Vector2.Zero && _body.IsGrounded)
                    action = targetJourneyAction == LiveWallpaperJourneyAction.FeatherJump
                        ? LiveWallpaperLinkRouteAction.FeatherJump
                        : LiveWallpaperLinkRouteAction.Walk;
            }

            var fallbackDirection = _lastRouteDirection;
            var direction = ResolveDirection(inputMove, fallbackDirection);
            if (interactionActor >= 0)
                direction = ResolveDirection(FaceActor(map, interactionActor), fallbackDirection);
            _lastRouteDirection = direction;
            var roosterVisible = _journeyPlan.HasRoosterFlight;
            ResolveRoosterState(carryingRooster,
                out var roosterX, out var roosterY, out var roosterHeight);
            return new LiveWallpaperSimulatedLinkState(
                _position.X / TileSize, _position.Y / TileSize, _position.Z,
                direction, action, new LiveWallpaperLinkInput(inputMove, false),
                interactionActor, roosterVisible, carryingRooster,
                roosterX / TileSize, roosterY / TileSize, roosterHeight);
        }

        private void StartJourney(
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            int scene,
            bool allowIslandLife,
            long elapsedMilliseconds)
        {
            _journeyPlan = LiveWallpaperJourneyPlanner.Create(
                map, viewport, scene, _journeyVariant, allowIslandLife);
            _journeyIslandLife = allowIslandLife;
            _journeyOriginX = viewport.OriginX;
            _journeyOriginY = viewport.OriginY;
            _journeyColumns = viewport.Columns;
            _journeyRows = viewport.Rows;
            _journeyPointIndex = _journeyPlan.Points.Count > 1 ? 1 : 0;
            _nextJourneyAt = 0;
            _pauseUntil = elapsedMilliseconds;
            _interactionPauseStarted = false;
            _roosterPickupPauseStarted = false;
            if (_journeyPlan.Points.Count > 0)
            {
                var start = _journeyPlan.Points[0];
                _position.Set(new Vector3(start.PixelX, start.PixelY, 0));
            }
            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;
            _body.IsGrounded = true;
        }

        private void PlaceAtJourneyRestPoint(LiveWallpaperMapViewport viewport)
        {
            if (_journeyPlan == null || _journeyPlan.Points.Count == 0)
                return;
            var centerX = (viewport.OriginX + viewport.Columns / 2f) * TileSize;
            var centerY = (viewport.OriginY + viewport.Rows / 2f) * TileSize;
            var nearestIndex = 0;
            var nearestDistance = float.MaxValue;
            for (var index = 0; index < _journeyPlan.Points.Count; index++)
            {
                var point = _journeyPlan.Points[index];
                var deltaX = point.PixelX - centerX;
                var deltaY = point.PixelY - centerY;
                var distance = deltaX * deltaX + deltaY * deltaY;
                if (distance >= nearestDistance)
                    continue;
                nearestDistance = distance;
                nearestIndex = index;
            }
            var restingPoint = _journeyPlan.Points[nearestIndex];
            _position.Set(new Vector3(restingPoint.PixelX, restingPoint.PixelY, 0));
            _journeyPointIndex = nearestIndex;
            _body.IsGrounded = true;
        }

        private void OnJourneyPointReached(long elapsedMilliseconds)
        {
            if (_journeyPlan.HasInteraction &&
                _journeyPointIndex == _journeyPlan.InteractionPointIndex &&
                !_interactionPauseStarted)
            {
                _interactionPauseStarted = true;
                _pauseUntil = elapsedMilliseconds + 2_200L;
                return;
            }
            if (_journeyPlan.HasRoosterFlight &&
                _journeyPointIndex == _journeyPlan.RoosterPickupPointIndex &&
                !_roosterPickupPauseStarted)
            {
                _roosterPickupPauseStarted = true;
                _pauseUntil = elapsedMilliseconds + 900L;
                return;
            }
            _journeyPointIndex++;
        }

        private bool IsCarryingRooster() =>
            _journeyPlan?.HasRoosterFlight == true &&
            _roosterPickupPauseStarted &&
            _journeyPointIndex > _journeyPlan.RoosterPickupPointIndex &&
            _journeyPointIndex <= _journeyPlan.RoosterLandingPointIndex;

        private void ResolveRoosterState(
            bool carrying, out float pixelX, out float pixelY, out float height)
        {
            pixelX = _position.X;
            pixelY = _position.Y;
            height = 0;
            if (_journeyPlan?.HasRoosterFlight != true)
                return;
            if (carrying)
            {
                height = _position.Z + 14f;
                return;
            }
            var pointIndex = _roosterPickupPauseStarted
                ? _journeyPlan.RoosterLandingPointIndex
                : _journeyPlan.RoosterPickupPointIndex;
            var point = _journeyPlan.Points[Math.Clamp(
                pointIndex, 0, _journeyPlan.Points.Count - 1)];
            pixelX = point.PixelX;
            pixelY = point.PixelY;
        }

        private Vector2 FaceActor(LiveWallpaperMap map, int actorIndex)
        {
            if (map == null || actorIndex < 0 || actorIndex >= map.Actors.Count)
                return Vector2.Zero;
            var actor = map.Actors[actorIndex];
            var difference = new Vector2(
                actor.BodyX + actor.BodyWidth / 2f - _position.X,
                actor.BodyY + actor.BodyHeight / 2f - _position.Y);
            return difference.LengthSquared() > 0.0001f
                ? Vector2.Normalize(difference)
                : Vector2.Zero;
        }

        private void ApplyJourneyConstrainedMovement(
            LiveWallpaperMap map, Vector2 movement, bool includeHoles)
        {
            if (map == null)
            {
                _position.Offset(movement);
                return;
            }
            if (movement.X != 0)
            {
                var nextX = _position.X + movement.X;
                if (!IntersectsJourneyMap(map, nextX, _position.Y, includeHoles))
                    _position.X = nextX;
            }
            if (movement.Y != 0)
            {
                var nextY = _position.Y + movement.Y;
                if (!IntersectsJourneyMap(map, _position.X, nextY, includeHoles))
                    _position.Y = nextY;
            }
        }

        private bool IntersectsJourneyMap(
            LiveWallpaperMap map, float positionX, float positionY, bool includeHoles) =>
            map.IntersectsCollision(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height,
                includeHoles) ||
            map.IntersectsActor(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height);

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
                _lastRouteDirection = route.Direction;
                _detourMove = Vector2.Zero;
                _blockedMove = Vector2.Zero;
                _committedJumpMove = Vector2.Zero;
                _committedJumpRemaining = 0;
            }

            var frameScale = Math.Clamp(elapsedDelta / (1000f / 60f), 0f, 6f);
            var difference = target - _position.Position;
            var desiredMove = activity.Walking && difference.LengthSquared() > 0.0001f
                ? Vector2.Normalize(difference)
                : Vector2.Zero;
            var featherPressed = animated &&
                route.Action == LiveWallpaperLinkRouteAction.FeatherJump &&
                (_lastAction != LiveWallpaperLinkRouteAction.FeatherJump ||
                 route.Direction != _lastRouteDirection);
            var inputMove = desiredMove;

            if (!reset && frameScale > 0)
            {
                if (featherPressed && _body.IsGrounded)
                {
                    _body.IsGrounded = false;
                    _body.Velocity.Z = 2.35f;
                    _committedJumpMove = DirectionToVector(route.Direction);
                    _committedJumpRemaining = TileSize * 2f;
                    if (_committedJumpMove.X != 0)
                        _position.Y = target.Y;
                    else
                        _position.X = target.X;
                }

                if (_committedJumpRemaining > 0)
                    inputMove = _committedJumpMove;
                else if (route.Action == LiveWallpaperLinkRouteAction.FeatherJump &&
                         _committedJumpMove != Vector2.Zero &&
                         Vector2.Dot(target - _position.Position, _committedJumpMove) < 0)
                    inputMove = Vector2.Zero;
                else
                    inputMove = ResolveSteeredMove(map, desiredMove, frameScale);
                _body.VelocityTarget = inputMove * WalkSpeedPerFrame;

                var movement = _body.VelocityTarget * frameScale;
                if (_committedJumpRemaining <= 0 &&
                    movement.LengthSquared() > difference.LengthSquared())
                    movement = difference;
                var beforeMove = _position.Position;
                ApplyMapConstrainedMovement(map, movement);
                if (_committedJumpRemaining > 0)
                {
                    var distanceMoved = (_position.Position - beforeMove).Length();
                    _committedJumpRemaining = Math.Max(
                        0, _committedJumpRemaining - distanceMoved);
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

            var input = new LiveWallpaperLinkInput(inputMove, featherPressed);
            _lastAction = route.Action;
            _lastRouteDirection = route.Direction;
            if (route.Action != LiveWallpaperLinkRouteAction.FeatherJump &&
                _committedJumpRemaining <= 0)
                _committedJumpMove = Vector2.Zero;
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
                return move.X < 0 ? 0 : 2;
            return move.Y < 0 ? 1 : 3;
        }

        private static Vector2 DirectionToVector(int direction) => direction switch
        {
            0 => -Vector2.UnitX,
            1 => -Vector2.UnitY,
            2 => Vector2.UnitX,
            _ => Vector2.UnitY
        };

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
                includeHoles: _body.IsGrounded) ||
            map.IntersectsActor(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height);
    }
}
