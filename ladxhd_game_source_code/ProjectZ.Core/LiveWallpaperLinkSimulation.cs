using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ
{
    public enum LiveWallpaperStoneImpactKind
    {
        None,
        Break,
        Water,
        Hole,
        Enemy
    }

    public enum LiveWallpaperVegetationDropKind
    {
        None,
        Heart,
        Rupee
    }

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

    public readonly struct LiveWallpaperAttackBox
    {
        public LiveWallpaperAttackBox(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = Math.Max(0f, width);
            Height = Math.Max(0f, height);
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public bool Valid => Width > 0f && Height > 0f;
        public bool Intersects(float x, float y, float width, float height) =>
            Valid && X < x + width && X + Width > x &&
            Y < y + height && Y + Height > y;
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
            float roosterHeight = 0,
            int combatEnemyIndex = -1,
            float actionProgress = 0,
            LiveWallpaperAttackBox attackBox = default,
            IReadOnlySet<int> cutBushes = null,
            IReadOnlyDictionary<int, long> cutVegetationTimes = null,
            IReadOnlyDictionary<int, LiveWallpaperVegetationDropKind>
                vegetationDrops = null,
            IReadOnlyDictionary<int, Vector2> vegetationDropDirections = null,
            IReadOnlyDictionary<int, long> collectedVegetationDropTimes = null,
            IReadOnlySet<int> liftedStones = null,
            int activeLiftedStoneKey = -1,
            float activeStoneEntityX = 0,
            float activeStoneEntityY = 0,
            float activeStoneHeight = 0,
            LiveWallpaperStoneImpactKind stoneImpactKind =
                LiveWallpaperStoneImpactKind.None,
            float stoneImpactX = 0,
            float stoneImpactY = 0,
            long stoneImpactStartedAt = 0,
            int stoneImpactSerial = 0,
            int stoneImpactEnemyIndex = -1,
            int collectedRupees = 0,
            int collectedHearts = 0,
            bool activeStoneReleased = false,
            bool hookshotVisible = false,
            float hookshotMapX = 0,
            float hookshotMapY = 0,
            IReadOnlySet<int> openedChests = null,
            int activeChestKey = -1,
            string chestItemSpriteId = null,
            int chestItemShowAnimation = 1,
            IReadOnlyDictionary<int, Vector2> moveStones = null,
            IReadOnlySet<int> fallenMoveStones = null)
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
            CombatEnemyIndex = combatEnemyIndex;
            ActionProgress = Math.Clamp(actionProgress, 0f, 1f);
            AttackBox = attackBox;
            CutBushes = cutBushes;
            CutVegetationTimes = cutVegetationTimes;
            VegetationDrops = vegetationDrops;
            VegetationDropDirections = vegetationDropDirections;
            CollectedVegetationDropTimes = collectedVegetationDropTimes;
            LiftedStones = liftedStones;
            ActiveLiftedStoneKey = activeLiftedStoneKey;
            ActiveStoneEntityX = activeStoneEntityX;
            ActiveStoneEntityY = activeStoneEntityY;
            ActiveStoneHeight = Math.Max(0f, activeStoneHeight);
            StoneImpactKind = stoneImpactKind;
            StoneImpactX = stoneImpactX;
            StoneImpactY = stoneImpactY;
            StoneImpactStartedAt = stoneImpactStartedAt;
            StoneImpactSerial = stoneImpactSerial;
            StoneImpactEnemyIndex = stoneImpactEnemyIndex;
            CollectedRupees = Math.Max(0, collectedRupees);
            CollectedHearts = Math.Max(0, collectedHearts);
            ActiveStoneReleased = activeStoneReleased;
            HookshotVisible = hookshotVisible;
            HookshotMapX = hookshotMapX;
            HookshotMapY = hookshotMapY;
            OpenedChests = openedChests;
            ActiveChestKey = activeChestKey;
            ChestItemSpriteId = chestItemSpriteId;
            ChestItemShowAnimation = chestItemShowAnimation == 2 ? 2 : 1;
            MoveStones = moveStones;
            FallenMoveStones = fallenMoveStones;
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
        public int CombatEnemyIndex { get; }
        public float ActionProgress { get; }
        public LiveWallpaperAttackBox AttackBox { get; }
        public IReadOnlySet<int> CutBushes { get; }
        public IReadOnlyDictionary<int, long> CutVegetationTimes { get; }
        public IReadOnlyDictionary<int, LiveWallpaperVegetationDropKind>
            VegetationDrops { get; }
        public IReadOnlyDictionary<int, Vector2> VegetationDropDirections { get; }
        public IReadOnlyDictionary<int, long> CollectedVegetationDropTimes { get; }
        public IReadOnlySet<int> LiftedStones { get; }
        public int ActiveLiftedStoneKey { get; }
        public float ActiveStoneEntityX { get; }
        public float ActiveStoneEntityY { get; }
        public float ActiveStoneHeight { get; }
        public LiveWallpaperStoneImpactKind StoneImpactKind { get; }
        public float StoneImpactX { get; }
        public float StoneImpactY { get; }
        public long StoneImpactStartedAt { get; }
        public int StoneImpactSerial { get; }
        public int StoneImpactEnemyIndex { get; }
        public int CollectedRupees { get; }
        public int CollectedHearts { get; }
        public bool ActiveStoneReleased { get; }
        public bool HookshotVisible { get; }
        public float HookshotMapX { get; }
        public float HookshotMapY { get; }
        public IReadOnlySet<int> OpenedChests { get; }
        public int ActiveChestKey { get; }
        public string ChestItemSpriteId { get; }
        public int ChestItemShowAnimation { get; }
        public IReadOnlyDictionary<int, Vector2> MoveStones { get; }
        public IReadOnlySet<int> FallenMoveStones { get; }

        public LiveWallpaperSimulatedLinkState WithAttackBox(
            LiveWallpaperAttackBox attackBox) =>
            new(MapX, MapY, Height, Direction, Action, Input,
                InteractionActorIndex, RoosterVisible, CarryingRooster,
                RoosterMapX, RoosterMapY, RoosterHeight,
                CombatEnemyIndex, ActionProgress, attackBox, CutBushes,
                CutVegetationTimes, VegetationDrops,
                VegetationDropDirections,
                CollectedVegetationDropTimes,
                LiftedStones, ActiveLiftedStoneKey,
                ActiveStoneEntityX, ActiveStoneEntityY, ActiveStoneHeight,
                StoneImpactKind, StoneImpactX, StoneImpactY,
                StoneImpactStartedAt, StoneImpactSerial,
                StoneImpactEnemyIndex, CollectedRupees,
                CollectedHearts,
                ActiveStoneReleased,
                HookshotVisible, HookshotMapX, HookshotMapY,
                OpenedChests, ActiveChestKey, ChestItemSpriteId,
                ChestItemShowAnimation, MoveStones, FallenMoveStones);
    }

    /// <summary>
    /// Silent wallpaper locomotion backed by the same position and body components used by
    /// gameplay. The wallpaper supplies directional and feather inputs but never creates saves,
    /// audio, map events, or Archipelago sessions.
    /// </summary>
    public sealed class LiveWallpaperLinkSimulation
    {
        private const float TileSize = 16f;
        private const float WalkSpeedPerFrame = LinkGameplayMotion.WalkSpeed;
        private const long MoveStoneInertiaMilliseconds = 500L;
        private const long MoveStoneMovementMilliseconds = 450L;
        // ObjLink power-bracelet and ObjStone timings/physics.
        private const long StonePullMilliseconds =
            (long)LinkGameplayMotion.PullMilliseconds;
        private const long StonePreCarryMilliseconds =
            (long)LinkGameplayMotion.PreCarryMilliseconds;
        private const long StoneThrowInputDelayMilliseconds =
            LinkGameplayMotion.MinimumSeparateInputMilliseconds;
        private long _stoneThrowAnimationMilliseconds =
            StoneGameplayMotion.ThrowFlightMilliseconds;
        private long StoneSequenceMilliseconds =>
            StonePullMilliseconds + StonePreCarryMilliseconds +
            StoneThrowInputDelayMilliseconds + Math.Max(
                _stoneThrowAnimationMilliseconds,
                StoneGameplayMotion.ThrowFlightMilliseconds) +
            LinkGameplayMotion.MinimumSeparateInputMilliseconds;
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
        private Vector2 _airMoveVelocity;
        private LiveWallpaperJourneyPlan _journeyPlan;
        private LiveWallpaperMap _currentJourneyMap;
        private int _journeyPointIndex;
        private int _journeyVariant;
        private long _nextJourneyAt;
        private bool _manualDestinationActive;
        private long _journeyBlockedSince;
        private int _journeyProgressPointIndex = -1;
        private float _journeyBestTargetDistance = float.MaxValue;
        private long _pauseUntil;
        private bool _interactionPauseStarted;
        private bool _chestPauseStarted;
        private long _chestOpenedAt;
        private bool _roosterPickupPauseStarted;
        private long _roosterPickupStartedAt;
        private float _roosterFlightHeight;
        private bool _roosterReleaseStarted;
        private long _roosterReleaseStartedAt;
        private RoosterReleaseMotionState _roosterReleaseState;
        private bool _combatPauseStarted;
        private long _combatStartedAt;
        private int _runtimeCombatEnemyIndex = -1;
        private bool _pegasusChargePauseStarted;
        private long _pegasusChargeStartedAt;
        private bool _pegasusJumpActive;
        private bool _hookshotStarted;
        private bool _hookshotPulling;
        private Vector2 _hookshotLinkStart;
        private Vector2 _hookshotPosition;
        private Vector2 _hitVelocity;
        private long _damageUntil;
        private Vector2 _holeResetPosition;
        private Point _holeResetField = new(int.MinValue, int.MinValue);
        private bool _holeFalling;
        private long _holeFallStartedAt;
        private long _holeFallAnimationMilliseconds = 850L;
        private bool _journeyIslandLife;
        private bool _journeyFollowLoadingZones;
        private bool _journeyAllowViewportFollow;
        private bool _journeyExited;
        private readonly HashSet<int> _cutBushes = new();
        private readonly Dictionary<int, long> _cutVegetationTimes = new();
        private readonly Dictionary<int, LiveWallpaperVegetationDropKind>
            _vegetationDrops = new();
        private readonly Dictionary<int, Vector2> _vegetationDropDirections = new();
        private readonly Dictionary<int, long> _collectedVegetationDropTimes = new();
        private readonly Random _vegetationRandom;
        private int _pendingVegetationDropKey = -1;
        private int _collectedRupees;
        private int _collectedHearts;
        private long _bushCutStartedAt;
        private bool _runtimeBushCutActive;
        private Vector2 _runtimeBushCutDirection;
        private readonly HashSet<int> _liftedStones = new();
        private readonly Dictionary<int, Vector2> _moveStones = new();
        private readonly HashSet<int> _relocatedMoveStones = new();
        private readonly HashSet<int> _fallenMoveStones = new();
        private int _activeMoveStoneKey = -1;
        private Vector2 _activeMoveStoneStart;
        private Vector2 _activeMoveStoneGoal;
        private long _moveStonePushStartedAt;
        private bool _moveStoneJourneyAction;
        private readonly HashSet<int> _openedChests = new();
        private readonly Dictionary<int, long> _liftedStoneTimes = new();
        private long _stoneLiftStartedAt;
        private bool _runtimeStoneLiftActive;
        private Vector2 _runtimeStoneLiftDirection;
        private int _activeLiftedStoneKey = -1;
        private LiveWallpaperStoneImpactKind _stoneImpactKind;
        private float _stoneImpactX;
        private float _stoneImpactY;
        private long _stoneImpactStartedAt;
        private int _stoneImpactSerial;
        private int _stoneImpactEnemyIndex = -1;
        private readonly Dictionary<int, LiveWallpaperEnemyState> _liveEnemies = new();
        private LiveWallpaperMap _liveEnemyMap;
        private readonly Dictionary<int, LiveWallpaperActorState> _liveActors = new();
        private LiveWallpaperMap _liveActorMap;
        private int _journeyOriginX = -1;
        private int _journeyOriginY = -1;
        private int _journeyColumns = -1;
        private int _journeyRows = -1;
        private readonly HashSet<int> _visitedFieldKeys = new();
        private int _lastRememberedFieldKey = -1;
        private int _fieldTransitionsWithoutDiscovery;
        private bool _continueFromCurrentOnReset;
        private string _entryPortalIdToAvoid;

        private const int MaximumFieldTransitionsWithoutDiscovery = 12;

        public LiveWallpaperLinkSimulation(
            int vegetationRandomSeed = unchecked((int)0x6d2b79f5u))
        {
            _vegetationRandom = new Random(vegetationRandomSeed);
            _body = new BodyComponent(_position, -4, -10, 8, 10, 8)
            {
                MaxJumpHeight = 3,
                Drag = 0.72f,
                DragAir = 0.72f,
                Gravity = LinkGameplayMotion.Gravity,
                AbsorbStop = 0.25f,
                AbsorbPercentage = 1f,
                CornerCorrection = true,
                CornerCorrectionThreshold =
                    LinkGameplayMotion.CornerCorrectionThreshold
            };
        }

        public BodyComponent Body => _body;
        public int VisitedOverworldFieldCount => _visitedFieldKeys.Count;

        public void EnterMap(
            float pixelX, float pixelY, string entryPortalId = null)
        {
            _position.Set(new Vector3(pixelX, pixelY, 0f));
            _holeResetPosition = new Vector2(pixelX, pixelY);
            _holeResetField = GetHoleResetField(pixelX, pixelY);
            _holeFalling = false;
            _holeFallStartedAt = 0L;
            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;
            _body.HoleAbsorption = Vector2.Zero;
            _body.WasHolePulled = false;
            _body.SpeedMultiply = 1f;
            _body.IsGrounded = true;
            _journeyPlan = null;
            _journeyPointIndex = 0;
            _journeyExited = false;
            _continueFromCurrentOnReset = true;
            _entryPortalIdToAvoid = entryPortalId;
            _nextJourneyAt = 0L;
            _pauseUntil = _lastElapsed ?? 0L;
            _manualDestinationActive = false;
            _runtimeBushCutActive = false;
            _runtimeStoneLiftActive = false;
            _runtimeCombatEnemyIndex = -1;
            _chestPauseStarted = false;
            _chestOpenedAt = 0L;
            _activeLiftedStoneKey = -1;
            _hookshotStarted = false;
            _hookshotPulling = false;
            _hitVelocity = Vector2.Zero;
            _airMoveVelocity = Vector2.Zero;
            _pegasusJumpActive = false;
            _cutBushes.Clear();
            _cutVegetationTimes.Clear();
            _vegetationDrops.Clear();
            _vegetationDropDirections.Clear();
            _collectedVegetationDropTimes.Clear();
            _liftedStones.Clear();
            _liftedStoneTimes.Clear();
            _moveStones.Clear();
            _relocatedMoveStones.Clear();
            _fallenMoveStones.Clear();
            _activeMoveStoneKey = -1;
            _moveStonePushStartedAt = 0L;
            _moveStoneJourneyAction = false;
            _openedChests.Clear();
        }

        public void UpdateLiveEnemyState(
            LiveWallpaperMap map, int enemyIndex, LiveWallpaperEnemyState state)
        {
            if (!ReferenceEquals(_liveEnemyMap, map))
            {
                _liveEnemyMap = map;
                _liveEnemies.Clear();
            }
            if (enemyIndex >= 0 && map != null && enemyIndex < map.Enemies.Count)
                _liveEnemies[enemyIndex] = state;
        }

        public void BeginLiveStateFrame(LiveWallpaperMap map)
        {
            // Keep resolved enemy states across culling frames. Clearing them here
            // discarded a dead/hidden enemy as soon as it left the active margin,
            // causing collision to fall back to its static spawn rectangle.
            if (!ReferenceEquals(_liveEnemyMap, map))
            {
                _liveEnemyMap = map;
                _liveEnemies.Clear();
            }
            if (!ReferenceEquals(_liveActorMap, map))
            {
                _liveActorMap = map;
                _liveActors.Clear();
            }
        }

        public void UpdateLiveActorState(
            LiveWallpaperMap map, int actorIndex, LiveWallpaperActorState state)
        {
            if (!ReferenceEquals(_liveActorMap, map))
            {
                _liveActorMap = map;
                _liveActors.Clear();
            }
            if (actorIndex >= 0 && map != null && actorIndex < map.Actors.Count)
                _liveActors[actorIndex] = state;
        }

        public bool ApplyEnemyHit(LiveWallpaperLinkHit hit, long elapsedMilliseconds)
        {
            if (!hit.Valid || elapsedMilliseconds < _damageUntil)
                return false;
            var direction = _position.Position - new Vector2(
                hit.SourcePixelX, hit.SourcePixelY);
            if (direction.LengthSquared() <= 0.000001f)
                direction = DirectionToVector((_lastRouteDirection + 2) % 4);
            else
                direction.Normalize();
            _hitVelocity += direction * hit.PushMultiplier;
            // ObjLink.BlinkTime (66 ms) * the default GameSettings.DmgCooldown (16).
            _damageUntil = elapsedMilliseconds + 66L * 16L;
            return true;
        }

        public bool IsDamageVisible(long elapsedMilliseconds)
        {
            if (elapsedMilliseconds >= _damageUntil)
                return true;
            var remaining = _damageUntil - elapsedMilliseconds;
            return (remaining / 66L) % 2L != 0;
        }

        public bool TryWalkTo(
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            float targetPixelX,
            float targetPixelY)
        {
            var plan = LiveWallpaperJourneyPlanner.CreateToPoint(
                map, viewport, _position.X, _position.Y,
                targetPixelX, targetPixelY);
            if (plan.Points.Count < 2)
                return false;

            _journeyPlan = plan;
            _journeyOriginX = viewport.OriginX;
            _journeyOriginY = viewport.OriginY;
            _journeyColumns = viewport.Columns;
            _journeyRows = viewport.Rows;
            var firstPoint = plan.Points[0];
            _journeyPointIndex = firstPoint.Action ==
                                     LiveWallpaperJourneyAction.CutBush ||
                                 firstPoint.Action ==
                                     LiveWallpaperJourneyAction.LiftStone ||
                                 firstPoint.Action ==
                                     LiveWallpaperJourneyAction.PushBlock ||
                                 firstPoint.Action ==
                                     LiveWallpaperJourneyAction.PegasusCharge ||
                                 firstPoint.Action ==
                                     LiveWallpaperJourneyAction.OpenChest
                ? 0
                : plan.Points.Count > 1 ? 1 : 0;
            _nextJourneyAt = 0;
            _manualDestinationActive = true;
            _journeyBlockedSince = 0;
            _journeyProgressPointIndex = -1;
            _journeyBestTargetDistance = float.MaxValue;
            _pauseUntil = _lastElapsed ?? 0L;
            _interactionPauseStarted = false;
            _chestPauseStarted = false;
            _chestOpenedAt = 0L;
            _roosterPickupPauseStarted = false;
            _combatPauseStarted = false;
            _runtimeCombatEnemyIndex = -1;
            _pegasusChargePauseStarted = false;
            _pegasusChargeStartedAt = 0;
            _pegasusJumpActive = false;
            _hookshotStarted = false;
            _hookshotPulling = false;
            _hookshotPosition = Vector2.Zero;
            _bushCutStartedAt = 0;
            _runtimeBushCutActive = false;
            _runtimeBushCutDirection = Vector2.Zero;
            _runtimeStoneLiftActive = false;
            _runtimeStoneLiftDirection = Vector2.Zero;
            _activeLiftedStoneKey = -1;
            _journeyExited = false;
            _hitVelocity = Vector2.Zero;
            _airMoveVelocity = Vector2.Zero;
            return true;
        }

        public LiveWallpaperSimulatedLinkState UpdateJourney(
            int scene,
            int activityMode,
            long elapsedMilliseconds,
            bool animated,
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            bool allowIslandLife,
            bool followLoadingZones = false,
            long stoneThrowAnimationMilliseconds = 0L,
            bool allowViewportFollow = false,
            long holeFallAnimationMilliseconds = 0L)
        {
            _currentJourneyMap = map;
            if (stoneThrowAnimationMilliseconds > 0L)
                _stoneThrowAnimationMilliseconds =
                    stoneThrowAnimationMilliseconds;
            if (holeFallAnimationMilliseconds > 0L)
                _holeFallAnimationMilliseconds =
                    holeFallAnimationMilliseconds;
            var elapsedDelta = _lastElapsed.HasValue
                ? elapsedMilliseconds - _lastElapsed.Value
                : 0L;
            var viewportChanged = _journeyOriginX != viewport.OriginX ||
                                  _journeyOriginY != viewport.OriginY ||
                                  _journeyColumns != viewport.Columns ||
                                  _journeyRows != viewport.Rows;
            var sceneChanged = _scene != scene;
            var reset = sceneChanged || elapsedDelta < 0 || elapsedDelta > 1000 ||
                        _journeyPlan == null || _journeyIslandLife != allowIslandLife ||
                        _journeyFollowLoadingZones != followLoadingZones ||
                        _journeyAllowViewportFollow != allowViewportFollow ||
                        viewportChanged && !followLoadingZones &&
                        !allowViewportFollow;
            if (sceneChanged)
            {
                _visitedFieldKeys.Clear();
                _lastRememberedFieldKey = -1;
                _fieldTransitionsWithoutDiscovery = 0;
                _openedChests.Clear();
            }
            _scene = scene;
            _lastElapsed = elapsedMilliseconds;
            ExpireCollectedVegetationDrops(elapsedMilliseconds);
            RespawnDistantObjects(map);
            if (reset)
            {
                _journeyVariant = (int)Math.Max(0, elapsedMilliseconds / 20_000L);
                var continueThroughLoadingZone = _continueFromCurrentOnReset ||
                                                 followLoadingZones &&
                                                 _journeyExited;
                StartJourney(
                    map, viewport, scene, allowIslandLife, elapsedMilliseconds,
                    continueFromCurrentPosition: continueThroughLoadingZone,
                    followLoadingZones: followLoadingZones,
                    allowViewportFollow: allowViewportFollow);
                _continueFromCurrentOnReset = false;
                if (!animated || activityMode == 1)
                    PlaceAtJourneyRestPoint(viewport);
                elapsedDelta = 0;
            }

            var frameScale = Math.Clamp(
                elapsedDelta / (1000f / 60f), 0f, 6f);
            UpdateMoveStoneMotion(map, elapsedMilliseconds);
            // Hole absorption is physical state, not journey state. Process it
            // before the no-route fallback so an unavailable path cannot bypass
            // a fall that has already begun under Link's body.
            UpdateHoleAbsorption(map, frameScale, elapsedMilliseconds);

            if (_journeyPlan == null || _journeyPlan.Points.Count == 0)
            {
                if (animated && activityMode != 1)
                {
                    if (_nextJourneyAt <= 0)
                        _nextJourneyAt = elapsedMilliseconds + 650L;
                    else if (elapsedMilliseconds >= _nextJourneyAt)
                    {
                        _journeyVariant++;
                        StartJourney(
                            map, viewport, scene, allowIslandLife,
                            elapsedMilliseconds,
                            continueFromCurrentPosition: true,
                            followLoadingZones: followLoadingZones,
                            allowViewportFollow: allowViewportFollow);
                    }
                }
                if (_journeyPlan != null && _journeyPlan.Points.Count > 0)
                    _nextJourneyAt = 0;
                else
                {
                    if (_holeFalling || _body.HoleAbsorption != Vector2.Zero)
                    {
                        _journeyPlan = new LiveWallpaperJourneyPlan(
                        [
                            new LiveWallpaperJourneyPoint(
                                _position.X, _position.Y)
                        ]);
                        _journeyPointIndex = 1;
                    }
                    else
                    {
                    var fallback = LiveWallpaperLinkActivity.ResolveForScene(
                        activityMode, scene, elapsedMilliseconds, animated);
                    return Update(scene, fallback, elapsedMilliseconds, animated, map);
                    }
                }
            }

            if (_journeyPointIndex >= _journeyPlan.Points.Count)
            {
                // The arrival route has now genuinely carried Link away from
                // the reciprocal entrance. Later journeys may use that door.
                _entryPortalIdToAvoid = null;
                if (_nextJourneyAt <= 0)
                    _nextJourneyAt = elapsedMilliseconds +
                                     (_journeyExited ? 0L :
                                         _manualDestinationActive ? 4_000L :
                                         activityMode == 2 ? 4_000L : 650L);
                if (animated && activityMode != 1 && elapsedMilliseconds >= _nextJourneyAt)
                {
                    _journeyVariant++;
                    var insideViewport = !_journeyExited &&
                                         IsInsideJourneyBounds(viewport);
                    var continueFromCurrentPosition = insideViewport ||
                                                      followLoadingZones &&
                                                      _journeyExited;
                    StartJourney(
                        map, viewport, scene, allowIslandLife, elapsedMilliseconds,
                        continueFromCurrentPosition: continueFromCurrentPosition,
                        edgeStartOnly: !continueFromCurrentPosition,
                        followLoadingZones: followLoadingZones,
                        allowViewportFollow: allowViewportFollow);
                }
            }

            if (_holeFalling && elapsedMilliseconds - _holeFallStartedAt >=
                    _holeFallAnimationMilliseconds)
            {
                // ObjLink.OnHoleReset restores the saved safe position when the
                // canonical fall animation finishes, then resumes from idle.
                _position.Set(new Vector3(_holeResetPosition, 0f));
                _body.Velocity = Vector3.Zero;
                _body.VelocityTarget = Vector2.Zero;
                _body.HoleAbsorption = Vector2.Zero;
                _body.WasHolePulled = false;
                _body.SpeedMultiply = 1f;
                _body.IsGrounded = true;
                _airMoveVelocity = Vector2.Zero;
                _holeFalling = false;
                _holeFallStartedAt = 0L;
                _journeyVariant++;
                StartJourney(
                    map, viewport, scene, allowIslandLife,
                    elapsedMilliseconds,
                    continueFromCurrentPosition: true,
                    followLoadingZones: followLoadingZones,
                    allowViewportFollow: allowViewportFollow);
                elapsedDelta = 0L;
            }

            var canMove = animated && activityMode != 1 && !_holeFalling &&
                          elapsedMilliseconds >= _pauseUntil &&
                          _journeyPointIndex < _journeyPlan.Points.Count;
            if (_holeFalling)
                canMove = false;
            if (canMove && _body.IsGrounded && map != null)
            {
                var bodyX = _position.X + _body.OffsetX;
                var bodyY = _position.Y + _body.OffsetY;
                var holeCoverage = map.GetLinkHoleCoverage(
                    bodyX, bodyY, _body.Width, _body.Height);
                // ObjHoleTeleporter changes the fall-reset destination. Once
                // the real hole pull has caught Link, locomotion no longer
                // fights that pull toward the journey node he just left.
                if (holeCoverage > _body.AbsorbStop &&
                    map.IntersectsHoleTeleporter(
                        bodyX, bodyY, _body.Width, _body.Height))
                    canMove = false;
            }
            var movementPointIndex = _journeyPointIndex;
            var movementTarget = movementPointIndex >= 0 &&
                                 movementPointIndex < _journeyPlan.Points.Count
                ? new Vector2(
                    _journeyPlan.Points[movementPointIndex].PixelX,
                    _journeyPlan.Points[movementPointIndex].PixelY)
                : _position.Position;
            var attemptedJourneyMovement = canMove && frameScale > 0;
            var inputMove = Vector2.Zero;
            var featherPressed = false;
            var interactionActor = -1;
            var combatEnemy = -1;
            var activeChestKey = -1;
            string chestItemSpriteId = null;
            var chestItemShowAnimation = 1;
            var actionProgress = 0f;
            var action = LiveWallpaperLinkRouteAction.Stand;
            var hookshotVisible = false;
            var targetJourneyAction = _journeyPointIndex < _journeyPlan.Points.Count
                ? _journeyPlan.Points[_journeyPointIndex].Action
                : LiveWallpaperJourneyAction.Walk;
            if (_runtimeBushCutActive && elapsedMilliseconds >= _pauseUntil)
                _runtimeBushCutActive = false;
            if (_runtimeStoneLiftActive && elapsedMilliseconds >= _pauseUntil)
            {
                _runtimeStoneLiftActive = false;
                _activeLiftedStoneKey = -1;
            }
            if (_runtimeCombatEnemyIndex >= 0 &&
                elapsedMilliseconds >= _pauseUntil)
                _runtimeCombatEnemyIndex = -1;
            if (_hitVelocity.LengthSquared() > 0.0025f && frameScale > 0)
            {
                ApplyJourneyConstrainedMovement(
                    map, _hitVelocity *
                         (0.5f + _body.SpeedMultiply * 0.5f) * frameScale,
                    includeHoles: false, includeEnemies: false);
                var hitNormal = Vector2.Normalize(_hitVelocity);
                var slowDownAmount = 0.05f + Math.Clamp(
                    _hitVelocity.Length() / 25f, 0f, 0.05f);
                _hitVelocity -= hitNormal * slowDownAmount * frameScale;
                if (_hitVelocity.Length() < 0.25f)
                    _hitVelocity = Vector2.Zero;
                canMove = false;
            }
            else if (_hitVelocity != Vector2.Zero)
            {
                _hitVelocity = Vector2.Zero;
            }
            if (canMove && frameScale > 0 && targetJourneyAction ==
                    LiveWallpaperJourneyAction.Hookshot)
            {
                var point = _journeyPlan.Points[_journeyPointIndex];
                var landing = new Vector2(point.PixelX, point.PixelY);
                var contact = new Vector2(
                    point.HookshotTargetX, point.HookshotTargetY);
                var fireDirection = contact - _position.Position;
                if (!_hookshotStarted)
                {
                    _hookshotStarted = true;
                    _hookshotPulling = false;
                    _hookshotLinkStart = _position.Position;
                    var fireFacing = ResolveDirection(
                        fireDirection, _lastRouteDirection);
                    _lastRouteDirection = fireFacing;
                    var offset = fireFacing switch
                    {
                        0 => new Vector2(-5f, -4f),
                        1 => new Vector2(-3f, -12f),
                        2 => new Vector2(5f, -4f),
                        _ => new Vector2(3f, 0f)
                    };
                    _hookshotPosition = _position.Position + offset;
                }
                hookshotVisible = true;
                inputMove = contact - _hookshotLinkStart;
                if (inputMove.LengthSquared() > 0.0001f)
                    inputMove.Normalize();
                var hookshotMovement = LinkGameplayMotion.HookshotSpeed *
                                       frameScale;
                if (!_hookshotPulling)
                {
                    var extension = contact - _hookshotPosition;
                    if (extension.LengthSquared() <=
                        hookshotMovement * hookshotMovement)
                    {
                        _hookshotPosition = contact;
                        _hookshotPulling = true;
                    }
                    else
                    {
                        _hookshotPosition += Vector2.Normalize(extension) *
                                             hookshotMovement;
                    }
                }
                if (_hookshotPulling)
                {
                    _hookshotPosition = contact;
                    var pull = landing - _position.Position;
                    if (pull.LengthSquared() <=
                        hookshotMovement * hookshotMovement)
                    {
                        _position.X = landing.X;
                        _position.Y = landing.Y;
                        _hookshotStarted = false;
                        _hookshotPulling = false;
                        OnJourneyPointReached(elapsedMilliseconds);
                    }
                    else
                    {
                        var movement = Vector2.Normalize(pull) *
                                       hookshotMovement;
                        _position.X += movement.X;
                        _position.Y += movement.Y;
                    }
                }
            }
            else if (canMove && frameScale > 0)
            {
                var targetPoint = _journeyPlan.Points[_journeyPointIndex];
                var target = new Vector2(targetPoint.PixelX, targetPoint.PixelY);
                if (targetPoint.Action == LiveWallpaperJourneyAction.Attack &&
                    _journeyPlan.HasCombat &&
                    _journeyPointIndex == _journeyPlan.CombatPointIndex)
                {
                    target = ResolveLiveEnemyApproach(
                        map, _journeyPlan.CombatEnemyIndex, target);
                }
                else if (targetPoint.Action ==
                             LiveWallpaperJourneyAction.Interact &&
                         _journeyPlan.HasInteraction &&
                         _journeyPointIndex ==
                             _journeyPlan.InteractionPointIndex)
                {
                    target = ResolveLiveActorApproach(
                        map, _journeyPlan.InteractionActorIndex, target);
                }
                else if (targetPoint.Action ==
                             LiveWallpaperJourneyAction.CutBush &&
                         targetPoint.BushKey == _pendingVegetationDropKey &&
                         _cutBushes.Contains(targetPoint.BushKey) &&
                         _vegetationDrops.ContainsKey(targetPoint.BushKey))
                {
                    target = GetVegetationDropPosition(
                        map, targetPoint.BushKey, elapsedMilliseconds);
                }
                var difference = target - _position.Position;
                var carrying = IsCarryingRooster();
                var swimming = IsSwimming(map, _position.X, _position.Y);
                var pegasusJump = targetJourneyAction ==
                                      LiveWallpaperJourneyAction.PegasusJump ||
                                  _pegasusJumpActive && !_body.IsGrounded;
                var speed = carrying || swimming
                    ? 0.5f
                    : targetJourneyAction is
                        LiveWallpaperJourneyAction.PegasusDash or
                        LiveWallpaperJourneyAction.PegasusJump || pegasusJump
                        ? LinkGameplayMotion.PegasusBootsSpeed
                        : WalkSpeedPerFrame;
                var maximumMovement = speed * _body.SpeedMultiply * frameScale;
                var interactionReach = targetJourneyAction is
                    LiveWallpaperJourneyAction.CutBush or
                    LiveWallpaperJourneyAction.LiftStone or
                    LiveWallpaperJourneyAction.PegasusCharge or
                    LiveWallpaperJourneyAction.OpenChest;
                var reachDistance = targetJourneyAction ==
                                    LiveWallpaperJourneyAction.PegasusCharge
                    ? Math.Max(maximumMovement, 12f)
                    : interactionReach
                        ? Math.Max(maximumMovement, 4f)
                        : maximumMovement;
                if (targetJourneyAction is
                        LiveWallpaperJourneyAction.FeatherJump or
                        LiveWallpaperJourneyAction.PegasusJump &&
                    _body.IsGrounded && difference.LengthSquared() > 0.0001f)
                {
                    var jumpInput = Vector2.Normalize(difference);
                    _pegasusJumpActive = targetJourneyAction ==
                                         LiveWallpaperJourneyAction.PegasusJump;
                    _body.Velocity.Z = LinkGameplayMotion.FeatherVelocity;
                    _body.IsGrounded = false;
                    _airMoveVelocity = jumpInput * (_pegasusJumpActive
                        ? LinkGameplayMotion.PegasusBootsSpeed
                        : WalkSpeedPerFrame);
                    inputMove = jumpInput;
                    featherPressed = true;
                    // ObjLink receives one complete grounded movement update
                    // on the feather press before SystemBody starts advancing
                    // the airborne arc. A renderer running above 60 Hz has a
                    // fractional frameScale here; without this minimum logical
                    // update, a Pegasus jump loses part of its first two-pixel
                    // step and cannot fully clear the canonical three-tile gap.
                    maximumMovement = Math.Max(
                        maximumMovement,
                        speed * _body.SpeedMultiply);
                }
                var airborne = !_body.IsGrounded || _body.Velocity.Z > 0f;
                if (difference.LengthSquared() <= reachDistance * reachDistance)
                {
                    if (TryStartBlockingBushCut(
                            map, difference, elapsedMilliseconds, out var cutDirection))
                    {
                        inputMove = cutDirection;
                        canMove = false;
                    }
                    else if (TryStartBlockingStoneLift(
                                 map, difference, elapsedMilliseconds,
                                 out var liftDirection))
                    {
                        inputMove = liftDirection;
                        canMove = false;
                    }
                    else if (TryStartBlockingMoveStonePush(
                                 map, difference, elapsedMilliseconds,
                                 out var pushDirection))
                    {
                        inputMove = pushDirection;
                        canMove = false;
                    }
                    else if (TryStartBlockingEnemyAttack(
                                 map, difference, elapsedMilliseconds,
                                 out var attackDirection))
                    {
                        inputMove = attackDirection;
                        canMove = false;
                    }
                    else
                    {
                        var moved = ApplyJourneyConstrainedMovement(
                            map, difference,
                            includeHoles: false);
                        if ((_position.Position - target).LengthSquared() <= 0.01f)
                        {
                            _position.X = target.X;
                            _position.Y = target.Y;
                            OnJourneyPointReached(elapsedMilliseconds);
                        }
                        else if (!moved)
                        {
                            if (interactionReach)
                                OnJourneyPointReached(elapsedMilliseconds);
                            else
                                inputMove = Vector2.Zero;
                        }
                    }
                }
                else
                {
                    inputMove = Vector2.Normalize(difference);
                    var movement = inputMove * maximumMovement;
                    if (airborne)
                    {
                        var airSpeed = _pegasusJumpActive
                            ? LinkGameplayMotion.PegasusBootsSpeed
                            : WalkSpeedPerFrame;
                        _airMoveVelocity = LinkGameplayMotion.ResolveAirVelocity(
                            _airMoveVelocity, inputMove,
                            airSpeed, frameScale);
                        movement = _airMoveVelocity * frameScale;
                    }
                    if (TryStartBlockingBushCut(
                            map, movement, elapsedMilliseconds, out var cutDirection))
                    {
                        inputMove = cutDirection;
                        canMove = false;
                    }
                    else if (TryStartBlockingStoneLift(
                                 map, movement, elapsedMilliseconds,
                                 out var liftDirection))
                    {
                        inputMove = liftDirection;
                        canMove = false;
                    }
                    else if (TryStartBlockingMoveStonePush(
                                 map, movement, elapsedMilliseconds,
                                 out var pushDirection))
                    {
                        inputMove = pushDirection;
                        canMove = false;
                    }
                    else if (TryStartBlockingEnemyAttack(
                                 map, movement, elapsedMilliseconds,
                                 out var attackDirection))
                    {
                        inputMove = attackDirection;
                        canMove = false;
                    }
                    else if (!ApplyJourneyConstrainedMovement(
                                 map, movement,
                                 includeHoles: false))
                        inputMove = Vector2.Zero;
                }
            }

            // SystemBody applies the independently smoothed hole force after
            // ordinary movement, omitting the hole itself from collision tests.
            if (!_holeFalling && _body.IsGrounded &&
                _body.HoleAbsorption != Vector2.Zero && frameScale > 0f)
            {
                ApplyJourneyConstrainedMovement(
                    map, _body.HoleAbsorption * frameScale,
                    includeHoles: false, includeEnemies: true);
            }

            var carryingRooster = IsCarryingRooster();
            if (_roosterReleaseStarted &&
                !_roosterReleaseState.Grounded && frameScale > 0f)
                _roosterReleaseState = RoosterGameplayMotion.AdvanceRelease(
                    _roosterReleaseState, frameScale);
            if (_journeyExited)
            {
                action = LiveWallpaperLinkRouteAction.Hidden;
                inputMove = Vector2.Zero;
            }
            else if (targetJourneyAction ==
                         LiveWallpaperJourneyAction.Hookshot &&
                     (hookshotVisible || _hookshotStarted))
            {
                action = LiveWallpaperLinkRouteAction.Hookshot;
            }
            else if (carryingRooster)
            {
                _roosterFlightHeight = RoosterGameplayMotion.AdvanceFlightHeight(
                    Math.Max(_roosterFlightHeight,
                        RoosterGameplayMotion.CarryHeight),
                    elapsedMilliseconds, frameScale);
                _position.Z = Math.Max(
                    0f, _roosterFlightHeight -
                        RoosterGameplayMotion.CarryHeight);
                _body.IsGrounded = false;
                _body.Velocity.Z = 0;
                action = LiveWallpaperLinkRouteAction.RoosterFly;
            }
            else if (_roosterReleaseStarted &&
                     elapsedMilliseconds - _roosterReleaseStartedAt <
                     _stoneThrowAnimationMilliseconds)
            {
                action = LiveWallpaperLinkRouteAction.RoosterThrow;
                actionProgress = Math.Clamp(
                    (elapsedMilliseconds - _roosterReleaseStartedAt) /
                    (float)_stoneThrowAnimationMilliseconds, 0f, 1f);
            }
            else
            {
                if ((!_body.IsGrounded || _body.Velocity.Z > 0f) &&
                    frameScale > 0)
                {
                    // ObjLink handles the feather press after SystemBody's
                    // grounded movement for that update. Preserve that takeoff
                    // update before consuming the 31-update airborne arc.
                    var airborneFrameScale = Math.Max(
                        0f, frameScale - (featherPressed ? 1f : 0f));
                    if (featherPressed ||
                        AdvanceFeatherHeight(airborneFrameScale))
                        action = _pegasusJumpActive
                            ? LiveWallpaperLinkRouteAction.PegasusJump
                            : LiveWallpaperLinkRouteAction.FeatherJump;
                }
                else if (_body.IsGrounded)
                {
                    _position.Z = 0;
                    _pegasusJumpActive = false;
                }
                if (_pauseUntil > elapsedMilliseconds &&
                    _runtimeCombatEnemyIndex >= 0)
                {
                    action = LiveWallpaperLinkRouteAction.Attack;
                    combatEnemy = _runtimeCombatEnemyIndex;
                    inputMove = FaceEnemy(map, combatEnemy);
                    actionProgress = Math.Clamp(
                        (elapsedMilliseconds - _combatStartedAt) / 233f, 0f, 1f);
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                    _journeyPlan.HasCombat && _combatPauseStarted &&
                    _journeyPointIndex == _journeyPlan.CombatPointIndex)
                {
                    action = LiveWallpaperLinkRouteAction.Attack;
                    combatEnemy = _journeyPlan.CombatEnemyIndex;
                    inputMove = FaceEnemy(map, combatEnemy);
                    actionProgress = Math.Clamp(
                        (elapsedMilliseconds - _combatStartedAt) / 233f, 0f, 1f);
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                    (_runtimeBushCutActive ||
                     targetJourneyAction == LiveWallpaperJourneyAction.CutBush))
                {
                    action = LiveWallpaperLinkRouteAction.Attack;
                    actionProgress = Math.Clamp(
                        (elapsedMilliseconds - _bushCutStartedAt) / 233f, 0f, 1f);
                    if (_runtimeBushCutActive &&
                        _runtimeBushCutDirection.LengthSquared() > 0.0001f)
                    {
                        inputMove = _runtimeBushCutDirection;
                    }
                    else if (_journeyPointIndex + 1 < _journeyPlan.Points.Count)
                    {
                        var next = _journeyPlan.Points[_journeyPointIndex + 1];
                        inputMove = new Vector2(
                            next.PixelX - _position.X, next.PixelY - _position.Y);
                        if (inputMove.LengthSquared() > 0.0001f)
                            inputMove.Normalize();
                    }
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                         _activeMoveStoneKey >= 0)
                {
                    action = LiveWallpaperLinkRouteAction.Pushing;
                    actionProgress = Math.Clamp(
                        (elapsedMilliseconds - _moveStonePushStartedAt) /
                        (float)(MoveStoneInertiaMilliseconds +
                                MoveStoneMovementMilliseconds), 0f, 1f);
                    inputMove = DirectionToVector(_lastRouteDirection);
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                    (_runtimeStoneLiftActive ||
                          targetJourneyAction ==
                              LiveWallpaperJourneyAction.LiftStone))
                {
                    var stoneElapsed = Math.Max(
                        0L, elapsedMilliseconds - _stoneLiftStartedAt);
                    var throwStart = StonePullMilliseconds +
                                     StonePreCarryMilliseconds +
                                     StoneThrowInputDelayMilliseconds;
                    action = stoneElapsed < StonePullMilliseconds + 100L
                        ? LiveWallpaperLinkRouteAction.LiftStone
                        : stoneElapsed < throwStart
                            ? LiveWallpaperLinkRouteAction.CarryStone
                            : stoneElapsed < throwStart +
                              _stoneThrowAnimationMilliseconds
                                ? LiveWallpaperLinkRouteAction.ThrowStone
                                : LiveWallpaperLinkRouteAction.Stand;
                    actionProgress = action switch
                    {
                        LiveWallpaperLinkRouteAction.LiftStone => Math.Clamp(
                            stoneElapsed /
                            (float)(StonePullMilliseconds + 100L), 0f, 1f),
                        LiveWallpaperLinkRouteAction.CarryStone => Math.Clamp(
                            (stoneElapsed - StonePullMilliseconds - 100L) /
                            100f, 0f, 1f),
                        LiveWallpaperLinkRouteAction.ThrowStone => Math.Clamp(
                            (stoneElapsed - throwStart) /
                            (float)_stoneThrowAnimationMilliseconds, 0f, 1f),
                        _ => 1f
                    };
                    inputMove = _runtimeStoneLiftDirection;
                    if (inputMove.LengthSquared() <= 0.0001f &&
                        _journeyPointIndex + 1 < _journeyPlan.Points.Count)
                    {
                        var next = _journeyPlan.Points[_journeyPointIndex + 1];
                        inputMove = new Vector2(
                            next.PixelX - _position.X,
                            next.PixelY - _position.Y);
                        if (inputMove.LengthSquared() > 0.0001f)
                            inputMove.Normalize();
                    }
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                    _pegasusChargePauseStarted &&
                    targetJourneyAction ==
                        LiveWallpaperJourneyAction.PegasusCharge)
                {
                    action = LiveWallpaperLinkRouteAction.PegasusCharge;
                    actionProgress = Math.Clamp(
                        (elapsedMilliseconds - _pegasusChargeStartedAt) /
                        LinkGameplayMotion.PegasusBootsChargeMilliseconds,
                        0f, 1f);
                    if (_journeyPointIndex + 1 < _journeyPlan.Points.Count)
                    {
                        var next = _journeyPlan.Points[_journeyPointIndex + 1];
                        inputMove = new Vector2(
                            next.PixelX - _position.X,
                            next.PixelY - _position.Y);
                        if (inputMove.LengthSquared() > 0.0001f)
                            inputMove.Normalize();
                    }
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                    _chestPauseStarted &&
                    targetJourneyAction == LiveWallpaperJourneyAction.OpenChest)
                {
                    var chestPoint = _journeyPlan.Points[_journeyPointIndex];
                    var chestElapsed = Math.Max(
                        0L, elapsedMilliseconds - _chestOpenedAt);
                    if (LiveWallpaperChestItem.TryResolve(
                            chestPoint.ChestItemName, out var chestVisual))
                    {
                        activeChestKey = chestPoint.ChestKey;
                        chestItemSpriteId = chestVisual.SpriteId;
                        chestItemShowAnimation = chestVisual.ShowAnimation;
                    }
                    inputMove = -Vector2.UnitY;
                    if (chestElapsed < ChestGameplayPresentation.OpeningMilliseconds)
                    {
                        action = LiveWallpaperLinkRouteAction.OpenChest;
                        actionProgress = Math.Clamp(
                            chestElapsed /
                            (float)ChestGameplayPresentation.OpeningMilliseconds,
                            0f, 1f);
                    }
                    else
                    {
                        action = LiveWallpaperLinkRouteAction.ShowItem;
                        actionProgress = Math.Clamp(
                            (chestElapsed -
                             ChestGameplayPresentation.OpeningMilliseconds) /
                            (float)LiveWallpaperChestItem.PresentationMilliseconds,
                            0f, 1f);
                    }
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                    _journeyPlan.HasRoosterFlight && _roosterPickupPauseStarted &&
                    _journeyPointIndex == _journeyPlan.RoosterPickupPointIndex)
                {
                    action = LiveWallpaperLinkRouteAction.RoosterPickup;
                    actionProgress = Math.Clamp(
                        (elapsedMilliseconds - _roosterPickupStartedAt) /
                        (float)RoosterGameplayMotion.PickupSequenceMilliseconds,
                        0f, 1f);
                }
                else if (_pauseUntil > elapsedMilliseconds &&
                    _journeyPlan.HasInteraction && _interactionPauseStarted &&
                    _journeyPointIndex == _journeyPlan.InteractionPointIndex)
                {
                    action = LiveWallpaperLinkRouteAction.Interact;
                    interactionActor = _journeyPlan.InteractionActorIndex;
                    inputMove = FaceActor(map, interactionActor);
                }
                else if (inputMove != Vector2.Zero && _body.IsGrounded)
                    action = targetJourneyAction ==
                                 LiveWallpaperJourneyAction.PegasusDash
                        ? LiveWallpaperLinkRouteAction.PegasusDash
                        : targetJourneyAction is
                            LiveWallpaperJourneyAction.FeatherJump or
                            LiveWallpaperJourneyAction.PegasusJump
                        ? targetJourneyAction ==
                              LiveWallpaperJourneyAction.PegasusJump
                            ? LiveWallpaperLinkRouteAction.PegasusJump
                            : LiveWallpaperLinkRouteAction.FeatherJump
                        : targetJourneyAction == LiveWallpaperJourneyAction.Swim ||
                          IsSwimming(map, _position.X, _position.Y)
                            ? LiveWallpaperLinkRouteAction.Swim
                            : LiveWallpaperLinkRouteAction.Walk;
                else if (_body.IsGrounded && IsSwimming(map, _position.X, _position.Y))
                    action = LiveWallpaperLinkRouteAction.Swim;
            }

            if (!_holeFalling && _body.IsGrounded &&
                map?.GetLinkHoleCoverage(
                    _position.X + _body.OffsetX,
                    _position.Y + _body.OffsetY,
                    _body.Width, _body.Height) >= _body.AbsorbPercentage)
            {
                BeginHoleFall(elapsedMilliseconds);
            }
            if (_holeFalling)
            {
                action = LiveWallpaperLinkRouteAction.Falling;
                actionProgress = Math.Clamp(
                    (elapsedMilliseconds - _holeFallStartedAt) /
                    (float)Math.Max(1L, _holeFallAnimationMilliseconds),
                    0f, 1f);
                inputMove = Vector2.Zero;
                featherPressed = false;
                interactionActor = -1;
                combatEnemy = -1;
                hookshotVisible = false;
            }
            else if (_body.IsGrounded)
            {
                UpdateHoleResetPosition(map);
            }

            var fallbackDirection = _lastRouteDirection;
            var direction = ResolveDirection(inputMove, fallbackDirection);
            if (interactionActor >= 0)
                direction = ResolveDirection(FaceActor(map, interactionActor), fallbackDirection);
            else if (combatEnemy >= 0)
                direction = ResolveDirection(FaceEnemy(map, combatEnemy), fallbackDirection);
            _lastRouteDirection = direction;
            if (_journeyPointIndex != movementPointIndex)
            {
                _journeyBlockedSince = 0;
                _journeyProgressPointIndex = -1;
                _journeyBestTargetDistance = float.MaxValue;
            }
            else if (attemptedJourneyMovement && !_runtimeBushCutActive &&
                     !_runtimeStoneLiftActive && _activeMoveStoneKey < 0 &&
                     targetJourneyAction != LiveWallpaperJourneyAction.Hookshot)
            {
                var targetDistance = Vector2.Distance(
                    _position.Position, movementTarget);
                if (_journeyProgressPointIndex != movementPointIndex ||
                    targetDistance + 0.25f < _journeyBestTargetDistance)
                {
                    _journeyProgressPointIndex = movementPointIndex;
                    _journeyBestTargetDistance = targetDistance;
                    _journeyBlockedSince = elapsedMilliseconds;
                }
                else if (_journeyBlockedSince <= 0)
                {
                    _journeyBlockedSince = elapsedMilliseconds;
                }
                else if (elapsedMilliseconds - _journeyBlockedSince >= 2_500L)
                {
                    var holeCoverage = map?.GetLinkHoleCoverage(
                        _position.X + _body.OffsetX,
                        _position.Y + _body.OffsetY,
                        _body.Width, _body.Height) ?? 0f;
                    if (_body.IsGrounded &&
                        holeCoverage > _body.AbsorbStop)
                    {
                        // SystemBody would continuously pull this overlap toward
                        // full absorption. The wallpaper has no hidden gameplay
                        // loop, so its existing stuck timeout advances to the
                        // same ObjLink fall/reset outcome instead of replanning
                        // forever from inside the hole.
                        BeginHoleFall(elapsedMilliseconds);
                        action = LiveWallpaperLinkRouteAction.Falling;
                        actionProgress = 0f;
                    }
                    else
                    {
                        _journeyVariant++;
                        StartJourney(
                            map, viewport, scene, allowIslandLife,
                            elapsedMilliseconds,
                            continueFromCurrentPosition: true,
                            followLoadingZones: followLoadingZones,
                            allowViewportFollow: allowViewportFollow);
                        action = LiveWallpaperLinkRouteAction.Stand;
                    }
                    inputMove = Vector2.Zero;
                    direction = _lastRouteDirection;
                }
            }
            else
            {
                _journeyBlockedSince = 0;
                _journeyProgressPointIndex = -1;
                _journeyBestTargetDistance = float.MaxValue;
            }
            var roosterVisible = _journeyPlan.HasRoosterFlight;
            ResolveRoosterState(carryingRooster, elapsedMilliseconds,
                out var roosterX, out var roosterY, out var roosterHeight);
            ResolveActiveStoneState(
                map, elapsedMilliseconds,
                out var activeStoneEntityX,
                out var activeStoneEntityY,
                out var activeStoneHeight,
                out var activeStoneReleased);
            return new LiveWallpaperSimulatedLinkState(
                _position.X / TileSize, _position.Y / TileSize, _position.Z,
                direction, action,
                new LiveWallpaperLinkInput(inputMove, featherPressed),
                interactionActor, roosterVisible, carryingRooster,
                roosterX / TileSize, roosterY / TileSize, roosterHeight,
                combatEnemy, actionProgress, cutBushes: _cutBushes,
                cutVegetationTimes: _cutVegetationTimes,
                vegetationDrops: _vegetationDrops,
                vegetationDropDirections: _vegetationDropDirections,
                collectedVegetationDropTimes: _collectedVegetationDropTimes,
                liftedStones: _liftedStones,
                activeLiftedStoneKey: _activeLiftedStoneKey,
                activeStoneEntityX: activeStoneEntityX,
                activeStoneEntityY: activeStoneEntityY,
                activeStoneHeight: activeStoneHeight,
                stoneImpactKind: _stoneImpactKind,
                stoneImpactX: _stoneImpactX,
                stoneImpactY: _stoneImpactY,
                stoneImpactStartedAt: _stoneImpactStartedAt,
                stoneImpactSerial: _stoneImpactSerial,
                stoneImpactEnemyIndex: _stoneImpactEnemyIndex,
                collectedRupees: _collectedRupees,
                collectedHearts: _collectedHearts,
                activeStoneReleased: activeStoneReleased,
                hookshotVisible: hookshotVisible,
                hookshotMapX: _hookshotPosition.X / TileSize,
                hookshotMapY: _hookshotPosition.Y / TileSize,
                openedChests: _openedChests,
                activeChestKey: activeChestKey,
                chestItemSpriteId: chestItemSpriteId,
                chestItemShowAnimation: chestItemShowAnimation,
                moveStones: _moveStones,
                fallenMoveStones: _fallenMoveStones);
        }

        private void StartJourney(
            LiveWallpaperMap map,
            LiveWallpaperMapViewport viewport,
            int scene,
            bool allowIslandLife,
            long elapsedMilliseconds,
            bool continueFromCurrentPosition = false,
            bool edgeStartOnly = false,
            bool followLoadingZones = false,
            bool allowViewportFollow = false)
        {
            if (followLoadingZones && continueFromCurrentPosition)
                RememberCurrentField();
            _journeyPlan = LiveWallpaperJourneyPlanner.Create(
                map, viewport, scene, _journeyVariant, allowIslandLife,
                continueFromCurrentPosition ? _position.X : null,
                continueFromCurrentPosition ? _position.Y : null,
                edgeStartOnly, followLoadingZones,
                followLoadingZones ? _visitedFieldKeys : null,
                _entryPortalIdToAvoid, _openedChests);
            // ObjDoor marks the arrival door as already colliding while Link is
            // placed on the new map. Give the autonomous wallpaper one complete
            // route away from that entrance before it may choose the reciprocal
            // door again. If the room genuinely has no other route, retry without
            // the latch so a one-exit cave cannot trap Link forever.
            if (_entryPortalIdToAvoid != null &&
                _journeyPlan.Points.Count == 0)
            {
                _journeyPlan = LiveWallpaperJourneyPlanner.Create(
                    map, viewport, scene, _journeyVariant, allowIslandLife,
                    continueFromCurrentPosition ? _position.X : null,
                    continueFromCurrentPosition ? _position.Y : null,
                    edgeStartOnly, followLoadingZones,
                    followLoadingZones ? _visitedFieldKeys : null,
                    openedChests: _openedChests);
                _entryPortalIdToAvoid = null;
            }
            _journeyIslandLife = allowIslandLife;
            _journeyFollowLoadingZones = followLoadingZones;
            _journeyAllowViewportFollow = allowViewportFollow;
            _journeyOriginX = viewport.OriginX;
            _journeyOriginY = viewport.OriginY;
            _journeyColumns = viewport.Columns;
            _journeyRows = viewport.Rows;
            // The planner can place a bush cut on the starting point when Link
            // begins directly beside a bush. Skipping point zero made him walk
            // into that collider and stand there forever without swinging.
            if (_journeyPlan.Points.Count > 0)
            {
                var firstPoint = _journeyPlan.Points[0];
                _journeyPointIndex = firstPoint.Action ==
                                     LiveWallpaperJourneyAction.CutBush ||
                                     firstPoint.Action ==
                                     LiveWallpaperJourneyAction.LiftStone ||
                                 firstPoint.Action ==
                                     LiveWallpaperJourneyAction.PushBlock ||
                                 firstPoint.Action ==
                                         LiveWallpaperJourneyAction.PegasusCharge ||
                                     firstPoint.Action ==
                                         LiveWallpaperJourneyAction.OpenChest
                    ? 0
                    : _journeyPlan.Points.Count > 1 ? 1 : 0;
            }
            else
            {
                _journeyPointIndex = 0;
            }
            _nextJourneyAt = 0;
            _manualDestinationActive = false;
            _journeyBlockedSince = 0;
            _journeyProgressPointIndex = -1;
            _journeyBestTargetDistance = float.MaxValue;
            _pauseUntil = elapsedMilliseconds;
            _interactionPauseStarted = false;
            _chestPauseStarted = false;
            _chestOpenedAt = 0L;
            _roosterPickupPauseStarted = false;
            _roosterPickupStartedAt = 0;
            _roosterFlightHeight = 0f;
            _roosterReleaseStarted = false;
            _roosterReleaseStartedAt = 0L;
            _roosterReleaseState = default;
            _combatPauseStarted = false;
            _combatStartedAt = 0;
            _runtimeCombatEnemyIndex = -1;
            _pegasusChargePauseStarted = false;
            _pegasusChargeStartedAt = 0;
            _pegasusJumpActive = false;
            _hookshotStarted = false;
            _hookshotPulling = false;
            _hookshotPosition = Vector2.Zero;
            _bushCutStartedAt = 0;
            _runtimeBushCutActive = false;
            _runtimeBushCutDirection = Vector2.Zero;
            _runtimeStoneLiftActive = false;
            _runtimeStoneLiftDirection = Vector2.Zero;
            _activeLiftedStoneKey = -1;
            _hitVelocity = Vector2.Zero;
            _airMoveVelocity = Vector2.Zero;
            _damageUntil = 0;
            _journeyExited = false;
            if (_journeyPlan.Points.Count > 0 && !continueFromCurrentPosition)
            {
                var start = _journeyPlan.Points[0];
                _position.Set(new Vector3(start.PixelX, start.PixelY, 0));
            }
            if (followLoadingZones)
                RememberCurrentField();
            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;
            _body.IsGrounded = true;
        }

        private void RememberCurrentField()
        {
            var key = LiveWallpaperJourneyPlanner.GetOverworldFieldKey(
                _position.X, _position.Y);
            if (key == _lastRememberedFieldKey)
                return;
            _lastRememberedFieldKey = key;
            if (_visitedFieldKeys.Add(key))
            {
                _fieldTransitionsWithoutDiscovery = 0;
                return;
            }

            _fieldTransitionsWithoutDiscovery++;
            if (_fieldTransitionsWithoutDiscovery <
                MaximumFieldTransitionsWithoutDiscovery)
                return;

            // Coverage is a route preference, not permanent state. A deterministic
            // sequence can otherwise exhaust the local choices and repeat the same
            // handful of fields forever. Forget the old coverage after enough
            // revisits so the next variant may take a different branch.
            _visitedFieldKeys.Clear();
            _visitedFieldKeys.Add(key);
            _fieldTransitionsWithoutDiscovery = 0;
        }

        private void RespawnDistantObjects(LiveWallpaperMap map)
        {
            if (map == null || map.Width <= 0)
                return;
            var currentFieldX = (int)MathF.Floor(_position.X / 160f);
            var currentFieldY = (int)MathF.Floor(_position.Y / 128f);
            RespawnDistantKeys(
                map, _cutBushes, _cutVegetationTimes,
                currentFieldX, currentFieldY, activeKey: -1);
            if (_vegetationDrops.Count > 0)
            {
                var staleDrops = new List<int>();
                foreach (var key in _vegetationDrops.Keys)
                {
                    if (!_cutBushes.Contains(key))
                        staleDrops.Add(key);
                }
                foreach (var key in staleDrops)
                {
                    _vegetationDrops.Remove(key);
                    _vegetationDropDirections.Remove(key);
                    _collectedVegetationDropTimes.Remove(key);
                    if (_pendingVegetationDropKey == key)
                        _pendingVegetationDropKey = -1;
                }
            }
            RespawnDistantKeys(
                map, _liftedStones, _liftedStoneTimes,
                currentFieldX, currentFieldY, _activeLiftedStoneKey);
        }

        private void RollVegetationDrop(int key, Vector2 direction)
        {
            _collectedVegetationDropTimes.Remove(key);
            var itemName = BushDropRules.Roll(_vegetationRandom.Next);
            var dropKind = itemName switch
            {
                BushDropRules.HeartItemName =>
                    LiveWallpaperVegetationDropKind.Heart,
                BushDropRules.RupeeItemName =>
                    LiveWallpaperVegetationDropKind.Rupee,
                _ => LiveWallpaperVegetationDropKind.None
            };
            if (dropKind == LiveWallpaperVegetationDropKind.None)
            {
                _vegetationDrops.Remove(key);
                _vegetationDropDirections.Remove(key);
                return;
            }
            _vegetationDrops[key] = dropKind;
            if (direction.LengthSquared() > 1f)
                direction.Normalize();
            _vegetationDropDirections[key] = direction;
            _pendingVegetationDropKey = key;
        }

        private void ExpireCollectedVegetationDrops(long elapsedMilliseconds)
        {
            if (_collectedVegetationDropTimes.Count == 0)
                return;
            var expired = new List<int>();
            foreach (var pair in _collectedVegetationDropTimes)
            {
                if (elapsedMilliseconds - pair.Value >
                    DroppedItemMotion.CollectionDespawnMilliseconds)
                    expired.Add(pair.Key);
            }
            foreach (var key in expired)
            {
                _collectedVegetationDropTimes.Remove(key);
                _vegetationDrops.Remove(key);
                _vegetationDropDirections.Remove(key);
            }
        }

        private Vector2 GetVegetationDropPosition(
            LiveWallpaperMap map, int key, long elapsedMilliseconds)
        {
            var tileX = key % map.Width;
            var tileY = key / map.Width;
            // ObjItem created by ObjBush anchors at the cut tile's (+8,+11).
            var position = new Vector2(
                tileX * TileSize + 8f, tileY * TileSize + 11f);
            if (_cutVegetationTimes.TryGetValue(key, out var cutAt) &&
                _vegetationDropDirections.TryGetValue(key, out var direction))
                position += DroppedItemMotion.Resolve(
                    direction, Math.Max(0L, elapsedMilliseconds - cutAt)).Offset;
            return position;
        }

        private static void RespawnDistantKeys(
            LiveWallpaperMap map, HashSet<int> removed,
            Dictionary<int, long> removedAt,
            int currentFieldX, int currentFieldY, int activeKey)
        {
            if (removed.Count == 0)
                return;
            var respawn = new List<int>();
            foreach (var key in removed)
            {
                if (key == activeKey)
                    continue;
                var tileX = key % map.Width;
                var tileY = key / map.Width;
                var fieldX = tileX / 10;
                var fieldY = tileY / 8;
                // Smooth-camera Map.GetUpdateState changes only after Link is at
                // least three 10x8-tile fields away from the object's field.
                if (Math.Abs(fieldX - currentFieldX) >= 3 ||
                    Math.Abs(fieldY - currentFieldY) >= 3)
                    respawn.Add(key);
            }
            foreach (var key in respawn)
            {
                removed.Remove(key);
                removedAt.Remove(key);
            }
        }

        private bool IsInsideJourneyBounds(LiveWallpaperMapViewport viewport)
        {
            var minimumX = viewport.OriginX * TileSize + 8f;
            var minimumY = viewport.OriginY * TileSize + 8f;
            var maximumX =
                (viewport.OriginX + viewport.Columns) * TileSize - 8f;
            var maximumY =
                (viewport.OriginY + viewport.Rows) * TileSize - 8f;
            return _position.X >= minimumX && _position.X <= maximumX &&
                   _position.Y >= minimumY && _position.Y <= maximumY;
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
            if (_journeyPointIndex == _journeyPlan.Points.Count - 1 &&
                _journeyPlan.Points[_journeyPointIndex].Action ==
                    LiveWallpaperJourneyAction.Exit)
            {
                _journeyExited = true;
                _journeyPointIndex++;
                return;
            }
            var point = _journeyPlan.Points[_journeyPointIndex];
            if (point.Action == LiveWallpaperJourneyAction.CutBush &&
                point.BushKey == _pendingVegetationDropKey &&
                _cutBushes.Contains(point.BushKey) &&
                _vegetationDrops.TryGetValue(
                    point.BushKey, out var pendingDrop))
            {
                _pendingVegetationDropKey = -1;
                _collectedVegetationDropTimes[point.BushKey] =
                    elapsedMilliseconds;
                if (pendingDrop == LiveWallpaperVegetationDropKind.Rupee)
                    _collectedRupees++;
                else if (pendingDrop == LiveWallpaperVegetationDropKind.Heart)
                    _collectedHearts++;
                _journeyPointIndex++;
                return;
            }
            if (point.Action == LiveWallpaperJourneyAction.CutBush &&
                point.BushKey >= 0 && !_cutBushes.Contains(point.BushKey))
            {
                _cutBushes.Add(point.BushKey);
                _cutVegetationTimes[point.BushKey] = elapsedMilliseconds;
                RollVegetationDrop(
                    point.BushKey, DirectionToVector(_lastRouteDirection));
                _bushCutStartedAt = elapsedMilliseconds;
                _runtimeBushCutActive = true;
                _pauseUntil = elapsedMilliseconds + 233L;
                return;
            }
            if (point.Action == LiveWallpaperJourneyAction.LiftStone &&
                point.StoneKey >= 0 && !_liftedStones.Contains(point.StoneKey))
            {
                _liftedStones.Add(point.StoneKey);
                _liftedStoneTimes[point.StoneKey] = elapsedMilliseconds;
                _activeLiftedStoneKey = point.StoneKey;
                _stoneLiftStartedAt = elapsedMilliseconds;
                _runtimeStoneLiftActive = true;
                _runtimeStoneLiftDirection = ResolveStoneThrowDirection();
                _stoneImpactKind = LiveWallpaperStoneImpactKind.None;
                _stoneImpactEnemyIndex = -1;
                _pauseUntil = elapsedMilliseconds + StoneSequenceMilliseconds;
                return;
            }
            if (point.Action == LiveWallpaperJourneyAction.PushBlock &&
                point.MoveStoneKey >= 0 && _activeMoveStoneKey < 0)
            {
                var direction = ResolveMoveStonePushDirection(
                    _currentJourneyMap, point.MoveStoneKey);
                if (TryStartMoveStonePush(
                        _currentJourneyMap, point.MoveStoneKey, direction,
                        elapsedMilliseconds, journeyAction: true))
                    return;
                // A direction-mask or destination change can invalidate a
                // precomputed push. Advance and let the normal stuck/replan
                // guard choose another route instead of retrying forever.
                _journeyPointIndex++;
                return;
            }
            if (point.Action == LiveWallpaperJourneyAction.PegasusCharge &&
                !_pegasusChargePauseStarted)
            {
                _pegasusChargePauseStarted = true;
                _pegasusChargeStartedAt = elapsedMilliseconds;
                _pauseUntil = elapsedMilliseconds +
                    (long)LinkGameplayMotion.PegasusBootsChargeMilliseconds;
                return;
            }
            if (point.Action == LiveWallpaperJourneyAction.PegasusCharge)
            {
                _pegasusChargePauseStarted = false;
                _journeyPointIndex++;
                return;
            }
            if (point.Action == LiveWallpaperJourneyAction.OpenChest &&
                point.ChestKey >= 0 && !_openedChests.Contains(point.ChestKey))
            {
                _openedChests.Add(point.ChestKey);
                _chestPauseStarted = true;
                _chestOpenedAt = elapsedMilliseconds;
                _pauseUntil = elapsedMilliseconds +
                    ChestGameplayPresentation.OpeningMilliseconds +
                    LiveWallpaperChestItem.PresentationMilliseconds;
                return;
            }
            if (point.Action == LiveWallpaperJourneyAction.OpenChest)
            {
                _chestPauseStarted = false;
                _chestOpenedAt = 0L;
                _journeyPointIndex++;
                return;
            }
            if (_journeyPlan.HasCombat &&
                _journeyPointIndex == _journeyPlan.CombatPointIndex &&
                !_combatPauseStarted)
            {
                _combatPauseStarted = true;
                _combatStartedAt = elapsedMilliseconds;
                // link0.ani attack frames last 50 + 50 + 133 ms. ObjLink leaves
                // its attacking state when that one-shot body animation ends.
                _pauseUntil = elapsedMilliseconds + 233L;
                return;
            }
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
                _roosterPickupStartedAt = elapsedMilliseconds;
                _roosterFlightHeight = 0f;
                _pauseUntil = elapsedMilliseconds +
                              RoosterGameplayMotion.PickupSequenceMilliseconds;
                return;
            }
            if (_journeyPlan.HasRoosterFlight &&
                _journeyPointIndex == _journeyPlan.RoosterLandingPointIndex &&
                _roosterPickupPauseStarted && !_roosterReleaseStarted)
            {
                _roosterReleaseStarted = true;
                _roosterReleaseStartedAt = elapsedMilliseconds;
                var releaseDirection = DirectionToVector(_lastRouteDirection);
                _roosterReleaseState = new RoosterReleaseMotionState(
                    _position.Position,
                    Math.Max(_roosterFlightHeight,
                        RoosterGameplayMotion.CarryHeight),
                    releaseDirection * StoneGameplayMotion.ThrowSpeed,
                    0f, grounded: false);
                _body.IsGrounded = false;
                _body.Velocity.Z = 0f;
                _journeyPointIndex++;
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
            bool carrying, long elapsedMilliseconds,
            out float pixelX, out float pixelY, out float height)
        {
            pixelX = _position.X;
            pixelY = _position.Y;
            height = 0;
            if (_journeyPlan?.HasRoosterFlight != true)
                return;
            if (_roosterReleaseStarted)
            {
                pixelX = _roosterReleaseState.Position.X;
                pixelY = _roosterReleaseState.Position.Y;
                height = _roosterReleaseState.Height;
                return;
            }
            if (carrying)
            {
                height = Math.Max(
                    _roosterFlightHeight,
                    _position.Z + RoosterGameplayMotion.CarryHeight);
                return;
            }
            if (_roosterPickupPauseStarted &&
                _journeyPointIndex == _journeyPlan.RoosterPickupPointIndex &&
                elapsedMilliseconds < _pauseUntil)
            {
                var pickupPoint = _journeyPlan.Points[
                    _journeyPlan.RoosterPickupPointIndex];
                var carried = RoosterGameplayMotion.ResolvePickupPosition(
                    new Vector3(
                        pickupPoint.PixelX, pickupPoint.PixelY, 0f),
                    new Vector3(_position.X, _position.Y, _position.Z),
                    elapsedMilliseconds - _roosterPickupStartedAt);
                pixelX = carried.X;
                pixelY = carried.Y;
                height = carried.Z;
                _roosterFlightHeight = height;
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
            var actorX = actor.BodyX + actor.BodyWidth / 2f;
            var actorY = actor.BodyY + actor.BodyHeight / 2f;
            if (ReferenceEquals(_liveActorMap, map) &&
                _liveActors.TryGetValue(actorIndex, out var state) &&
                TryGetLiveActorBody(actor, state, out var liveBody))
            {
                actorX = liveBody.X + liveBody.Width / 2f;
                actorY = liveBody.Y + liveBody.Height / 2f;
            }
            var difference = new Vector2(
                actorX - _position.X, actorY - _position.Y);
            return difference.LengthSquared() > 0.0001f
                ? Vector2.Normalize(difference)
                : Vector2.Zero;
        }

        private Vector2 FaceEnemy(LiveWallpaperMap map, int enemyIndex)
        {
            if (map == null || enemyIndex < 0 || enemyIndex >= map.Enemies.Count)
                return Vector2.Zero;
            var enemy = map.Enemies[enemyIndex];
            var enemyX = enemy.BodyX + enemy.BodyWidth / 2f;
            var enemyY = enemy.BodyY + enemy.BodyHeight / 2f;
            if (ReferenceEquals(_liveEnemyMap, map) &&
                _liveEnemies.TryGetValue(enemyIndex, out var state) && state.Visible)
            {
                enemyX = state.PixelX + enemy.BodyX - enemy.EntityX +
                         enemy.BodyWidth / 2f;
                enemyY = state.PixelY + enemy.BodyY - enemy.EntityY +
                         enemy.BodyHeight / 2f;
            }
            var difference = new Vector2(
                enemyX - _position.X, enemyY - _position.Y);
            return difference.LengthSquared() > 0.0001f
                ? Vector2.Normalize(difference)
                : Vector2.Zero;
        }

        private Vector2 ResolveLiveEnemyApproach(
            LiveWallpaperMap map, int enemyIndex, Vector2 fallback)
        {
            if (map == null || enemyIndex < 0 || enemyIndex >= map.Enemies.Count ||
                !ReferenceEquals(_liveEnemyMap, map) ||
                !_liveEnemies.TryGetValue(enemyIndex, out var state) || !state.Visible)
                return fallback;
            var enemy = map.Enemies[enemyIndex];
            var bodyX = state.PixelX + enemy.BodyX - enemy.EntityX;
            var bodyY = state.PixelY + enemy.BodyY - enemy.EntityY;
            var centerX = bodyX + enemy.BodyWidth / 2f;
            var centerY = bodyY + enemy.BodyHeight / 2f;
            var originalCenterX = enemy.BodyX + enemy.BodyWidth / 2f;
            var originalCenterY = enemy.BodyY + enemy.BodyHeight / 2f;
            var distances = new[]
            {
                Vector2.DistanceSquared(fallback,
                    new Vector2(enemy.BodyX - 12f, originalCenterY + 5f)),
                Vector2.DistanceSquared(fallback,
                    new Vector2(enemy.BodyX + enemy.BodyWidth + 12f,
                        originalCenterY + 5f)),
                Vector2.DistanceSquared(fallback,
                    new Vector2(originalCenterX, enemy.BodyY - 6f)),
                Vector2.DistanceSquared(fallback,
                    new Vector2(originalCenterX,
                        enemy.BodyY + enemy.BodyHeight + 14f))
            };
            var side = 0;
            for (var index = 1; index < distances.Length; index++)
            {
                if (distances[index] < distances[side])
                    side = index;
            }
            return side switch
            {
                0 => new Vector2(bodyX - 12f, centerY + 5f),
                1 => new Vector2(bodyX + enemy.BodyWidth + 12f, centerY + 5f),
                2 => new Vector2(centerX, bodyY - 6f),
                _ => new Vector2(centerX, bodyY + enemy.BodyHeight + 14f)
            };
        }

        private Vector2 ResolveLiveActorApproach(
            LiveWallpaperMap map, int actorIndex, Vector2 fallback)
        {
            if (map == null || actorIndex < 0 || actorIndex >= map.Actors.Count ||
                !ReferenceEquals(_liveActorMap, map) ||
                !_liveActors.TryGetValue(actorIndex, out var state))
                return fallback;
            var actor = map.Actors[actorIndex];
            if (!TryGetLiveActorBody(actor, state, out _))
                return fallback;
            return LiveWallpaperActorSimulation.ResolveInteractionApproach(
                actor, state, fallback);
        }

        private bool ApplyJourneyConstrainedMovement(
            LiveWallpaperMap map, Vector2 movement, bool includeHoles,
            bool includeEnemies = true)
        {
            if (map == null)
            {
                _position.Offset(movement);
                return movement != Vector2.Zero;
            }
            var start = _position.Position;
            var steps = Math.Max(1, (int)MathF.Ceiling(Math.Max(
                MathF.Abs(movement.X), MathF.Abs(movement.Y))));
            var step = movement / steps;
            for (var index = 0; index < steps; index++)
            {
                if (step.X != 0)
                {
                    var nextX = _position.X + step.X;
                    if (CanOccupyJourneyPosition(
                            map, nextX, _position.Y, includeHoles, includeEnemies))
                        _position.X = nextX;
                    else if (_body.CornerCorrection &&
                             MathF.Abs(step.X) >= MathF.Abs(step.Y) &&
                             TryGetJourneyCollisionBounds(
                                 map, nextX, _position.Y, includeHoles,
                                 includeEnemies, out var collision))
                    {
                        var nudge = LinkGameplayMotion.ResolveHorizontalCornerNudge(
                            _position.Y + _body.OffsetY, _body.Height,
                            collision.Y, collision.Bottom,
                            _body.CornerCorrectionThreshold);
                        if (nudge != 0f && CanOccupyJourneyPosition(
                                map, nextX, _position.Y + nudge,
                                includeHoles, includeEnemies))
                        {
                            _position.Y += nudge;
                            _position.X = nextX;
                        }
                    }
                }
                if (step.Y != 0)
                {
                    var nextY = _position.Y + step.Y;
                    if (CanOccupyJourneyPosition(
                            map, _position.X, nextY, includeHoles, includeEnemies))
                        _position.Y = nextY;
                    else if (_body.CornerCorrection &&
                             MathF.Abs(step.Y) > MathF.Abs(step.X) &&
                             TryGetJourneyCollisionBounds(
                                 map, _position.X, nextY, includeHoles,
                                 includeEnemies, out var collision))
                    {
                        var nudge = LinkGameplayMotion.ResolveVerticalCornerNudge(
                            _position.X + _body.OffsetX, _body.Width,
                            collision.X, collision.Right,
                            _body.CornerCorrectionThreshold);
                        if (nudge != 0f && CanOccupyJourneyPosition(
                                map, _position.X + nudge, nextY,
                                includeHoles, includeEnemies))
                        {
                            _position.X += nudge;
                            _position.Y = nextY;
                        }
                    }
                }
            }
            return (_position.Position - start).LengthSquared() > 0.0001f;
        }

        private bool CanOccupyJourneyPosition(
            LiveWallpaperMap map, float positionX, float positionY,
            bool includeHoles, bool includeEnemies)
        {
            if (!IntersectsJourneyMap(
                    map, positionX, positionY, includeHoles, includeEnemies))
                return true;

            var oldOverlap = GetJourneyBlockingOverlapArea(
                map, _position.X, _position.Y, includeEnemies);
            var newOverlap = GetJourneyBlockingOverlapArea(
                map, positionX, positionY, includeEnemies);
            return !LinkGameplayMotion.BlocksInsideCollisionMovement(
                oldOverlap, newOverlap, _body.Width * _body.Height,
                _body.InsideCollisionEscape);
        }

        private float GetJourneyBlockingOverlapArea(
            LiveWallpaperMap map, float positionX, float positionY,
            bool includeEnemies)
        {
            var bodyX = positionX + _body.OffsetX;
            var bodyY = positionY + _body.OffsetY;
            var overlapArea = map.GetBlockingOverlapArea(
                bodyX, bodyY, _body.Width, _body.Height,
                includeHoles: false,
                includeBushes: true, ignoredBushes: _cutBushes,
                includeStones: true, ignoredStones: _liftedStones,
                ignoredMoveStones: _relocatedMoveStones);
            foreach (var pair in _moveStones)
            {
                if (_fallenMoveStones.Contains(pair.Key))
                    continue;
                overlapArea += GetRectangleOverlapArea(
                    bodyX, bodyY, _body.Width, _body.Height,
                    new LiveWallpaperCollisionBounds(
                        pair.Value.X, pair.Value.Y, TileSize, TileSize));
            }
            for (var actorIndex = 0; actorIndex < map.Actors.Count; actorIndex++)
            {
                var actor = map.Actors[actorIndex];
                if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0)
                    continue;
                if (ReferenceEquals(_liveActorMap, map) &&
                    _liveActors.TryGetValue(actorIndex, out var liveState) &&
                    !liveState.BlocksMovement)
                    continue;
                var collision = new LiveWallpaperCollisionBounds(
                    actor.BodyX, actor.BodyY, actor.BodyWidth, actor.BodyHeight);
                if (ReferenceEquals(_liveActorMap, map) &&
                    _liveActors.TryGetValue(actorIndex, out liveState) &&
                    TryGetLiveActorBody(actor, liveState, out var liveBody))
                    collision = new LiveWallpaperCollisionBounds(
                        liveBody.X, liveBody.Y, liveBody.Width, liveBody.Height);
                overlapArea += GetRectangleOverlapArea(
                    bodyX, bodyY, _body.Width, _body.Height, collision);
            }
            if (includeEnemies)
            {
                if (!ReferenceEquals(_liveEnemyMap, map) || _liveEnemies.Count == 0)
                {
                    foreach (var enemy in map.Enemies)
                    {
                        overlapArea += GetRectangleOverlapArea(
                            bodyX, bodyY, _body.Width, _body.Height,
                            new LiveWallpaperCollisionBounds(
                                enemy.BodyX, enemy.BodyY,
                                enemy.BodyWidth, enemy.BodyHeight));
                    }
                }
                else
                {
                    foreach (var pair in _liveEnemies)
                    {
                        if (!pair.Value.Visible || pair.Key < 0 ||
                            pair.Key >= map.Enemies.Count)
                            continue;
                        var enemy = map.Enemies[pair.Key];
                        overlapArea += GetRectangleOverlapArea(
                            bodyX, bodyY, _body.Width, _body.Height,
                            new LiveWallpaperCollisionBounds(
                                pair.Value.PixelX + enemy.BodyX - enemy.EntityX,
                                pair.Value.PixelY + enemy.BodyY - enemy.EntityY,
                                enemy.BodyWidth, enemy.BodyHeight));
                    }
                }
            }
            return MathF.Min(overlapArea, _body.Width * _body.Height);
        }

        private bool TryGetJourneyCollisionBounds(
            LiveWallpaperMap map, float positionX, float positionY,
            bool includeHoles, bool includeEnemies,
            out LiveWallpaperCollisionBounds collision)
        {
            var bodyX = positionX + _body.OffsetX;
            var bodyY = positionY + _body.OffsetY;
            if (map.TryGetBlockingCollisionBounds(
                    bodyX, bodyY, _body.Width, _body.Height, includeHoles,
                    out collision,
                    includeBushes: true, ignoredBushes: _cutBushes,
                    includeStones: true, ignoredStones: _liftedStones,
                    ignoredMoveStones: _relocatedMoveStones))
                return true;
            foreach (var pair in _moveStones)
            {
                if (_fallenMoveStones.Contains(pair.Key))
                    continue;
                collision = new LiveWallpaperCollisionBounds(
                    pair.Value.X, pair.Value.Y, TileSize, TileSize);
                if (GetRectangleOverlapArea(
                        bodyX, bodyY, _body.Width, _body.Height, collision) > 0f)
                    return true;
            }
            for (var actorIndex = 0; actorIndex < map.Actors.Count; actorIndex++)
            {
                var actor = map.Actors[actorIndex];
                if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0)
                    continue;
                if (ReferenceEquals(_liveActorMap, map) &&
                    _liveActors.TryGetValue(actorIndex, out var liveState) &&
                    !liveState.BlocksMovement)
                    continue;
                collision = new LiveWallpaperCollisionBounds(
                    actor.BodyX, actor.BodyY, actor.BodyWidth, actor.BodyHeight);
                if (ReferenceEquals(_liveActorMap, map) &&
                    _liveActors.TryGetValue(actorIndex, out liveState) &&
                    TryGetLiveActorBody(actor, liveState, out var liveBody))
                    collision = new LiveWallpaperCollisionBounds(
                        liveBody.X, liveBody.Y, liveBody.Width, liveBody.Height);
                if (GetRectangleOverlapArea(
                        bodyX, bodyY, _body.Width, _body.Height, collision) > 0f)
                    return true;
            }
            if (includeEnemies && ReferenceEquals(_liveEnemyMap, map) &&
                _liveEnemies.Count > 0)
            {
                foreach (var pair in _liveEnemies)
                {
                    if (!pair.Value.Visible || pair.Key < 0 ||
                        pair.Key >= map.Enemies.Count)
                        continue;
                    var enemy = map.Enemies[pair.Key];
                    collision = new LiveWallpaperCollisionBounds(
                        pair.Value.PixelX + enemy.BodyX - enemy.EntityX,
                        pair.Value.PixelY + enemy.BodyY - enemy.EntityY,
                        enemy.BodyWidth, enemy.BodyHeight);
                    if (GetRectangleOverlapArea(
                            bodyX, bodyY, _body.Width, _body.Height, collision) > 0f)
                        return true;
                }
            }
            else if (includeEnemies)
            {
                foreach (var enemy in map.Enemies)
                {
                    collision = new LiveWallpaperCollisionBounds(
                        enemy.BodyX, enemy.BodyY,
                        enemy.BodyWidth, enemy.BodyHeight);
                    if (GetRectangleOverlapArea(
                            bodyX, bodyY, _body.Width, _body.Height, collision) > 0f)
                        return true;
                }
            }
            collision = default;
            return false;
        }

        private static float GetRectangleOverlapArea(
            float x, float y, float width, float height,
            LiveWallpaperCollisionBounds collision)
        {
            var intersectionWidth = MathF.Min(x + width, collision.Right) -
                                    MathF.Max(x, collision.X);
            var intersectionHeight = MathF.Min(y + height, collision.Bottom) -
                                     MathF.Max(y, collision.Y);
            return intersectionWidth > 0f && intersectionHeight > 0f
                ? intersectionWidth * intersectionHeight
                : 0f;
        }

        private bool TryStartBlockingBushCut(
            LiveWallpaperMap map,
            Vector2 movement,
            long elapsedMilliseconds,
            out Vector2 direction)
        {
            direction = movement;
            if (map == null || movement.LengthSquared() <= 0.0001f ||
                _runtimeBushCutActive || elapsedMilliseconds < _pauseUntil)
                return false;
            if (direction.LengthSquared() > 0.0001f)
                direction.Normalize();

            if (!map.TryGetBushKeyAlongMovement(
                    _position.X + _body.OffsetX,
                    _position.Y + _body.OffsetY,
                    _body.Width,
                    _body.Height,
                    movement.X,
                    movement.Y,
                    out var bushKey,
                    _cutBushes))
                return false;

            _cutBushes.Add(bushKey);
            _cutVegetationTimes[bushKey] = elapsedMilliseconds;
            RollVegetationDrop(bushKey, direction);
            _bushCutStartedAt = elapsedMilliseconds;
            _runtimeBushCutActive = true;
            _runtimeBushCutDirection = direction;
            _pauseUntil = elapsedMilliseconds + 233L;
            return true;
        }

        private bool TryStartBlockingStoneLift(
            LiveWallpaperMap map,
            Vector2 movement,
            long elapsedMilliseconds,
            out Vector2 direction)
        {
            direction = movement;
            if (map == null || movement.LengthSquared() <= 0.0001f ||
                _runtimeStoneLiftActive || elapsedMilliseconds < _pauseUntil)
                return false;
            if (direction.LengthSquared() > 0.0001f)
                direction.Normalize();
            if (!map.TryGetStoneKeyAlongMovement(
                    _position.X + _body.OffsetX,
                    _position.Y + _body.OffsetY,
                    _body.Width, _body.Height,
                    movement.X, movement.Y,
                    out var stoneKey,
                    _liftedStones))
                return false;

            _liftedStones.Add(stoneKey);
            _liftedStoneTimes[stoneKey] = elapsedMilliseconds;
            _activeLiftedStoneKey = stoneKey;
            _stoneLiftStartedAt = elapsedMilliseconds;
            _runtimeStoneLiftActive = true;
            _runtimeStoneLiftDirection = DirectionToVector(
                ResolveDirection(direction, _lastRouteDirection));
            _stoneImpactKind = LiveWallpaperStoneImpactKind.None;
            _stoneImpactEnemyIndex = -1;
            _pauseUntil = elapsedMilliseconds + StoneSequenceMilliseconds;
            return true;
        }

        private bool TryStartBlockingMoveStonePush(
            LiveWallpaperMap map,
            Vector2 movement,
            long elapsedMilliseconds,
            out Vector2 direction)
        {
            direction = movement;
            if (map == null || movement.LengthSquared() <= 0.0001f ||
                _activeMoveStoneKey >= 0 || elapsedMilliseconds < _pauseUntil)
                return false;
            direction = DirectionToVector(
                ResolveDirection(movement, _lastRouteDirection));
            var bodyX = _position.X + movement.X + _body.OffsetX;
            var bodyY = _position.Y + movement.Y + _body.OffsetY;
            var moveStoneKey = -1;
            foreach (var pair in _moveStones)
            {
                if (_fallenMoveStones.Contains(pair.Key) ||
                    bodyX >= pair.Value.X + TileSize ||
                    bodyX + _body.Width <= pair.Value.X ||
                    bodyY >= pair.Value.Y + TileSize ||
                    bodyY + _body.Height <= pair.Value.Y)
                    continue;
                moveStoneKey = pair.Key;
                break;
            }
            if (moveStoneKey < 0 && !map.TryGetMoveStoneAt(
                    bodyX, bodyY, _body.Width, _body.Height,
                    out moveStoneKey, _relocatedMoveStones))
                return false;
            return TryStartMoveStonePush(
                map, moveStoneKey, direction, elapsedMilliseconds,
                journeyAction: false);
        }

        private Vector2 ResolveMoveStonePushDirection(
            LiveWallpaperMap map, int moveStoneKey)
        {
            if (_journeyPlan != null &&
                _journeyPointIndex + 1 < _journeyPlan.Points.Count)
            {
                var next = _journeyPlan.Points[_journeyPointIndex + 1];
                var routeDirection = new Vector2(
                    next.PixelX - _position.X, next.PixelY - _position.Y);
                if (routeDirection.LengthSquared() > 0.0001f)
                    return DirectionToVector(
                        ResolveDirection(routeDirection, _lastRouteDirection));
            }
            var foundPosition = _moveStones.TryGetValue(
                moveStoneKey, out var position);
            if (!foundPosition && map?.TryGetMoveStone(
                    moveStoneKey, out var originalX, out var originalY,
                    out _) == true)
            {
                position = new Vector2(originalX, originalY);
                foundPosition = true;
            }
            if (foundPosition)
            {
                var blockDirection = new Vector2(
                    position.X + TileSize / 2f - _position.X,
                    position.Y + TileSize / 2f -
                    (_position.Y + _body.OffsetY + _body.Height / 2f));
                if (blockDirection.LengthSquared() > 0.0001f)
                    return DirectionToVector(
                        ResolveDirection(blockDirection, _lastRouteDirection));
            }
            return DirectionToVector(_lastRouteDirection);
        }

        private bool TryStartMoveStonePush(
            LiveWallpaperMap map, int moveStoneKey, Vector2 direction,
            long elapsedMilliseconds, bool journeyAction)
        {
            if (map == null || _activeMoveStoneKey >= 0 ||
                !map.TryGetMoveStone(
                    moveStoneKey, out var originX, out var originY,
                    out var allowedDirections))
                return false;
            var pushDirection = ResolveDirection(direction, _lastRouteDirection);
            if (allowedDirections != -1 &&
                (allowedDirections & (1 << pushDirection)) == 0)
                return false;
            var start = _moveStones.TryGetValue(moveStoneKey, out var moved)
                ? moved
                : new Vector2(originX, originY);
            var goal = start + DirectionToVector(pushDirection) * TileSize;
            _relocatedMoveStones.Add(moveStoneKey);
            var blocked = map.IntersectsVoid(goal.X, goal.Y, TileSize, TileSize) ||
                          map.IntersectsCollision(
                              goal.X, goal.Y, TileSize, TileSize,
                              includeHoles: false,
                              ignoredMoveStones: _relocatedMoveStones);
            if (!blocked)
            {
                foreach (var pair in _moveStones)
                {
                    if (pair.Key == moveStoneKey ||
                        _fallenMoveStones.Contains(pair.Key) ||
                        goal.X >= pair.Value.X + TileSize ||
                        goal.X + TileSize <= pair.Value.X ||
                        goal.Y >= pair.Value.Y + TileSize ||
                        goal.Y + TileSize <= pair.Value.Y)
                        continue;
                    blocked = true;
                    break;
                }
            }
            if (blocked)
            {
                if (!_moveStones.ContainsKey(moveStoneKey))
                    _relocatedMoveStones.Remove(moveStoneKey);
                return false;
            }
            _moveStones[moveStoneKey] = start;
            _activeMoveStoneKey = moveStoneKey;
            _activeMoveStoneStart = start;
            _activeMoveStoneGoal = goal;
            _moveStonePushStartedAt = elapsedMilliseconds;
            _moveStoneJourneyAction = journeyAction;
            _lastRouteDirection = pushDirection;
            _pauseUntil = elapsedMilliseconds + MoveStoneInertiaMilliseconds +
                          MoveStoneMovementMilliseconds;
            return true;
        }

        private void UpdateMoveStoneMotion(
            LiveWallpaperMap map, long elapsedMilliseconds)
        {
            if (_activeMoveStoneKey < 0)
                return;
            var movementElapsed = elapsedMilliseconds - _moveStonePushStartedAt -
                                  MoveStoneInertiaMilliseconds;
            if (movementElapsed <= 0L)
            {
                _moveStones[_activeMoveStoneKey] = _activeMoveStoneStart;
                return;
            }
            var amount = Math.Clamp(
                movementElapsed / (float)MoveStoneMovementMilliseconds, 0f, 1f);
            amount = MathF.Sin(amount * MathF.PI / 2f);
            _moveStones[_activeMoveStoneKey] = Vector2.Lerp(
                _activeMoveStoneStart, _activeMoveStoneGoal, amount);
            if (movementElapsed < MoveStoneMovementMilliseconds)
                return;
            _moveStones[_activeMoveStoneKey] = _activeMoveStoneGoal;
            if (map != null &&
                (map.IntersectsHole(
                     _activeMoveStoneGoal.X, _activeMoveStoneGoal.Y,
                     TileSize, TileSize) ||
                 map.IsDeepWaterAt(
                     _activeMoveStoneGoal.X + TileSize / 2f,
                     _activeMoveStoneGoal.Y + TileSize / 2f)))
            {
                _fallenMoveStones.Add(_activeMoveStoneKey);
                _moveStones.Remove(_activeMoveStoneKey);
            }
            _activeMoveStoneKey = -1;
            _moveStonePushStartedAt = 0L;
            if (_moveStoneJourneyAction)
                _journeyPointIndex++;
            _moveStoneJourneyAction = false;
        }

        private Vector2 ResolveStoneThrowDirection()
        {
            if (_journeyPlan != null &&
                _journeyPointIndex + 1 < _journeyPlan.Points.Count)
            {
                var next = _journeyPlan.Points[_journeyPointIndex + 1];
                var difference = new Vector2(
                    next.PixelX - _position.X,
                    next.PixelY - _position.Y);
                if (difference.LengthSquared() > 0.0001f)
                    return DirectionToVector(
                        ResolveDirection(difference, _lastRouteDirection));
            }
            return DirectionToVector(_lastRouteDirection);
        }

        private bool AdvanceFeatherHeight(float timeMultiplier)
        {
            // Wallpaper frame rates can be intentionally much lower than the
            // game's 60 Hz update. Replay the same gravity-first SystemBody
            // update in at-most-one-frame slices so 15/30 Hz rendering does not
            // shorten the canonical feather arc and land Link inside its hole.
            var remaining = Math.Max(0f, timeMultiplier);
            while (remaining > 0f)
            {
                var step = Math.Min(1f, remaining);
                _body.Velocity.Z = LinkGameplayMotion.ApplyGravity(
                    _body.Velocity.Z, _body.Gravity, step);
                var nextHeight = _position.Z +
                                 _body.Velocity.Z * step;
                if (nextHeight > 0f &&
                    (!_body.IsGrounded || _body.Velocity.Z >= 0f))
                {
                    _position.Z = nextHeight;
                    _body.IsGrounded = false;
                    remaining -= step;
                    continue;
                }
                _position.Z = 0f;
                _body.Velocity.Z = 0f;
                _body.IsGrounded = true;
                _airMoveVelocity = Vector2.Zero;
                return false;
            }
            return !_body.IsGrounded;
        }

        private void UpdateHoleAbsorption(
            LiveWallpaperMap map, float timeMultiplier,
            long elapsedMilliseconds)
        {
            _body.SpeedMultiply = 1f;
            if (_holeFalling)
                return;
            if (!_body.IsGrounded)
            {
                // SystemBody leaves the accumulated vector untouched while in
                // the air, but marks it as no longer actively pulling.
                _body.WasHolePulled = false;
                return;
            }
            if (map == null)
            {
                _body.HoleAbsorption = Vector2.Zero;
                _body.WasHolePulled = false;
                return;
            }

            var contact = map.GetLinkHoleContact(
                _position.X + _body.OffsetX,
                _position.Y + _body.OffsetY,
                _body.Width, _body.Height);
            _body.SpeedMultiply = LinkGameplayMotion.ResolveHoleSpeedMultiply(
                contact.Coverage, _body.AbsorbStop);
            if (contact.Coverage >= _body.AbsorbPercentage)
            {
                if (!_body.WasHolePulled)
                    _body.HoleAbsorption = Vector2.Zero;
                _body.HoleAbsorption *= MathF.Pow(0.85f, timeMultiplier);
                BeginHoleFall(elapsedMilliseconds);
                return;
            }
            if (contact.Coverage > _body.AbsorbStop)
            {
                _body.HoleAbsorption =
                    LinkGameplayMotion.ResolveHoleAbsorption(
                        _body.HoleAbsorption,
                        new Vector2(contact.DirectionX, contact.DirectionY),
                        contact.Coverage, _body.AbsorbStop,
                        timeMultiplier);
                _body.WasHolePulled = true;
                return;
            }
            if (_body.HoleAbsorption != Vector2.Zero)
                _body.HoleAbsorption = Vector2.Zero;
            _body.WasHolePulled = false;
        }

        private void BeginHoleFall(long elapsedMilliseconds)
        {
            // ObjLink.OnHoleAbsorb clears movement and plays link0/fall before
            // OnHoleReset returns Link to the field's saved safe position.
            _holeFalling = true;
            _holeFallStartedAt = elapsedMilliseconds;
            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;
            _body.IsGrounded = true;
            _position.Z = 0f;
            _airMoveVelocity = Vector2.Zero;
            _hitVelocity = Vector2.Zero;
            _hookshotStarted = false;
            _hookshotPulling = false;
        }

        private void UpdateHoleResetPosition(LiveWallpaperMap map)
        {
            if (map == null)
                return;
            var field = GetHoleResetField(_position.X, _position.Y);
            var fieldDifference = field - _holeResetField;
            _holeResetField = field;
            if (fieldDifference == Point.Zero)
                return;

            // ObjLink.UpdateSavePosition aligns the axis that crossed a 160x128
            // field boundary to the installed 16-pixel grid, then pushes the
            // reset point inward. Saving the raw crossing coordinate can leave
            // the wallpaper reset point on the same hole edge that absorbed Link.
            var bodyCenterX = _position.X + _body.OffsetX + _body.Width / 2f;
            var bodyCenterY = _position.Y + _body.OffsetY + _body.Height / 2f;
            var resetX = fieldDifference.X == 0
                ? _position.X
                : (int)(bodyCenterX / TileSize +
                        (fieldDifference.X > 0 ? 0 : 1)) * TileSize;
            var resetY = fieldDifference.Y == 0
                ? _position.Y
                : (int)(bodyCenterY / TileSize +
                        (fieldDifference.Y > 0 ? 0 : 1)) * TileSize;
            if (fieldDifference.X > 0) resetX += 8f;
            if (fieldDifference.X < 0) resetX -= 8f;
            if (fieldDifference.Y > 0) resetY += 16f;
            if (fieldDifference.Y < 0) resetY -= 2f;
            if (!map.IntersectsHole(
                    resetX + _body.OffsetX, resetY + _body.OffsetY,
                    _body.Width, _body.Height))
                _holeResetPosition = new Vector2(resetX, resetY);
        }

        private Point GetHoleResetField(float positionX, float positionY)
        {
            var bodyCenterX = positionX + _body.OffsetX + _body.Width / 2f;
            var bodyCenterY = positionY + _body.OffsetY + _body.Height / 2f;
            return new Point((int)bodyCenterX / 160, (int)bodyCenterY / 128);
        }

        private void ResolveActiveStoneState(
            LiveWallpaperMap map,
            long elapsedMilliseconds,
            out float entityX,
            out float entityY,
            out float height,
            out bool released)
        {
            entityX = 0f;
            entityY = 0f;
            height = 0f;
            released = false;
            if (map == null || _activeLiftedStoneKey < 0 || map.Width <= 0)
                return;

            float mapX;
            float mapY;
            if (!map.TryGetStoneMapPosition(
                    _activeLiftedStoneKey, out mapX, out mapY))
            {
                var tileX = _activeLiftedStoneKey % map.Width;
                var tileY = _activeLiftedStoneKey / map.Width;
                mapX = tileX * TileSize;
                mapY = tileY * TileSize;
            }
            var original = GameObjectVisualLayout.GetStoneEntityPosition(mapX, mapY);
            var sequenceElapsed = Math.Max(
                0L, elapsedMilliseconds - _stoneLiftStartedAt);
            if (sequenceElapsed < StonePullMilliseconds)
            {
                entityX = original.X;
                entityY = original.Y;
                return;
            }

            var throwStart = StonePullMilliseconds +
                             StonePreCarryMilliseconds +
                             StoneThrowInputDelayMilliseconds;
            if (sequenceElapsed < throwStart)
            {
                // ObjLink.UpdatePositionCarriedObject and ObjStone.CarryUpdate.
                var carryStart = new Vector3(
                    original.X,
                    original.Y - GameObjectVisualLayout.StoneVerticalOffset,
                    0f);
                var target = new Vector3(
                    _position.X, _position.Y,
                    StoneGameplayMotion.CarryHeight);
                var carried = LinkGameplayMotion.ResolvePreCarryPosition(
                    carryStart, target,
                    sequenceElapsed - StonePullMilliseconds);
                entityX = carried.X;
                entityY = carried.Y - GameObjectVisualLayout.StoneVerticalOffset;
                height = carried.Z;
                return;
            }

            // ObjStone.CarryThrow gives the body a cardinal 3 px/frame velocity,
            // starts at CarryHeight, and applies BodyComponent gravity each frame.
            released = true;
            var throwFrame = Math.Max(0, (int)MathF.Floor(
                (sequenceElapsed - throwStart) / (1000f / 60f)));
            entityX = _position.X +
                      _runtimeStoneLiftDirection.X *
                      StoneGameplayMotion.ThrowSpeed * throwFrame;
            entityY = _position.Y - GameObjectVisualLayout.StoneVerticalOffset +
                      _runtimeStoneLiftDirection.Y *
                      StoneGameplayMotion.ThrowSpeed * throwFrame;
            height = StoneGameplayMotion.ResolveHeight(throwFrame);

            if (_stoneImpactKind != LiveWallpaperStoneImpactKind.None)
                return;

            var bodyX = entityX - 4f;
            var bodyY = entityY - 5f;
            var grounded = height <= 0.001f;
            var enemyIndex = height <= 12f
                ? TryGetThrownStoneEnemy(map, entityX - 7f, entityY - 11f,
                    14f, 14f)
                : -1;
            var impact = enemyIndex >= 0
                ? LiveWallpaperStoneImpactKind.Enemy
                : map.IntersectsHole(bodyX, bodyY, 8f, 8f)
                    ? LiveWallpaperStoneImpactKind.Hole
                    : grounded && map.IsWaterAt(entityX, entityY)
                        ? LiveWallpaperStoneImpactKind.Water
                        : map.IntersectsVoid(bodyX, bodyY, 8f, 8f) ||
                          map.IntersectsCollision(
                              bodyX, bodyY, 8f, 8f, includeHoles: false,
                              includeBushes: true,
                              ignoredBushes: _cutBushes,
                              includeStones: true,
                              ignoredStones: _liftedStones) ||
                          map.IntersectsActor(bodyX, bodyY, 8f, 8f) || grounded
                            ? LiveWallpaperStoneImpactKind.Break
                            : LiveWallpaperStoneImpactKind.None;
            if (impact == LiveWallpaperStoneImpactKind.None)
                return;
            _stoneImpactKind = impact;
            _stoneImpactX = entityX;
            _stoneImpactY = entityY;
            _stoneImpactStartedAt = elapsedMilliseconds;
            _stoneImpactEnemyIndex = enemyIndex;
            _stoneImpactSerial++;
        }

        private int TryGetThrownStoneEnemy(
            LiveWallpaperMap map, float x, float y, float width, float height)
        {
            if (!ReferenceEquals(_liveEnemyMap, map))
                return -1;
            foreach (var pair in _liveEnemies)
            {
                if (!pair.Value.Visible || pair.Key < 0 ||
                    pair.Key >= map.Enemies.Count)
                    continue;
                var enemy = map.Enemies[pair.Key];
                var bodyX = pair.Value.PixelX + enemy.BodyX - enemy.EntityX;
                var bodyY = pair.Value.PixelY + enemy.BodyY - enemy.EntityY;
                if (x < bodyX + enemy.BodyWidth && x + width > bodyX &&
                    y < bodyY + enemy.BodyHeight && y + height > bodyY)
                    return pair.Key;
            }
            return -1;
        }

        private bool IntersectsJourneyMap(
            LiveWallpaperMap map, float positionX, float positionY,
            bool includeHoles, bool includeEnemies) =>
            map.IntersectsVoid(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height) ||
            map.IntersectsCollision(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height,
                includeHoles,
                includeBushes: true,
                ignoredBushes: _cutBushes,
                includeStones: true,
                ignoredStones: _liftedStones,
                ignoredMoveStones: _relocatedMoveStones) ||
            IntersectsMovedMoveStone(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width, _body.Height) ||
            IntersectsLiveActor(
                map,
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height) ||
            includeEnemies && IntersectsLiveEnemy(
                map,
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height);

        private bool IntersectsMovedMoveStone(
            float x, float y, float width, float height)
        {
            foreach (var pair in _moveStones)
            {
                if (_fallenMoveStones.Contains(pair.Key))
                    continue;
                var position = pair.Value;
                if (x < position.X + TileSize && x + width > position.X &&
                    y < position.Y + TileSize && y + height > position.Y)
                    return true;
            }
            return false;
        }

        private bool IntersectsLiveActor(
            LiveWallpaperMap map, float x, float y, float width, float height)
        {
            for (var actorIndex = 0; actorIndex < map.Actors.Count; actorIndex++)
            {
                var actor = map.Actors[actorIndex];
                if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0)
                    continue;
                if (ReferenceEquals(_liveActorMap, map) &&
                    _liveActors.TryGetValue(actorIndex, out var state) &&
                    !state.BlocksMovement)
                    continue;
                var bodyX = (float)actor.BodyX;
                var bodyY = (float)actor.BodyY;
                var bodyWidth = (float)actor.BodyWidth;
                var bodyHeight = (float)actor.BodyHeight;
                if (ReferenceEquals(_liveActorMap, map) &&
                    _liveActors.TryGetValue(actorIndex, out state) &&
                    TryGetLiveActorBody(actor, state, out var liveBody))
                {
                    bodyX = liveBody.X;
                    bodyY = liveBody.Y;
                    bodyWidth = liveBody.Width;
                    bodyHeight = liveBody.Height;
                }
                if (x < bodyX + bodyWidth && x + width > bodyX &&
                    y < bodyY + bodyHeight && y + height > bodyY)
                    return true;
            }
            return false;
        }

        private static bool TryGetLiveActorBody(
            LiveWallpaperMapActor actor,
            LiveWallpaperActorState state,
            out (float X, float Y, float Width, float Height) body)
        {
            var spawnEntityX = actor.PixelX + 8f;
            var spawnEntityY = actor.PixelY + 16f;
            if (actor.Kind == LiveWallpaperMapActorKind.BowWow)
                spawnEntityX = actor.PixelX;
            if (actor.Kind is not (LiveWallpaperMapActorKind.Dog or
                LiveWallpaperMapActorKind.Bird or
                LiveWallpaperMapActorKind.BowWow or
                LiveWallpaperMapActorKind.Owl))
            {
                body = default;
                return false;
            }
            body = (
                state.EntityX + actor.BodyX - spawnEntityX,
                state.EntityY + actor.BodyY - spawnEntityY,
                actor.BodyWidth,
                actor.BodyHeight);
            return true;
        }

        private bool IntersectsLiveEnemy(
            LiveWallpaperMap map, float x, float y, float width, float height)
        {
            if (!ReferenceEquals(_liveEnemyMap, map) || _liveEnemies.Count == 0)
                return map.IntersectsEnemy(x, y, width, height);
            foreach (var pair in _liveEnemies)
            {
                if (!pair.Value.Visible || pair.Key < 0 || pair.Key >= map.Enemies.Count)
                    continue;
                var enemy = map.Enemies[pair.Key];
                var bodyX = pair.Value.PixelX + enemy.BodyX - enemy.EntityX;
                var bodyY = pair.Value.PixelY + enemy.BodyY - enemy.EntityY;
                if (x < bodyX + enemy.BodyWidth && x + width > bodyX &&
                    y < bodyY + enemy.BodyHeight && y + height > bodyY)
                    return true;
            }
            return false;
        }

        private bool TryStartBlockingEnemyAttack(
            LiveWallpaperMap map, Vector2 movement, long elapsedMilliseconds,
            out Vector2 attackDirection)
        {
            attackDirection = Vector2.Zero;
            if (map == null || movement.LengthSquared() <= 0.0001f ||
                _runtimeCombatEnemyIndex >= 0)
                return false;
            var bodyX = _position.X + movement.X + _body.OffsetX;
            var bodyY = _position.Y + movement.Y + _body.OffsetY;
            var enemyIndex = TryGetLiveEnemyAt(
                map, bodyX, bodyY, _body.Width, _body.Height);
            if (enemyIndex < 0)
                enemyIndex = TryGetEnemyAtSwordApproach(map, movement);
            if (enemyIndex < 0)
                return false;
            _runtimeCombatEnemyIndex = enemyIndex;
            _combatStartedAt = elapsedMilliseconds;
            _pauseUntil = elapsedMilliseconds + 233L;
            attackDirection = FaceEnemy(map, enemyIndex);
            return true;
        }

        private int TryGetLiveEnemyAt(
            LiveWallpaperMap map, float x, float y, float width, float height)
        {
            if (!ReferenceEquals(_liveEnemyMap, map) || _liveEnemies.Count == 0)
            {
                for (var enemyIndex = 0; enemyIndex < map.Enemies.Count; enemyIndex++)
                {
                    var enemy = map.Enemies[enemyIndex];
                    if (x < enemy.BodyX + enemy.BodyWidth && x + width > enemy.BodyX &&
                        y < enemy.BodyY + enemy.BodyHeight && y + height > enemy.BodyY)
                        return enemyIndex;
                }
                return -1;
            }
            foreach (var pair in _liveEnemies)
            {
                if (!pair.Value.Visible || pair.Key < 0 || pair.Key >= map.Enemies.Count)
                    continue;
                var enemy = map.Enemies[pair.Key];
                var bodyX = pair.Value.PixelX + enemy.BodyX - enemy.EntityX;
                var bodyY = pair.Value.PixelY + enemy.BodyY - enemy.EntityY;
                if (x < bodyX + enemy.BodyWidth && x + width > bodyX &&
                    y < bodyY + enemy.BodyHeight && y + height > bodyY)
                    return pair.Key;
            }
            return -1;
        }

        private int TryGetEnemyAtSwordApproach(
            LiveWallpaperMap map, Vector2 movement)
        {
            // Proximity combat requires a live enemy session so completed hits
            // can remove the target. Static map metadata alone has no death
            // state and would cause a permanent repeated attack.
            if (!ReferenceEquals(_liveEnemyMap, map) || _liveEnemies.Count == 0)
                return -1;
            var direction = ResolveDirection(movement, _lastRouteDirection);
            foreach (var pair in _liveEnemies)
            {
                var enemyIndex = pair.Key;
                if (enemyIndex < 0 || enemyIndex >= map.Enemies.Count ||
                    !pair.Value.Visible)
                    continue;
                var enemy = map.Enemies[enemyIndex];
                var bodyX = pair.Value.PixelX + enemy.BodyX - enemy.EntityX;
                var bodyY = pair.Value.PixelY + enemy.BodyY - enemy.EntityY;
                var centerX = bodyX + enemy.BodyWidth / 2f;
                var centerY = bodyY + enemy.BodyHeight / 2f;
                // These are the same four sword positions used by
                // LiveWallpaperJourneyPlanner.GetEnemyApproaches.
                var approach = direction switch
                {
                    0 => new Vector2(
                        bodyX + enemy.BodyWidth + 12f, centerY + 5f),
                    1 => new Vector2(
                        centerX, bodyY + enemy.BodyHeight + 14f),
                    2 => new Vector2(bodyX - 12f, centerY + 5f),
                    _ => new Vector2(centerX, bodyY - 6f)
                };
                var difference = approach - _position.Position;
                if (difference.LengthSquared() <= 8f * 8f &&
                    Vector2.Dot(difference, movement) >= -0.001f)
                    return enemyIndex;
            }
            return -1;
        }

        private bool IsSwimming(LiveWallpaperMap map, float positionX, float positionY) =>
            map?.IsDeepWaterAt(positionX, positionY - _body.Height * 0.5f) == true;

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
                _airMoveVelocity = Vector2.Zero;
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
                    _body.Velocity.Z = LinkGameplayMotion.FeatherVelocity;
                    _body.IsGrounded = false;
                    _committedJumpMove = DirectionToVector(route.Direction);
                    _committedJumpRemaining =
                        LinkGameplayMotion.FeatherAirborneFramesAt60Fps *
                        WalkSpeedPerFrame;
                    _airMoveVelocity = _committedJumpMove * WalkSpeedPerFrame;
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
                if (!_body.IsGrounded || _body.Velocity.Z > 0f)
                    _airMoveVelocity = LinkGameplayMotion.ResolveAirVelocity(
                        _airMoveVelocity, inputMove,
                        WalkSpeedPerFrame, frameScale);
                _body.VelocityTarget = (!_body.IsGrounded || _body.Velocity.Z > 0f)
                    ? _airMoveVelocity
                    : inputMove * WalkSpeedPerFrame;

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

                if (!_body.IsGrounded || _body.Velocity.Z > 0f)
                    AdvanceFeatherHeight(Math.Max(
                        0f, frameScale - (featherPressed ? 1f : 0f)));
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
                includeHoles: _body.IsGrounded,
                ignoredBushes: _cutBushes,
                ignoredStones: _liftedStones) ||
            map.IntersectsActor(
                positionX + _body.OffsetX,
                positionY + _body.OffsetY,
                _body.Width,
                _body.Height);
    }
}
