using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectZ;

internal sealed class LiveWallpaperSideViewSimulation
{
    private const double FrameMilliseconds = 1000.0 / 60;
    private readonly LiveWallpaperMap _map;
    private readonly Vector2 _spawn;
    private readonly string _entryId;
    private bool _entryLatched;
    private SideViewBody _body;
    private LiveWallpaperSideViewPlanner _search;
    private IReadOnlyList<SideViewInput> _route = [];
    private int _routeIndex;
    private bool _routeReachedGoal;
    private int _targetChunks;
    private long? _lastTime;
    private double _remainder;
    private long _nextSearch;
    private Vector2? _target;
    private bool _allowReturn;
    private bool _noRoute;
    private SideViewInput _input;
    private string _pendingPortal;
    private int _autonomousSearches;
    private bool _hookshotStaging;
    private bool _hookshotActive;
    private bool _hookshotPulling;
    private Vector2 _hookshotPosition;
    private Vector2 _hookshotContact;
    private Vector2 _hookshotLanding;
    private bool _hookshotFireRight;
    public SideViewBody Body => _body;

    public LiveWallpaperSideViewSimulation(LiveWallpaperMap map, Vector2 spawn, string entryId)
    {
        _map = map;
        _spawn = spawn;
        _entryId = entryId;
        _entryLatched = entryId != null;
        _body = LiveWallpaperSideViewPhysics.Spawn(map, spawn);
    }

    public bool CanActivate(LiveWallpaperMapPortal portal) =>
        (!_entryLatched || portal.EntryId != _entryId) &&
        (_pendingPortal == null || portal.EntryId == _pendingPortal);

    public void WalkTo(Vector2 target)
    {
        _target = target;
        _search = null;
        _route = [];
        _routeIndex = 0;
        _pendingPortal = null;
        _nextSearch = 0;
        _noRoute = false;
        _allowReturn = false;
        _targetChunks = 0;
        _hookshotStaging = false;
        _hookshotActive = false;
        _hookshotPulling = false;
    }

    public LiveWallpaperSimulatedLinkState Update(long elapsed, bool active)
    {
        var delta = _lastTime.HasValue ? elapsed - _lastTime.Value : 0;
        _lastTime = elapsed;
        if (!active || delta < 0 || delta > 1000) delta = 0;
        _remainder += delta;
        var ticks = Math.Min(6, (int)((_remainder + 0.0001) / FrameMilliseconds));
        _remainder -= ticks * FrameMilliseconds;

        if (active && _pendingPortal == null && !_hookshotActive)
        {
            if (_search == null && _routeIndex >= _route.Count && elapsed >= _nextSearch && !_noRoute &&
                (_body.Grounded || _body.Climbing || _body.Swimming))
            {
                if (!_target.HasValue && !_hookshotStaging &&
                    _autonomousSearches++ % 4 == 0)
                    TryQueueHookshotRoute();
                _search = new LiveWallpaperSideViewPlanner(_map, _body, _entryId,
                    _entryLatched, _target, _allowReturn);
            }
            if (_search != null)
            {
                _search.Advance();
                if (_search.Complete)
                {
                    _route = _search.Route;
                    _routeIndex = 0;
                    _routeReachedGoal = _search.ReachedGoal;
                    _nextSearch = elapsed + 650;
                    if (_route.Count == 0)
                    {
                        _hookshotStaging = false;
                        // An unreachable tap must not permanently disable
                        // autonomous navigation for this entire room.
                        if (_target.HasValue)
                        {
                            _target = null;
                            _targetChunks = 0;
                            _nextSearch = elapsed + 4000;
                        }
                        else if (!_allowReturn) _allowReturn = true;
                        else _noRoute = true;
                    }
                    else if (_target.HasValue) _targetChunks++;
                    _search = null;
                }
                // Planning happens at a supported rest point, not while falling.
                // Do not advance game time during that bounded planning pause.
                ticks = 0;
                _input = default;
            }
        }
        if (active && _pendingPortal == null)
        for (var tick = 0; tick < ticks; tick++)
        {
            if (_hookshotActive)
            {
                AdvanceHookshot();
                continue;
            }
            _input = _routeIndex < _route.Count ? _route[_routeIndex++] : default;
            if (!LiveWallpaperSideViewPhysics.Step(_map, ref _body, _input))
            {
                _body = LiveWallpaperSideViewPhysics.Spawn(_map, _spawn);
                _route = [];
                _search = null;
                _entryLatched = _entryId != null;
                _allowReturn = true;
                _nextSearch = elapsed + 650;
                break;
            }
            foreach (var portal in _map.Portals)
            {
                if (portal.EntryId == _entryId && !portal.TouchesSideViewTrigger(_body.Position.X, _body.Position.Y))
                    _entryLatched = false;
                if (!portal.HasDestination || portal.IsHoleTeleporter || portal.Mode is not (0 or 1 or 3) || !CanActivate(portal)) continue;
                if (!portal.ShouldActivateAt(_body.Position.X, _body.Position.Y, _input.Move.Y,
                        _input.Move.Y < 0 ? 1 : _body.Direction, true, _body.Grounded)) continue;
                _pendingPortal = portal.EntryId;
                break;
            }
            if (_pendingPortal != null) break;
            if (_route.Count > 0 && _routeIndex == _route.Count)
            {
                // Consume completion once. Leaving the finished route here
                // would postpone the next search on every subsequent tick.
                _route = [];
                _routeIndex = 0;
                if (_routeReachedGoal && _hookshotStaging)
                {
                    _hookshotStaging = false;
                    _hookshotActive = true;
                    _hookshotPulling = false;
                    _body.Direction = _hookshotFireRight ? 2 : 0;
                    _hookshotPosition = _body.Position +
                        new Vector2(_hookshotFireRight ? 5f : -5f, -4f);
                    _target = null;
                    _targetChunks = 0;
                    _input = default;
                    continue;
                }
                // A bounded search may end at a safe intermediate point.
                // Keep the user's goal across those sections, but bound the
                // number of sections attempted for an unreachable tap.
                var continueRoute = !_routeReachedGoal &&
                    (!_target.HasValue || _targetChunks < 8);
                if (!continueRoute) _target = null;
                _nextSearch = elapsed + (continueRoute ? 650 : 4000);
            }
        }

        var action = _hookshotActive ? LiveWallpaperLinkRouteAction.Hookshot :
            _body.Swimming ? LiveWallpaperLinkRouteAction.SideViewSwim :
            _body.Climbing ? LiveWallpaperLinkRouteAction.Climb :
            !_body.Grounded ? (_body.FallVelocity < 0 ? LiveWallpaperLinkRouteAction.FeatherJump : LiveWallpaperLinkRouteAction.SideViewFall) :
            _input.Move.X != 0 ? LiveWallpaperLinkRouteAction.Walk : LiveWallpaperLinkRouteAction.Stand;
        var direction = _body.Climbing ? 1 : _body.Direction;
        if (_pendingPortal != null && _input.Move.Y < 0) direction = 1;
        return new LiveWallpaperSimulatedLinkState(_body.Position.X / 16, _body.Position.Y / 16,
            0, direction, action, new LiveWallpaperLinkInput(_input.Move, _input.Jump),
            hookshotVisible: _hookshotActive,
            hookshotMapX: _hookshotPosition.X / 16,
            hookshotMapY: _hookshotPosition.Y / 16);
    }

    private bool TryQueueHookshotRoute()
    {
        if (_map.HookshotTargets.Count == 0)
            return false;
        foreach (var target in _map.HookshotTargets)
        {
            // ObjLink2d fires four pixels above its body anchor. Use the
            // lowest valid point on the installed grip first so a grounded
            // firing position remains physically reachable.
            var contactY = target.Y + Math.Max(1f, target.Height - 4f);
            foreach (var fireRight in new[] { true, false })
            for (var distance = 96f; distance >= 48f; distance -= 16f)
            {
                var contactX = fireRight ? target.X : target.X + target.Width;
                var shot = new Vector2(
                    fireRight ? contactX - distance : contactX + distance,
                    contactY + 4f);
                if (!_map.SideViewPositionInBounds(shot))
                    continue;
                var stagedBody = LiveWallpaperSideViewPhysics.Spawn(_map, shot);
                if (!stagedBody.Grounded && !stagedBody.Climbing &&
                    !stagedBody.Swimming)
                    continue;
                var hand = shot + new Vector2(fireRight ? 5f : -5f, -4f);
                var contact = new Vector2(contactX, contactY);
                var length = Vector2.Distance(hand, contact);
                if (length < 40f ||
                    length > LinkGameplayMotion.HookshotMaximumDistance ||
                    !HasClearHookshotLine(hand, contact))
                    continue;
                var landing = new Vector2(
                    fireRight ? target.X - 4f : target.X + target.Width + 4f,
                    shot.Y);
                if (!_map.SideViewPositionInBounds(landing) ||
                    _map.IntersectsCollision(
                        landing.X - 4f, landing.Y - 10f, 8f, 10f,
                        includeHoles: false))
                    continue;
                _target = shot;
                _hookshotPosition = hand;
                _hookshotContact = contact;
                _hookshotLanding = landing;
                _hookshotFireRight = fireRight;
                _hookshotStaging = true;
                _noRoute = false;
                _allowReturn = false;
                return true;
            }
        }
        return false;
    }

    private bool HasClearHookshotLine(Vector2 start, Vector2 contact)
    {
        var delta = contact - start;
        var distance = delta.Length();
        if (distance <= 0f)
            return false;
        var direction = delta / distance;
        for (var travelled = 0f;
             travelled + 3f < distance;
             travelled += LinkGameplayMotion.HookshotSpeed)
        {
            var point = start + direction * travelled;
            if (_map.IntersectsCollision(
                    point.X - 2f, point.Y - 2f, 4f, 4f,
                    includeHoles: false))
                return false;
        }
        return true;
    }

    private void AdvanceHookshot()
    {
        var goal = _hookshotPulling ? _hookshotLanding : _hookshotContact;
        var current = _hookshotPulling ? _body.Position : _hookshotPosition;
        var delta = goal - current;
        if (delta.LengthSquared() <=
            LinkGameplayMotion.HookshotSpeed *
            LinkGameplayMotion.HookshotSpeed)
        {
            if (!_hookshotPulling)
            {
                _hookshotPosition = _hookshotContact;
                _hookshotPulling = true;
                return;
            }
            _body = LiveWallpaperSideViewPhysics.Spawn(
                _map, _hookshotLanding);
            _hookshotPosition = _hookshotContact;
            _hookshotActive = false;
            _hookshotPulling = false;
            _route = [];
            _routeIndex = 0;
            _search = null;
            _nextSearch = (_lastTime ?? 0L) + 650L;
            _noRoute = false;
            return;
        }
        var step = Vector2.Normalize(delta) * LinkGameplayMotion.HookshotSpeed;
        if (_hookshotPulling)
            _body.Position += step;
        else
            _hookshotPosition += step;
    }
}
