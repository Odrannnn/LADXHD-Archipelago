using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ProjectZ;

// A bounded input search, advanced a little per wallpaper frame. Each edge is
// replayed by the same physical Step as runtime, including held feather input.
internal sealed class LiveWallpaperSideViewPlanner
{
    private const int InputFrames = 8;
    private const int MaximumNodes = 8000;
    private static readonly SideViewInput[] Inputs =
    [
        new(-Vector2.UnitX), new(Vector2.UnitX), new(-Vector2.UnitY), new(Vector2.UnitY),
        new(Vector2.Zero), new(-Vector2.UnitX, true), new(Vector2.UnitX, true),
        new(Vector2.Zero, true),
        new(Vector2.Normalize(new Vector2(-1, -1))), new(Vector2.Normalize(new Vector2(1, -1))),
        new(Vector2.Normalize(new Vector2(-1, 1))), new(Vector2.Normalize(new Vector2(1, 1)))
    ];
    private readonly record struct Key(int X, int Y, int Fall, int MoveX, int MoveY, int Flags, int JumpAge);
    private readonly record struct Node(SideViewBody Body, int Parent, SideViewInput Input, int Frames, int Cost, bool LeftEntry);
    private readonly LiveWallpaperMap _map;
    private readonly LiveWallpaperMapPortal[] _goals;
    private readonly Vector2? _target;
    private readonly LiveWallpaperMapPortal? _entry;
    private readonly List<Node> _nodes = [];
    private readonly Dictionary<Key, int> _costs = [];
    private readonly PriorityQueue<int, float> _open = new();
    private const int GuidanceSpacing = 4;
    private readonly int[] _guidance;
    private readonly int _guidanceWidth, _guidanceHeight;
    private int _nearest;
    private float _nearestDistance;
    public bool Complete { get; private set; }
    public bool ReachedGoal { get; private set; }
    public IReadOnlyList<SideViewInput> Route { get; private set; } = [];
    public int ExpandedNodes { get; private set; }

    public LiveWallpaperSideViewPlanner(LiveWallpaperMap map, SideViewBody start,
        string entryId, bool entryLatched, Vector2? target = null, bool allowReturn = false)
    {
        _map = map;
        _target = target;
        _entry = map.Portals.Where(p => p.EntryId == entryId).Select(p => (LiveWallpaperMapPortal?)p).FirstOrDefault();
        _goals = map.Portals.Where(p => p.HasDestination && !p.IsHoleTeleporter &&
            p.Mode is 0 or 1 or 3 && (allowReturn || p.EntryId != entryId)).ToArray();
        if (target.HasValue && Vector2.DistanceSquared(start.Position, target.Value) <= 9 &&
            (start.Grounded || start.Climbing || start.Swimming))
        {
            Complete = ReachedGoal = true;
            return;
        }
        if (_goals.Length == 0 && target == null)
            _goals = map.Portals.Where(p => p.HasDestination && !p.IsHoleTeleporter && p.Mode is 0 or 1 or 3).ToArray();
        _guidanceWidth = map.Width * 16 / GuidanceSpacing;
        _guidanceHeight = map.Height * 16 / GuidanceSpacing;
        _guidance = CreateGuidance();
        _nodes.Add(new Node(start, -1, default, 0, 0, !entryLatched));
        _nearestDistance = Distance(start.Position);
        _costs[GetKey(start, !entryLatched)] = 0;
        _open.Enqueue(0, _nearestDistance);
    }

    public void Advance(int budget = 32)
    {
        if (Complete) return;
        while (budget-- > 0 && _open.TryDequeue(out var index, out _))
        {
            var node = _nodes[index];
            if (_costs.TryGetValue(GetKey(node.Body, node.LeftEntry), out var bestCost) && bestCost < node.Cost) continue;
            ExpandedNodes++;
            foreach (var input in Inputs)
            {
                if (input.Move.X != 0 && input.Move.Y != 0 && !node.Body.Swimming) continue;
                // Pressing the feather in midair cannot start a jump. Retain
                // held/released input for an existing variable-height jump.
                if (input.Jump && (node.Body.Swimming ||
                    !node.Body.JumpHeld && !node.Body.Grounded && !node.Body.Climbing ||
                    node.Body.JumpHeld && !node.Body.VariableJump && node.Body.Grounded)) continue;
                // Vertical input in plain air is identical to neutral input;
                // it matters only where Link can grab a ladder (or swim).
                if (input.Move.Y != 0 && !node.Body.Swimming && !node.Body.Climbing &&
                    !node.Body.Grounded && !_map.TouchesSideViewLadder(node.Body.Position)) continue;
                var next = node.Body;
                var leftEntry = node.LeftEntry;
                var valid = true;
                var found = false;
                var frames = 0;
                for (; frames < InputFrames; frames++)
                {
                    valid = LiveWallpaperSideViewPhysics.Step(_map, ref next, input);
                    if (_entry.HasValue && !_entry.Value.TouchesSideViewTrigger(next.Position.X, next.Position.Y)) leftEntry = true;
                    // Test the actual trigger at every physics step, not only at
                    // a coarse route node or the next 15/30/60Hz rendered frame.
                    found = IsGoal(next, input, leftEntry);
                    // Runtime would leave immediately at any active doorway.
                    // Do not plan a route through a different exit and pretend
                    // that Link can continue moving inside this map afterward.
                    if (!found && TouchesActiveExit(next, input, leftEntry)) valid = false;
                    if (!valid || found) { frames++; break; }
                }
                if (!valid) continue;
                var cost = node.Cost + frames;
                var key = GetKey(next, leftEntry);
                if (!found && _costs.TryGetValue(key, out var oldCost) && oldCost <= cost) continue;
                _costs[key] = cost;
                var nextIndex = _nodes.Count;
                _nodes.Add(new Node(next, index, input, frames, cost, leftEntry));
                if (found) { Finish(nextIndex, true); return; }
                var distance = Distance(next.Position);
                // If the bounded search cannot finish a long crossing in one
                // batch, retain verified progress to a supported resting point.
                // Runtime can continue planning there without walking into air.
                if ((next.Grounded || next.Climbing || next.Swimming) && distance < _nearestDistance)
                {
                    _nearest = nextIndex;
                    _nearestDistance = distance;
                }
                // The wallpaper needs a feasible crossing, not the shortest
                // possible button sequence. Weighted A* favours progress so
                // its bounded work is not spent enumerating tiny jump variants.
                _open.Enqueue(nextIndex, cost + distance * 4);
                if (_nodes.Count >= MaximumNodes) { Finish(_nearest, false); return; }
            }
        }
        if (_open.Count == 0) Finish(_nearest, false);
    }

    private bool IsGoal(SideViewBody body, SideViewInput input, bool leftEntry)
    {
        if (_target.HasValue)
            return Vector2.DistanceSquared(body.Position, _target.Value) <= 9 &&
                   (body.Grounded || body.Climbing || body.Swimming);
        foreach (var portal in _goals)
        {
            if (!leftEntry && _entry.HasValue && portal.EntryId == _entry.Value.EntryId) continue;
            if (portal.ShouldActivateAt(body.Position.X, body.Position.Y, input.Move.Y,
                    input.Move.Y < 0 ? 1 : body.Direction, true, body.Grounded)) return true;
        }
        return false;
    }

    private bool TouchesActiveExit(SideViewBody body, SideViewInput input, bool leftEntry)
    {
        foreach (var portal in _map.Portals)
        {
            if (!portal.HasDestination || portal.IsHoleTeleporter || portal.Mode is not (0 or 1 or 3) ||
                !leftEntry && _entry.HasValue && portal.EntryId == _entry.Value.EntryId) continue;
            if (portal.ShouldActivateAt(body.Position.X, body.Position.Y, input.Move.Y,
                    body.Direction, true, body.Grounded)) return true;
        }
        return false;
    }

    private float Distance(Vector2 position)
    {
        if (_guidance != null)
        {
            var x = Math.Clamp((int)MathF.Round(position.X / GuidanceSpacing) - 1, 0, _guidanceWidth - 1);
            var y = Math.Clamp((int)MathF.Round(position.Y / GuidanceSpacing) - 1, 0, _guidanceHeight - 1);
            var distance = _guidance[y * _guidanceWidth + x];
            if (distance >= 0) return distance * GuidanceSpacing;
        }
        return DirectDistance(position);
    }

    private float DirectDistance(Vector2 position)
    {
        if (_target.HasValue) return Vector2.Distance(position, _target.Value);
        var best = float.MaxValue;
        foreach (var goal in _goals)
        {
            var x = Math.Clamp(position.X, goal.PixelX + 4, goal.PixelX + Math.Max(4, goal.Width - 4));
            var y = Math.Clamp(position.Y, goal.PixelY + 1, goal.PixelY + goal.Height + 9);
            best = Math.Min(best, Vector2.Distance(position, new Vector2(x, y)));
        }
        return best == float.MaxValue ? 0 : best;
    }

    // A relaxed distance field guides the input search around installed walls.
    // It does not authorize movement: every route edge still runs Step. Ladders,
    // gravity and one-way surfaces are deliberately left to that physical check.
    private int[] CreateGuidance()
    {
        var count = _guidanceWidth * _guidanceHeight;
        if (count > 65536) return null;
        var distances = new int[count];
        Array.Fill(distances, -1);
        var open = new Queue<int>();
        for (var y = 0; y < _guidanceHeight; y++)
        for (var x = 0; x < _guidanceWidth; x++)
        {
            var position = new Vector2((x + 1) * GuidanceSpacing, (y + 1) * GuidanceSpacing);
            var index = y * _guidanceWidth + x;
            if (!_map.SideViewPositionInBounds(position) ||
                _map.SideViewCollision(position, -1, false, out _))
            {
                distances[index] = -2;
                continue;
            }
            var goal = _target.HasValue && Vector2.DistanceSquared(position, _target.Value) <= 9;
            if (!_target.HasValue)
                foreach (var portal in _goals)
                    if (portal.TouchesSideViewTrigger(position.X, position.Y)) { goal = true; break; }
            if (goal) { distances[index] = 0; open.Enqueue(index); }
        }
        while (open.TryDequeue(out var index))
        {
            var x = index % _guidanceWidth;
            var y = index / _guidanceWidth;
            if (x > 0) Visit(index - 1, distances[index] + 1);
            if (x + 1 < _guidanceWidth) Visit(index + 1, distances[index] + 1);
            if (y > 0) Visit(index - _guidanceWidth, distances[index] + 1);
            if (y + 1 < _guidanceHeight) Visit(index + _guidanceWidth, distances[index] + 1);
        }
        return distances;

        void Visit(int index, int distance)
        {
            if (distances[index] != -1) return;
            distances[index] = distance;
            open.Enqueue(index);
        }
    }

    private static Key GetKey(SideViewBody b, bool leftEntry) => new(
        (int)MathF.Round(b.Position.X / 2), (int)MathF.Round(b.Position.Y / 2),
        (int)MathF.Round(b.FallVelocity * 5),
        (int)MathF.Round((b.Swimming ? b.SwimVelocity.X : b.Movement.X) * 5),
        (int)MathF.Round((b.Swimming ? b.SwimVelocity.Y : b.Movement.Y) * 5),
        (b.Grounded ? 1 : 0) | (b.Climbing ? 2 : 0) | (b.Swimming ? 4 : 0) |
        (b.JumpHeld ? 8 : 0) | (b.VariableJump ? 16 : 0) | (leftEntry ? 32 : 0), b.JumpAge / 4);

    private void Finish(int index, bool reached)
    {
        var nodes = new List<Node>();
        for (; index > 0; index = _nodes[index].Parent) nodes.Add(_nodes[index]);
        nodes.Reverse();
        var route = new List<SideViewInput>();
        foreach (var node in nodes)
            for (var frame = 0; frame < node.Frames; frame++) route.Add(node.Input);
        Route = route;
        ReachedGoal = reached;
        Complete = true;
    }
}
