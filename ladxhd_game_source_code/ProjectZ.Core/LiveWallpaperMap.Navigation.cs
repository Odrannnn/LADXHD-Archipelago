using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectZ;

public sealed partial class LiveWallpaperMap
{
    private IReadOnlyDictionary<int, LiveWallpaperActorState> _navigationActors;
    private IReadOnlyDictionary<int, LiveWallpaperEnemyState> _navigationEnemies;
    private IReadOnlyDictionary<(Point From, Point To), long> _navigationFailedSteps;

    internal bool TryGetActorBody(int index, out LiveWallpaperCollisionBounds body, bool ignoreOwl = false)
    {
        body = default;
        if (index < 0 || index >= Actors.Count) return false;
        var actor = Actors[index];
        if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0 ||
            ignoreOwl && actor.Kind == LiveWallpaperMapActorKind.Owl) return false;
        body = new LiveWallpaperCollisionBounds(actor.BodyX, actor.BodyY, actor.BodyWidth, actor.BodyHeight);
        if (_navigationActors != null && _navigationActors.TryGetValue(index, out var state))
        {
            if (!state.BlocksMovement) return false;
            if (TryGetLiveActorBody(actor, state, out var liveBody))
                body = new LiveWallpaperCollisionBounds(liveBody.X, liveBody.Y, liveBody.Width, liveBody.Height);
        }
        return true;
    }

    internal bool TryGetEnemyBody(int index, out LiveWallpaperCollisionBounds body)
    {
        body = default;
        if (index < 0 || index >= Enemies.Count) return false;
        var enemy = Enemies[index];
        if (enemy.BodyWidth <= 0 || enemy.BodyHeight <= 0) return false;
        var x = (float)enemy.BodyX;
        var y = (float)enemy.BodyY;
        if (_navigationEnemies?.Count > 0)
        {
            // Match runtime culling: after live simulation starts, only its
            // resolved bodies participate; hidden/dead entries remain absent.
            if (!_navigationEnemies.TryGetValue(index, out var state) || !state.Visible) return false;
            x = state.PixelX + enemy.BodyX - enemy.EntityX;
            y = state.PixelY + enemy.BodyY - enemy.EntityY;
        }
        body = new LiveWallpaperCollisionBounds(x, y, enemy.BodyWidth, enemy.BodyHeight);
        return true;
    }

    // Installed bodies are translated from the actor simulation's own spawn
    // origin, including Mouse's different Y origin. Static states translate by
    // zero; movement and planning no longer maintain separate actor-kind lists.
    internal static bool TryGetLiveActorBody(LiveWallpaperMapActor actor, LiveWallpaperActorState state,
        out (float X, float Y, float Width, float Height) body)
    {
        if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0)
        {
            body = default;
            return false;
        }
        var spawn = LiveWallpaperActorSimulation.Session.GetSpawn(actor);
        body = (state.EntityX + actor.BodyX - spawn.X, state.EntityY + actor.BodyY - spawn.Y,
            actor.BodyWidth, actor.BodyHeight);
        return true;
    }

    internal int GetNavigationStepPenalty(Point from, Point to) =>
        _navigationFailedSteps?.Count > 0 &&
        (_navigationFailedSteps.ContainsKey((from, to)) || _navigationFailedSteps.ContainsKey((to, from)))
            ? 12 : 0;
}
