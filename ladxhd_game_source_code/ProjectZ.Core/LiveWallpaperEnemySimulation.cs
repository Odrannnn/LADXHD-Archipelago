using System;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    public enum LiveWallpaperEnemyAction
    {
        Idle,
        Walk,
        Attack,
        Spawn,
        Leave,
        Hit,
        Hidden
    }

    public readonly struct LiveWallpaperEnemyState
    {
        public LiveWallpaperEnemyState(
            float pixelX, float pixelY, int direction,
            LiveWallpaperEnemyAction action)
        {
            PixelX = pixelX;
            PixelY = pixelY;
            Direction = Math.Clamp(direction, 0, 3);
            Action = action;
        }

        public float PixelX { get; }
        public float PixelY { get; }
        public int Direction { get; }
        public LiveWallpaperEnemyAction Action { get; }
        public bool Visible => Action != LiveWallpaperEnemyAction.Hidden;
    }

    /// <summary>
    /// Deterministic, side-effect-free wallpaper interpretation of the installed enemy spawn
    /// objects. Speeds and cardinal direction conventions mirror the corresponding game actors;
    /// no enemy deaths, drops, save flags, or map events are persisted.
    /// </summary>
    public static class LiveWallpaperEnemySimulation
    {
        public static LiveWallpaperEnemyState Resolve(
            LiveWallpaperMap map,
            int enemyIndex,
            long elapsedMilliseconds,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (map == null || enemyIndex < 0 || enemyIndex >= map.Enemies.Count)
                return default;
            var enemy = map.Enemies[enemyIndex];
            var elapsed = Math.Max(0L, elapsedMilliseconds);
            if (link?.CombatEnemyIndex == enemyIndex)
            {
                var direction = DirectionTo(
                    enemy.EntityX, enemy.EntityY,
                    link.Value.MapX * 16f, link.Value.MapY * 16f);
                if (link.Value.Action == LiveWallpaperLinkRouteAction.Attack)
                {
                    var blink = elapsed / 70L % 2L != 0;
                    if (blink)
                        return new LiveWallpaperEnemyState(
                            enemy.EntityX, enemy.EntityY, direction,
                            LiveWallpaperEnemyAction.Hidden);
                    var push = DirectionVector(Opposite(direction)) * 5f;
                    return new LiveWallpaperEnemyState(
                        enemy.EntityX + push.X, enemy.EntityY + push.Y,
                        direction, LiveWallpaperEnemyAction.Hit);
                }
                return new LiveWallpaperEnemyState(
                    enemy.EntityX, enemy.EntityY, direction,
                    LiveWallpaperEnemyAction.Attack);
            }

            return enemy.Kind switch
            {
                LiveWallpaperMapEnemyKind.SeaUrchin => AtSpawn(
                    enemy, 3, LiveWallpaperEnemyAction.Idle),
                LiveWallpaperMapEnemyKind.Leever => ResolveBurrower(enemy, elapsed),
                LiveWallpaperMapEnemyKind.RiverZora => ResolveRiverZora(enemy, elapsed),
                LiveWallpaperMapEnemyKind.Pincer => ResolvePincer(enemy, elapsed),
                LiveWallpaperMapEnemyKind.Ghini => ResolveGhini(
                    map, enemy, enemyIndex, elapsed),
                LiveWallpaperMapEnemyKind.Crab => ResolveWalker(
                    map, enemy, enemyIndex, elapsed, horizontalOnly: true),
                LiveWallpaperMapEnemyKind.RedZol => ResolveWalker(
                    map, enemy, enemyIndex, elapsed, horizontalOnly: false, speed: 0.35f),
                _ => ResolveWalker(
                    map, enemy, enemyIndex, elapsed, horizontalOnly: false)
            };
        }

        private static LiveWallpaperEnemyState ResolveWalker(
            LiveWallpaperMap map,
            LiveWallpaperMapEnemy enemy,
            int enemyIndex,
            long elapsed,
            bool horizontalOnly,
            float speed = 0.5f)
        {
            const long legDuration = 1_200L;
            const long cycleDuration = legDuration * 2;
            var cycle = elapsed / cycleDuration;
            var position = elapsed % cycleDuration;
            var direction = horizontalOnly
                ? (PositiveHash(enemyIndex, (int)cycle) % 2 == 0 ? 0 : 2)
                : PositiveHash(enemyIndex, (int)cycle) % 4;
            var travelMilliseconds = position <= legDuration
                ? position
                : cycleDuration - position;
            var distance = travelMilliseconds / (1000f / 60f) * speed;
            var movement = DirectionVector(direction) * distance;
            var x = enemy.EntityX + movement.X;
            var y = enemy.EntityY + movement.Y;
            if (IntersectsMap(map, enemy, x, y, enemyIndex))
            {
                direction = Opposite(direction);
                movement = DirectionVector(direction) * Math.Min(12f, distance);
                x = enemy.EntityX + movement.X;
                y = enemy.EntityY + movement.Y;
                if (IntersectsMap(map, enemy, x, y, enemyIndex))
                {
                    x = enemy.EntityX;
                    y = enemy.EntityY;
                }
            }
            var resting = position > legDuration - 180L && position < legDuration + 180L;
            return new LiveWallpaperEnemyState(
                x, y, direction,
                resting ? LiveWallpaperEnemyAction.Idle : LiveWallpaperEnemyAction.Walk);
        }

        private static LiveWallpaperEnemyState ResolveBurrower(
            LiveWallpaperMapEnemy enemy, long elapsed)
        {
            var phase = elapsed % 5_000L;
            var action = phase switch
            {
                < 700L => LiveWallpaperEnemyAction.Spawn,
                < 3_500L => LiveWallpaperEnemyAction.Walk,
                < 4_200L => LiveWallpaperEnemyAction.Leave,
                _ => LiveWallpaperEnemyAction.Hidden
            };
            return AtSpawn(enemy, 3, action);
        }

        private static LiveWallpaperEnemyState ResolveRiverZora(
            LiveWallpaperMapEnemy enemy, long elapsed)
        {
            var phase = elapsed % 4_500L;
            var action = phase switch
            {
                < 600L => LiveWallpaperEnemyAction.Spawn,
                < 1_500L => LiveWallpaperEnemyAction.Attack,
                < 3_100L => LiveWallpaperEnemyAction.Idle,
                _ => LiveWallpaperEnemyAction.Hidden
            };
            return AtSpawn(enemy, 3, action);
        }

        private static LiveWallpaperEnemyState ResolvePincer(
            LiveWallpaperMapEnemy enemy, long elapsed)
        {
            var phase = elapsed % 3_600L;
            var direction = (int)(elapsed / 3_600L % 4L);
            var action = phase < 850L
                ? LiveWallpaperEnemyAction.Attack
                : phase < 2_700L
                    ? LiveWallpaperEnemyAction.Idle
                    : LiveWallpaperEnemyAction.Hidden;
            return AtSpawn(enemy, direction, action);
        }

        private static LiveWallpaperEnemyState ResolveGhini(
            LiveWallpaperMap map,
            LiveWallpaperMapEnemy enemy,
            int enemyIndex,
            long elapsed)
        {
            var phase = elapsed / 900f + enemyIndex * 0.73f;
            var x = enemy.EntityX + MathF.Cos(phase) * 18f;
            var y = enemy.EntityY + MathF.Sin(phase * 0.8f) * 12f;
            if (IntersectsMap(map, enemy, x, y, enemyIndex, includeHoles: false))
            {
                x = enemy.EntityX;
                y = enemy.EntityY;
            }
            return new LiveWallpaperEnemyState(
                x, y, MathF.Cos(phase) < 0 ? 0 : 2,
                LiveWallpaperEnemyAction.Walk);
        }

        private static LiveWallpaperEnemyState AtSpawn(
            LiveWallpaperMapEnemy enemy,
            int direction,
            LiveWallpaperEnemyAction action) =>
            new(enemy.EntityX, enemy.EntityY, direction, action);

        private static bool IntersectsMap(
            LiveWallpaperMap map,
            LiveWallpaperMapEnemy enemy,
            float entityX,
            float entityY,
            int enemyIndex,
            bool includeHoles = true)
        {
            var bodyX = entityX + enemy.BodyX - enemy.EntityX;
            var bodyY = entityY + enemy.BodyY - enemy.EntityY;
            return map.IntersectsCollision(
                       bodyX, bodyY, enemy.BodyWidth, enemy.BodyHeight, includeHoles) ||
                   map.IntersectsActor(
                       bodyX, bodyY, enemy.BodyWidth, enemy.BodyHeight) ||
                   map.IntersectsEnemy(
                       bodyX, bodyY, enemy.BodyWidth, enemy.BodyHeight, enemyIndex);
        }

        private static int DirectionTo(
            float fromX, float fromY, float toX, float toY)
        {
            var deltaX = toX - fromX;
            var deltaY = toY - fromY;
            if (MathF.Abs(deltaX) >= MathF.Abs(deltaY))
                return deltaX < 0 ? 0 : 2;
            return deltaY < 0 ? 1 : 3;
        }

        private static Vector2 DirectionVector(int direction) => direction switch
        {
            0 => new Vector2(-1, 0),
            1 => new Vector2(0, -1),
            2 => new Vector2(1, 0),
            _ => new Vector2(0, 1)
        };

        private static int Opposite(int direction) => (direction + 2) % 4;

        private static int PositiveHash(int first, int second)
        {
            unchecked
            {
                var value = first * 73856093 ^ second * 19349663 ^ 83492791;
                return value == int.MinValue ? int.MaxValue : Math.Abs(value);
            }
        }
    }
}
