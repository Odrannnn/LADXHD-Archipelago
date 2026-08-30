using System;
using System.Collections.Generic;
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

    public enum LiveWallpaperEnemyProjectileKind
    {
        None,
        OctorokShot,
        Spear,
        Fireball
    }

    public readonly struct LiveWallpaperEnemyProjectileState
    {
        public LiveWallpaperEnemyProjectileState(
            LiveWallpaperEnemyProjectileKind kind,
            float pixelX, float pixelY, float height, int direction)
        {
            Kind = kind;
            PixelX = pixelX;
            PixelY = pixelY;
            Height = height;
            Direction = Math.Clamp(direction, 0, 3);
        }

        public LiveWallpaperEnemyProjectileKind Kind { get; }
        public float PixelX { get; }
        public float PixelY { get; }
        public float Height { get; }
        public int Direction { get; }
        public bool Visible => Kind != LiveWallpaperEnemyProjectileKind.None;
    }

    public readonly struct LiveWallpaperLinkHit
    {
        public LiveWallpaperLinkHit(
            float sourcePixelX, float sourcePixelY,
            int damage, float pushMultiplier)
        {
            SourcePixelX = sourcePixelX;
            SourcePixelY = sourcePixelY;
            Damage = Math.Max(0, damage);
            PushMultiplier = Math.Max(0f, pushMultiplier);
        }

        public float SourcePixelX { get; }
        public float SourcePixelY { get; }
        public int Damage { get; }
        public float PushMultiplier { get; }
        public bool Valid => Damage > 0;
    }

    public readonly struct LiveWallpaperEnemyState
    {
        public LiveWallpaperEnemyState(
            float pixelX, float pixelY, int direction,
            LiveWallpaperEnemyAction action,
            LiveWallpaperEnemyProjectileState projectile = default,
            LiveWallpaperLinkHit linkHit = default)
        {
            PixelX = pixelX;
            PixelY = pixelY;
            Direction = Math.Clamp(direction, 0, 3);
            Action = action;
            Projectile = projectile;
            LinkHit = linkHit;
        }

        public float PixelX { get; }
        public float PixelY { get; }
        public int Direction { get; }
        public LiveWallpaperEnemyAction Action { get; }
        public LiveWallpaperEnemyProjectileState Projectile { get; }
        public LiveWallpaperLinkHit LinkHit { get; }
        public bool Visible => Action != LiveWallpaperEnemyAction.Hidden;
    }

    /// <summary>
    /// Deterministic, side-effect-free wallpaper interpretation of the installed enemy spawn
    /// objects. Speeds and cardinal direction conventions mirror the corresponding game actors;
    /// no enemy deaths, drops, save flags, or map events are persisted.
    /// </summary>
    public static class LiveWallpaperEnemySimulation
    {
        public sealed class Session
        {
            private sealed class Runtime
            {
                public bool Initialized;
                public long LastElapsed;
                public float X;
                public float Y;
                public float VelocityX;
                public float VelocityY;
                public float Timer;
                public float Cooldown;
                public float Progress;
                public float RotationVelocity;
                public float AttackCooldown;
                public float ProjectileX;
                public float ProjectileY;
                public float ProjectileStartX;
                public float ProjectileStartY;
                public float ProjectileVelocityX;
                public float ProjectileVelocityY;
                public float ProjectileTimer;
                public float ProjectileHeight;
                public int Direction;
                public int Phase;
                public int Lives;
                public int ProjectileDirection;
                public LiveWallpaperEnemyProjectileKind ProjectileKind;
                public uint RandomState;
                public LiveWallpaperEnemyAction Action;
                public bool SwordHitApplied;
                public int LastStoneImpactSerial;
                public bool Dead;
                public LiveWallpaperLinkHit PendingLinkHit;
            }

            private readonly Dictionary<int, Runtime> _runtime = new();
            private LiveWallpaperMap _map;

            public LiveWallpaperEnemyState Resolve(
                LiveWallpaperMap map,
                int enemyIndex,
                long elapsedMilliseconds,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (map == null || enemyIndex < 0 || enemyIndex >= map.Enemies.Count)
                    return default;
                if (!ReferenceEquals(_map, map))
                {
                    _map = map;
                    _runtime.Clear();
                }

                var enemy = map.Enemies[enemyIndex];
                if (!IsSpawnTerrainValid(map, enemy))
                    return AtSpawn(enemy, 3, LiveWallpaperEnemyAction.Hidden);
                if (!_runtime.TryGetValue(enemyIndex, out var runtime))
                {
                    runtime = new Runtime();
                    _runtime.Add(enemyIndex, runtime);
                }
                if (!runtime.Initialized || elapsedMilliseconds < runtime.LastElapsed)
                    Initialize(runtime, enemy, enemyIndex, elapsedMilliseconds);

                runtime.PendingLinkHit = default;

                if (link.HasValue &&
                    link.Value.StoneImpactKind ==
                        LiveWallpaperStoneImpactKind.Enemy &&
                    link.Value.StoneImpactEnemyIndex == enemyIndex &&
                    link.Value.StoneImpactSerial > 0 &&
                    runtime.LastStoneImpactSerial !=
                        link.Value.StoneImpactSerial)
                {
                    runtime.LastStoneImpactSerial =
                        link.Value.StoneImpactSerial;
                    if (!runtime.Dead)
                        ApplyThrownStoneHit(runtime, link.Value);
                }

                var swordAttack = link?.CombatEnemyIndex == enemyIndex &&
                                  link.Value.Action == LiveWallpaperLinkRouteAction.Attack;
                if (!swordAttack)
                    runtime.SwordHitApplied = false;
                else if (!runtime.SwordHitApplied &&
                         SwordIntersectsEnemy(runtime, enemy, link.Value) &&
                         runtime.Cooldown <= 0 && !runtime.Dead)
                {
                    ApplySwordHit(runtime, link.Value);
                    runtime.SwordHitApplied = true;
                }

                var remaining = Math.Clamp(
                    elapsedMilliseconds - runtime.LastElapsed, 0L, 250L);
                runtime.LastElapsed = elapsedMilliseconds;
                while (remaining > 0)
                {
                    var step = Math.Min(1000f / 60f, remaining);
                    Update(runtime, map, enemy, enemyIndex, step, link);
                    remaining -= (long)Math.Ceiling(step);
                }

                if (link.HasValue && runtime.Cooldown <= 0 && !runtime.Dead)
                    TryHitLinkByContact(runtime, enemy.Kind, link.Value);

                if (runtime.Dead && runtime.Cooldown <= 0)
                    return new LiveWallpaperEnemyState(
                        runtime.X, runtime.Y, runtime.Direction,
                        LiveWallpaperEnemyAction.Hidden, GetProjectile(runtime),
                        runtime.PendingLinkHit);
                if (runtime.Cooldown > 0)
                {
                    // AiDamageState alternates the damage shader every 66 ms. The
                    // wallpaper has no shaders, so use the same cadence as visibility.
                    var blink = (int)(runtime.Cooldown / 66f) % 2 == 0;
                    return new LiveWallpaperEnemyState(
                        runtime.X, runtime.Y, runtime.Direction,
                        blink ? LiveWallpaperEnemyAction.Hidden :
                            LiveWallpaperEnemyAction.Hit, GetProjectile(runtime),
                        runtime.PendingLinkHit);
                }
                return new LiveWallpaperEnemyState(
                    runtime.X, runtime.Y, runtime.Direction, runtime.Action,
                    GetProjectile(runtime), runtime.PendingLinkHit);
            }

            private static void Initialize(
                Runtime runtime, LiveWallpaperMapEnemy enemy,
                int enemyIndex, long elapsed)
            {
                runtime.Initialized = true;
                runtime.LastElapsed = elapsed;
                runtime.X = enemy.EntityX;
                runtime.Y = enemy.EntityY;
                runtime.VelocityX = 0;
                runtime.VelocityY = 0;
                runtime.Direction = 3;
                runtime.Phase = 0;
                runtime.Progress = 0;
                runtime.Cooldown = 0;
                runtime.AttackCooldown = 2000;
                runtime.ProjectileKind = LiveWallpaperEnemyProjectileKind.None;
                runtime.Lives = GetLives(enemy.Kind);
                runtime.SwordHitApplied = false;
                runtime.LastStoneImpactSerial = 0;
                runtime.Dead = false;
                runtime.PendingLinkHit = default;
                runtime.RandomState = (uint)(enemyIndex + 1) * 747796405u + 2891336453u;
                switch (enemy.Kind)
                {
                    case LiveWallpaperMapEnemyKind.SeaUrchin:
                        runtime.Action = LiveWallpaperEnemyAction.Idle;
                        runtime.Timer = float.MaxValue;
                        break;
                    case LiveWallpaperMapEnemyKind.Leever:
                        runtime.Action = LiveWallpaperEnemyAction.Hidden;
                        runtime.Timer = Next(runtime, 750, 1500);
                        break;
                    case LiveWallpaperMapEnemyKind.RiverZora:
                        runtime.Action = LiveWallpaperEnemyAction.Hidden;
                        runtime.Timer = Next(runtime, 3500, 4500);
                        break;
                    case LiveWallpaperMapEnemyKind.Pincer:
                        runtime.Action = LiveWallpaperEnemyAction.Hidden;
                        runtime.Timer = 750;
                        break;
                    case LiveWallpaperMapEnemyKind.Ghini:
                        runtime.Action = LiveWallpaperEnemyAction.Walk;
                        runtime.Timer = 0;
                        break;
                    case LiveWallpaperMapEnemyKind.RedZol:
                        runtime.Action = LiveWallpaperEnemyAction.Idle;
                        runtime.Timer = 200;
                        break;
                    default:
                        BeginWalk(runtime, enemy.Kind);
                        break;
                }
            }

            private static void Update(
                Runtime runtime, LiveWallpaperMap map,
                LiveWallpaperMapEnemy enemy, int enemyIndex,
                float deltaMilliseconds, LiveWallpaperSimulatedLinkState? link)
            {
                runtime.Cooldown = Math.Max(0, runtime.Cooldown - deltaMilliseconds);
                runtime.AttackCooldown = Math.Max(
                    0, runtime.AttackCooldown - deltaMilliseconds);
                UpdateProjectile(runtime, map, deltaMilliseconds, link);
                if (runtime.Cooldown > 0 || runtime.Dead)
                    return;
                switch (enemy.Kind)
                {
                    case LiveWallpaperMapEnemyKind.SeaUrchin:
                        return;
                    case LiveWallpaperMapEnemyKind.Crab:
                        UpdateCrab(runtime, map, enemy, enemyIndex, deltaMilliseconds);
                        return;
                    case LiveWallpaperMapEnemyKind.Octorok:
                    case LiveWallpaperMapEnemyKind.Moblin:
                        UpdateWalker(runtime, map, enemy, enemyIndex,
                            deltaMilliseconds, link);
                        return;
                    case LiveWallpaperMapEnemyKind.MoblinSword:
                        UpdateMoblinSword(
                            runtime, map, enemy, enemyIndex, deltaMilliseconds, link);
                        return;
                    case LiveWallpaperMapEnemyKind.RedZol:
                        UpdateRedZol(
                            runtime, map, enemy, enemyIndex, deltaMilliseconds, link);
                        return;
                    case LiveWallpaperMapEnemyKind.Leever:
                        UpdateLeever(
                            runtime, map, enemy, enemyIndex, deltaMilliseconds, link);
                        return;
                    case LiveWallpaperMapEnemyKind.RiverZora:
                        UpdateRiverZora(
                            runtime, map, enemy, deltaMilliseconds, link);
                        return;
                    case LiveWallpaperMapEnemyKind.Ghini:
                        UpdateGhini(runtime, map, enemy, enemyIndex, deltaMilliseconds);
                        return;
                    case LiveWallpaperMapEnemyKind.Pincer:
                        UpdatePincer(runtime, enemy, deltaMilliseconds, link);
                        return;
                }
            }

            private static void UpdateCrab(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float delta)
            {
                runtime.Timer -= delta;
                if (runtime.Timer <= 0)
                    BeginWalk(runtime, LiveWallpaperMapEnemyKind.Crab);
                var speed = runtime.Direction % 2 == 0 ? 1f : 0.33f;
                Move(runtime, map, enemy, enemyIndex, speed, delta);
            }

            private static void UpdateWalker(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float delta, LiveWallpaperSimulatedLinkState? link)
            {
                runtime.Timer -= delta;
                if (runtime.Timer <= 0)
                {
                    if (runtime.Phase == 0)
                    {
                        runtime.Phase = 1;
                        runtime.Action = LiveWallpaperEnemyAction.Idle;
                        runtime.Timer = enemy.Kind == LiveWallpaperMapEnemyKind.Octorok
                            ? Next(runtime, 250, 500)
                            : Next(runtime, 300, 500);
                        if (link.HasValue)
                        {
                            if (enemy.Kind == LiveWallpaperMapEnemyKind.Octorok)
                                TryShootOctorok(runtime, map, link.Value);
                            else
                                TryThrowMoblinSpear(runtime, map, link.Value);
                        }
                    }
                    else
                    {
                        BeginWalk(runtime, enemy.Kind);
                    }
                }
                if (runtime.Phase == 0)
                    Move(runtime, map, enemy, enemyIndex, 0.5f, delta);
            }

            private static void UpdateMoblinSword(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float delta, LiveWallpaperSimulatedLinkState? link)
            {
                if (link.HasValue)
                {
                    var target = LinkPosition(link.Value);
                    var distance = target - new Vector2(runtime.X, runtime.Y);
                    var range = runtime.Phase == 2 ? 65f : 50f;
                    if (distance.LengthSquared() < range * range)
                    {
                        runtime.Phase = 2;
                        runtime.Action = LiveWallpaperEnemyAction.Attack;
                        runtime.Direction = DirectionTo(
                            runtime.X, runtime.Y, target.X, target.Y);
                        MoveToward(runtime, map, enemy, enemyIndex, target, 0.55f, delta);
                        return;
                    }
                    if (runtime.Phase == 2)
                    {
                        runtime.Phase = 1;
                        runtime.Action = LiveWallpaperEnemyAction.Idle;
                        runtime.Timer = Next(runtime, 300, 500);
                    }
                }
                UpdateWalker(runtime, map, enemy, enemyIndex, delta, link);
            }

            private static void UpdateRedZol(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float delta, LiveWallpaperSimulatedLinkState? link)
            {
                runtime.Timer -= delta;
                if (runtime.Phase == 0 && runtime.Timer <= 0)
                {
                    runtime.Phase = Next(runtime, 0, 10) == 0 ? 2 : 1;
                    runtime.Action = runtime.Phase == 2
                        ? LiveWallpaperEnemyAction.Idle
                        : LiveWallpaperEnemyAction.Walk;
                    runtime.Timer = runtime.Phase == 2 ? 1000 : 132;
                }
                else if (runtime.Phase == 1 && runtime.Timer <= 0)
                {
                    runtime.Phase = 0;
                    runtime.Action = LiveWallpaperEnemyAction.Idle;
                    runtime.Timer = 200;
                }
                else if (runtime.Phase == 2 && runtime.Timer <= 0)
                {
                    runtime.Phase = 3;
                    runtime.Action = LiveWallpaperEnemyAction.Walk;
                    runtime.Timer = 520;
                }
                else if (runtime.Phase == 3 && runtime.Timer <= 0)
                {
                    runtime.Phase = 0;
                    runtime.Action = LiveWallpaperEnemyAction.Idle;
                    runtime.Timer = 200;
                }
                if (link.HasValue && runtime.Phase is 1 or 3)
                {
                    MoveToward(runtime, map, enemy, enemyIndex,
                        LinkPosition(link.Value), runtime.Phase == 3 ? 1.25f : 0.4f,
                        delta);
                }
            }

            private static void UpdateLeever(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float delta, LiveWallpaperSimulatedLinkState? link)
            {
                runtime.Timer -= delta;
                if (runtime.Timer <= 0)
                {
                    switch (runtime.Phase)
                    {
                        case 0:
                            RelocateInField(runtime, map, enemy, requireWater: false);
                            runtime.Phase = 1;
                            runtime.Action = LiveWallpaperEnemyAction.Spawn;
                            runtime.Timer = 500;
                            break;
                        case 1:
                            runtime.Phase = 2;
                            runtime.Action = LiveWallpaperEnemyAction.Walk;
                            runtime.Timer = Next(runtime, 2000, 3000);
                            break;
                        case 2:
                            runtime.Phase = 3;
                            runtime.Action = LiveWallpaperEnemyAction.Leave;
                            runtime.Timer = 500;
                            break;
                        case 3:
                            runtime.Phase = 0;
                            runtime.Action = LiveWallpaperEnemyAction.Hidden;
                            runtime.Timer = Next(runtime, 1000, 2000);
                            break;
                    }
                }
                if (runtime.Phase == 2 && link.HasValue)
                    MoveToward(runtime, map, enemy, enemyIndex,
                        LinkPosition(link.Value), 0.5f, delta);
            }

            private static void UpdateRiverZora(
                Runtime runtime, LiveWallpaperMap map,
                LiveWallpaperMapEnemy enemy, float delta,
                LiveWallpaperSimulatedLinkState? link)
            {
                runtime.Timer -= delta;
                if (runtime.Timer > 0)
                    return;
                switch (runtime.Phase)
                {
                    case 0:
                        RelocateInField(runtime, map, enemy, requireWater: true);
                        runtime.Phase = 1;
                        runtime.Action = LiveWallpaperEnemyAction.Spawn;
                        runtime.Timer = 2000;
                        break;
                    case 1:
                        runtime.Phase = 2;
                        runtime.Action = LiveWallpaperEnemyAction.Idle;
                        runtime.Timer = Next(runtime, 500, 1000);
                        break;
                    case 2:
                        runtime.Phase = 3;
                        runtime.Action = LiveWallpaperEnemyAction.Attack;
                        runtime.Timer = 600;
                        if (link.HasValue)
                            SpawnFireball(runtime, link.Value);
                        break;
                    case 3:
                        runtime.Phase = 4;
                        runtime.Action = LiveWallpaperEnemyAction.Idle;
                        runtime.Timer = 500;
                        break;
                    default:
                        runtime.Phase = 0;
                        runtime.Action = LiveWallpaperEnemyAction.Hidden;
                        runtime.Timer = Next(runtime, 3500, 4500);
                        break;
                }
            }

            private static void UpdateGhini(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float delta)
            {
                runtime.Timer -= delta;
                var timeMultiplier = delta / (1000f / 60f);
                if (runtime.Timer <= 0)
                {
                    var centerDelta = new Vector2(
                        enemy.EntityX - runtime.X, enemy.EntityY - runtime.Y);
                    var radiusToCenter = MathF.Atan2(centerDelta.Y, centerDelta.X);
                    var distanceMultiplier = Math.Clamp(Math.Min(
                        (85f - MathF.Abs(centerDelta.X)) / 85f,
                        (55f - MathF.Abs(centerDelta.Y)) / 55f), 0f, 1f);
                    var spread = MathF.PI - Next(runtime, 0, 628) / 100f;
                    runtime.Progress = radiusToCenter + spread * distanceMultiplier;
                    runtime.Timer = Next(runtime, 750, 1500) *
                                    (distanceMultiplier * 0.5f + 0.5f);
                    runtime.RotationVelocity =
                        (Next(runtime, -100, 100) / 1000f) * distanceMultiplier;
                }
                var damping = MathF.Pow(0.95f, timeMultiplier);
                runtime.VelocityX *= damping;
                runtime.VelocityY *= damping;
                runtime.VelocityX += MathF.Cos(runtime.Progress) * 0.035f * timeMultiplier;
                runtime.VelocityY += MathF.Sin(runtime.Progress) * 0.035f * timeMultiplier;
                runtime.Progress += runtime.RotationVelocity * timeMultiplier;
                runtime.Direction = runtime.VelocityX < 0 ? 0 : 2;
                TryMove(runtime, map, enemy, enemyIndex,
                    runtime.VelocityX * timeMultiplier,
                    runtime.VelocityY * timeMultiplier, includeHoles: false);
            }

            private static void UpdatePincer(
                Runtime runtime, LiveWallpaperMapEnemy enemy, float delta,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (!link.HasValue)
                {
                    runtime.Action = LiveWallpaperEnemyAction.Hidden;
                    return;
                }
                var target = LinkPosition(link.Value);
                var toLink = target - new Vector2(enemy.EntityX, enemy.EntityY);
                switch (runtime.Phase)
                {
                    case 0:
                        runtime.Timer = Math.Max(0, runtime.Timer - delta);
                        runtime.X = enemy.EntityX;
                        runtime.Y = enemy.EntityY;
                        runtime.Action = LiveWallpaperEnemyAction.Hidden;
                        if (runtime.Timer <= 0 && toLink.LengthSquared() < 36f * 36f)
                        {
                            runtime.Phase = 1;
                            runtime.Timer = 1000;
                            runtime.Action = LiveWallpaperEnemyAction.Idle;
                            runtime.Direction = DirectionTo(
                                enemy.EntityX, enemy.EntityY, target.X, target.Y);
                        }
                        break;
                    case 1:
                        runtime.Timer -= delta;
                        if (runtime.Timer <= 0)
                        {
                            runtime.Phase = 2;
                            runtime.Progress = 0;
                        }
                        break;
                    case 2:
                        runtime.Progress = Math.Min(1f,
                            runtime.Progress + delta / (21f * (1000f / 60f)));
                        SetPincerPosition(runtime, enemy, toLink, runtime.Progress);
                        runtime.Action = LiveWallpaperEnemyAction.Attack;
                        if (runtime.Progress >= 1)
                        {
                            runtime.Phase = 3;
                            runtime.Timer = 1000;
                        }
                        break;
                    case 3:
                        runtime.Timer -= delta;
                        runtime.Action = LiveWallpaperEnemyAction.Attack;
                        if (runtime.Timer <= 0)
                            runtime.Phase = 4;
                        break;
                    case 4:
                        runtime.Progress = Math.Max(0f,
                            runtime.Progress - delta / (33.6f * (1000f / 60f)));
                        SetPincerPosition(runtime, enemy, toLink, runtime.Progress);
                        runtime.Action = LiveWallpaperEnemyAction.Attack;
                        if (runtime.Progress <= 0)
                        {
                            runtime.Phase = 0;
                            runtime.Timer = 750;
                            runtime.Action = LiveWallpaperEnemyAction.Hidden;
                        }
                        break;
                }
            }

            private static void SetPincerPosition(
                Runtime runtime, LiveWallpaperMapEnemy enemy,
                Vector2 toLink, float progress)
            {
                var direction = toLink == Vector2.Zero
                    ? Vector2.Zero
                    : Vector2.Normalize(toLink);
                runtime.X = enemy.EntityX + direction.X * progress * 42f;
                runtime.Y = enemy.EntityY + direction.Y * progress * 42f;
                runtime.Direction = DirectionTo(
                    enemy.EntityX, enemy.EntityY, runtime.X, runtime.Y);
            }

            private static void BeginWalk(Runtime runtime, LiveWallpaperMapEnemyKind kind)
            {
                runtime.Phase = 0;
                runtime.Direction = Next(runtime, 0, 4);
                runtime.Action = LiveWallpaperEnemyAction.Walk;
                runtime.Timer = kind switch
                {
                    LiveWallpaperMapEnemyKind.Crab when runtime.Direction % 2 == 0 =>
                        Next(runtime, 1000, 1500),
                    LiveWallpaperMapEnemyKind.Crab => Next(runtime, 250, 750),
                    LiveWallpaperMapEnemyKind.Octorok => Next(runtime, 750, 1000),
                    _ => Next(runtime, 550, 850)
                };
            }

            private static void Move(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float speed, float delta)
            {
                var direction = DirectionVector(runtime.Direction);
                var timeMultiplier = delta / (1000f / 60f);
                TryMove(runtime, map, enemy, enemyIndex,
                    direction.X * speed * timeMultiplier,
                    direction.Y * speed * timeMultiplier);
            }

            private static void MoveToward(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, Vector2 target, float speed, float delta)
            {
                var direction = target - new Vector2(runtime.X, runtime.Y);
                if (direction == Vector2.Zero)
                    return;
                direction.Normalize();
                runtime.Direction = DirectionTo(
                    runtime.X, runtime.Y, target.X, target.Y);
                var timeMultiplier = delta / (1000f / 60f);
                TryMove(runtime, map, enemy, enemyIndex,
                    direction.X * speed * timeMultiplier,
                    direction.Y * speed * timeMultiplier);
            }

            private static void TryMove(
                Runtime runtime, LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
                int enemyIndex, float deltaX, float deltaY, bool includeHoles = true)
            {
                var nextX = runtime.X + deltaX;
                if (!IntersectsMap(
                        map, enemy, nextX, runtime.Y, enemyIndex, includeHoles))
                    runtime.X = nextX;
                else
                    runtime.Direction = Opposite(runtime.Direction);
                var nextY = runtime.Y + deltaY;
                if (!IntersectsMap(
                        map, enemy, runtime.X, nextY, enemyIndex, includeHoles))
                    runtime.Y = nextY;
                else
                    runtime.Direction = Opposite(runtime.Direction);
            }

            private static void RelocateInField(
                Runtime runtime, LiveWallpaperMap map,
                LiveWallpaperMapEnemy enemy, bool requireWater)
            {
                var fieldX = (enemy.EntityX / 160) * 160;
                var fieldY = (enemy.EntityY / 128) * 128;
                for (var attempt = 0; attempt < 25; attempt++)
                {
                    var x = fieldX + Next(runtime, 0, 10) * 16 + 8;
                    var y = fieldY + Next(runtime, 0, 8) * 16 +
                            (requireWater ? 6 : 16);
                    if (map.IsWaterAt(x, y) != requireWater ||
                        map.IntersectsVoid(x - 6, y - 10, 12, 10))
                        continue;
                    runtime.X = x;
                    runtime.Y = y;
                    return;
                }
                runtime.X = enemy.EntityX;
                runtime.Y = enemy.EntityY;
            }

            private static void ApplySwordHit(
                Runtime runtime, LiveWallpaperSimulatedLinkState link)
            {
                var target = LinkPosition(link);
                var direction = DirectionTo(
                    target.X, target.Y, runtime.X, runtime.Y);
                var push = DirectionVector(direction) * 5f;
                runtime.X += push.X;
                runtime.Y += push.Y;
                runtime.Direction = Opposite(direction);
                runtime.Lives--;
                runtime.Dead = runtime.Lives <= 0;
                runtime.Cooldown = 66f * 6f;
                runtime.Action = LiveWallpaperEnemyAction.Hit;
            }

            private static void ApplyThrownStoneHit(
                Runtime runtime, LiveWallpaperSimulatedLinkState link)
            {
                var direction = DirectionTo(
                    link.StoneImpactX, link.StoneImpactY,
                    runtime.X, runtime.Y);
                var push = DirectionVector(direction) * 5f;
                runtime.X += push.X;
                runtime.Y += push.Y;
                runtime.Direction = Opposite(direction);
                // ObjStone.Update passes damage=2 with HitType.ThrownObject.
                runtime.Lives -= 2;
                runtime.Dead = runtime.Lives <= 0;
                runtime.Cooldown = 66f * 6f;
                runtime.Action = LiveWallpaperEnemyAction.Hit;
            }

            private static bool SwordIntersectsEnemy(
                Runtime runtime, LiveWallpaperMapEnemy enemy,
                LiveWallpaperSimulatedLinkState link)
            {
                if (!link.AttackBox.Valid ||
                    runtime.Action == LiveWallpaperEnemyAction.Hidden)
                    return false;
                var offsetX = enemy.BodyX - enemy.EntityX;
                var offsetY = enemy.BodyY - enemy.EntityY;
                var width = enemy.BodyWidth;
                var height = enemy.BodyHeight;
                switch (enemy.Kind)
                {
                    case LiveWallpaperMapEnemyKind.SeaUrchin:
                        offsetX = -8;
                        offsetY = -16;
                        width = 16;
                        height = 16;
                        break;
                    case LiveWallpaperMapEnemyKind.Octorok:
                        offsetX = -7;
                        offsetY = -15;
                        width = 14;
                        height = 15;
                        break;
                    case LiveWallpaperMapEnemyKind.Leever:
                        offsetX = -7;
                        offsetY = -14;
                        width = 14;
                        height = 14;
                        break;
                    case LiveWallpaperMapEnemyKind.Crab:
                        offsetX = -8;
                        offsetY = -15;
                        width = 16;
                        height = 15;
                        break;
                    case LiveWallpaperMapEnemyKind.Moblin:
                        offsetX = -7;
                        offsetY = -15;
                        width = 14;
                        height = 15;
                        break;
                    case LiveWallpaperMapEnemyKind.MoblinSword:
                        offsetX = -3;
                        offsetY = -12;
                        width = 6;
                        height = 8;
                        break;
                    case LiveWallpaperMapEnemyKind.RedZol:
                        offsetX = -6;
                        offsetY = -10;
                        width = 12;
                        height = 10;
                        break;
                    case LiveWallpaperMapEnemyKind.RiverZora:
                        offsetX = -6;
                        offsetY = -5;
                        width = 12;
                        height = 10;
                        break;
                    case LiveWallpaperMapEnemyKind.Ghini:
                        offsetX = -3;
                        offsetY = -10;
                        width = 6;
                        height = 6;
                        break;
                    case LiveWallpaperMapEnemyKind.Pincer:
                        offsetX = -7;
                        offsetY = -7;
                        width = 14;
                        height = 14;
                        break;
                }
                return link.AttackBox.Intersects(
                    runtime.X + offsetX, runtime.Y + offsetY, width, height);
            }

            private static void TryShootOctorok(
                Runtime runtime, LiveWallpaperMap map,
                LiveWallpaperSimulatedLinkState link)
            {
                if (runtime.AttackCooldown > 0 ||
                    runtime.ProjectileKind != LiveWallpaperEnemyProjectileKind.None)
                    return;
                var target = LinkPosition(link);
                var distance = target - new Vector2(runtime.X, runtime.Y);
                if (distance.LengthSquared() >= 80f * 80f ||
                    DirectionTo(runtime.X, runtime.Y, target.X, target.Y) !=
                    runtime.Direction)
                    return;
                var offsets = new[]
                {
                    new Vector2(-8, -1), new Vector2(0, -6),
                    new Vector2(8, -1), new Vector2(0, 11)
                };
                SpawnProjectile(runtime,
                    LiveWallpaperEnemyProjectileKind.OctorokShot,
                    new Vector2(runtime.X, runtime.Y) + offsets[runtime.Direction],
                    DirectionVector(runtime.Direction) * 2f,
                    runtime.Direction, 950, 2);
                runtime.AttackCooldown = 2000;
            }

            private static void TryThrowMoblinSpear(
                Runtime runtime, LiveWallpaperMap map,
                LiveWallpaperSimulatedLinkState link)
            {
                if (runtime.AttackCooldown > 0 ||
                    runtime.ProjectileKind != LiveWallpaperEnemyProjectileKind.None ||
                    Next(runtime, 0, 2) == 0)
                    return;
                var target = LinkPosition(link);
                var fieldX = MathF.Floor(runtime.X / 160f) * 160f;
                var fieldY = MathF.Floor(runtime.Y / 128f) * 128f;
                if (target.X < fieldX || target.X >= fieldX + 160f ||
                    target.Y < fieldY || target.Y >= fieldY + 128f)
                    return;
                var distance = target - new Vector2(runtime.X, runtime.Y);
                if (distance.LengthSquared() >= 160f * 160f ||
                    DirectionTo(runtime.X, runtime.Y, target.X, target.Y) !=
                    runtime.Direction)
                    return;
                var offsets = new[]
                {
                    new Vector2(-8, -3), new Vector2(0, -3),
                    new Vector2(8, -3), new Vector2(0, 2)
                };
                SpawnProjectile(runtime,
                    LiveWallpaperEnemyProjectileKind.Spear,
                    new Vector2(runtime.X, runtime.Y) + offsets[runtime.Direction],
                    DirectionVector(runtime.Direction) * 2f,
                    runtime.Direction, float.MaxValue, 3);
                runtime.AttackCooldown = 2000;
            }

            private static void SpawnFireball(
                Runtime runtime, LiveWallpaperSimulatedLinkState link)
            {
                var target = LinkPosition(link) + new Vector2(0, -4);
                var velocity = target - new Vector2(runtime.X, runtime.Y);
                if (velocity != Vector2.Zero)
                    velocity.Normalize();
                SpawnProjectile(runtime,
                    LiveWallpaperEnemyProjectileKind.Fireball,
                    new Vector2(runtime.X, runtime.Y), velocity * 1.5f,
                    DirectionTo(runtime.X, runtime.Y, target.X, target.Y),
                    2500, 0);
            }

            private static void SpawnProjectile(
                Runtime runtime, LiveWallpaperEnemyProjectileKind kind,
                Vector2 position, Vector2 velocity, int direction,
                float timer, float height)
            {
                runtime.ProjectileKind = kind;
                runtime.ProjectileX = runtime.ProjectileStartX = position.X;
                runtime.ProjectileY = runtime.ProjectileStartY = position.Y;
                runtime.ProjectileVelocityX = velocity.X;
                runtime.ProjectileVelocityY = velocity.Y;
                runtime.ProjectileDirection = direction;
                runtime.ProjectileTimer = timer;
                runtime.ProjectileHeight = height;
            }

            private static void UpdateProjectile(
                Runtime runtime, LiveWallpaperMap map, float delta,
                LiveWallpaperSimulatedLinkState? link)
            {
                if (runtime.ProjectileKind == LiveWallpaperEnemyProjectileKind.None)
                    return;
                var timeMultiplier = delta / (1000f / 60f);
                runtime.ProjectileX += runtime.ProjectileVelocityX * timeMultiplier;
                runtime.ProjectileY += runtime.ProjectileVelocityY * timeMultiplier;
                runtime.ProjectileTimer -= delta;

                var halfWidth = runtime.ProjectileKind ==
                                LiveWallpaperEnemyProjectileKind.Spear &&
                                runtime.ProjectileDirection % 2 == 0 ? 6f : 4f;
                var halfHeight = runtime.ProjectileKind ==
                                 LiveWallpaperEnemyProjectileKind.Spear &&
                                 runtime.ProjectileDirection % 2 != 0 ? 6f : 4f;
                var collision = runtime.ProjectileKind !=
                                    LiveWallpaperEnemyProjectileKind.Fireball &&
                                map.IntersectsCollision(
                                    runtime.ProjectileX - halfWidth,
                                    runtime.ProjectileY - halfHeight,
                                    halfWidth * 2, halfHeight * 2,
                                    includeHoles: false);
                if (runtime.ProjectileKind == LiveWallpaperEnemyProjectileKind.Spear &&
                    (MathF.Abs(runtime.ProjectileX - runtime.ProjectileStartX) > 112f ||
                     MathF.Abs(runtime.ProjectileY - runtime.ProjectileStartY) > 96f))
                    collision = true;
                var hitLink = false;
                if (link.HasValue)
                {
                    var target = LinkPosition(link.Value);
                    hitLink = RectanglesIntersect(
                        runtime.ProjectileX - halfWidth,
                        runtime.ProjectileY - halfHeight,
                        halfWidth * 2, halfHeight * 2,
                        target.X - 4, target.Y - 10, 8, 10);
                    collision |= hitLink;
                }
                if (hitLink)
                    runtime.PendingLinkHit = new LiveWallpaperLinkHit(
                        runtime.ProjectileX, runtime.ProjectileY, 2, 1.85f);
                if (runtime.ProjectileTimer <= 0 || collision)
                    runtime.ProjectileKind = LiveWallpaperEnemyProjectileKind.None;
            }

            private static void TryHitLinkByContact(
                Runtime runtime, LiveWallpaperMapEnemyKind kind,
                LiveWallpaperSimulatedLinkState link)
            {
                if (runtime.Action == LiveWallpaperEnemyAction.Hidden ||
                    kind == LiveWallpaperMapEnemyKind.RiverZora ||
                    kind == LiveWallpaperMapEnemyKind.Pincer && runtime.Phase < 2)
                    return;

                var offsetX = -3f;
                var offsetY = -8f;
                var width = 6f;
                var height = 6f;
                var damage = 2;
                var pushMultiplier = 1.85f;
                switch (kind)
                {
                    case LiveWallpaperMapEnemyKind.SeaUrchin:
                        offsetX = -8f;
                        offsetY = -16f;
                        width = 16f;
                        height = 16f;
                        pushMultiplier = 2f;
                        break;
                    case LiveWallpaperMapEnemyKind.RedZol:
                        offsetY = -6f;
                        height = 4f;
                        break;
                    case LiveWallpaperMapEnemyKind.Ghini:
                        offsetY = -10f;
                        damage = 4;
                        break;
                    case LiveWallpaperMapEnemyKind.Pincer:
                        offsetY = -3f;
                        damage = 4;
                        break;
                }

                var target = LinkPosition(link);
                if (RectanglesIntersect(
                        runtime.X + offsetX, runtime.Y + offsetY,
                        width, height,
                        target.X - 4f, target.Y - 10f, 8f, 10f))
                {
                    runtime.PendingLinkHit = new LiveWallpaperLinkHit(
                        runtime.X, runtime.Y, damage, pushMultiplier);
                }
            }

            private static bool RectanglesIntersect(
                float x0, float y0, float width0, float height0,
                float x1, float y1, float width1, float height1) =>
                x0 < x1 + width1 && x0 + width0 > x1 &&
                y0 < y1 + height1 && y0 + height0 > y1;

            private static LiveWallpaperEnemyProjectileState GetProjectile(Runtime runtime) =>
                runtime.ProjectileKind == LiveWallpaperEnemyProjectileKind.None
                    ? default
                    : new LiveWallpaperEnemyProjectileState(
                        runtime.ProjectileKind,
                        runtime.ProjectileX, runtime.ProjectileY,
                        runtime.ProjectileHeight, runtime.ProjectileDirection);

            private static int GetLives(LiveWallpaperMapEnemyKind kind) => kind switch
            {
                LiveWallpaperMapEnemyKind.SeaUrchin => 1,
                LiveWallpaperMapEnemyKind.Octorok => 1,
                LiveWallpaperMapEnemyKind.Leever => 2,
                LiveWallpaperMapEnemyKind.Crab => 2,
                LiveWallpaperMapEnemyKind.Moblin => 2,
                LiveWallpaperMapEnemyKind.MoblinSword => 2,
                LiveWallpaperMapEnemyKind.RedZol => 1,
                LiveWallpaperMapEnemyKind.RiverZora => 1,
                LiveWallpaperMapEnemyKind.Ghini => 8,
                _ => 2
            };

            private static Vector2 LinkPosition(LiveWallpaperSimulatedLinkState link) =>
                new(link.MapX * 16f, link.MapY * 16f);

            private static int Next(Runtime runtime, int minimum, int maximum)
            {
                runtime.RandomState = runtime.RandomState * 1664525u + 1013904223u;
                var range = Math.Max(1, maximum - minimum);
                return minimum + (int)(runtime.RandomState % (uint)range);
            }
        }

        public static LiveWallpaperEnemyState Resolve(
            LiveWallpaperMap map,
            int enemyIndex,
            long elapsedMilliseconds,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (map == null || enemyIndex < 0 || enemyIndex >= map.Enemies.Count)
                return default;
            var enemy = map.Enemies[enemyIndex];
            if (!IsSpawnTerrainValid(map, enemy))
                return AtSpawn(enemy, 3, LiveWallpaperEnemyAction.Hidden);
            var elapsed = Math.Max(0L, elapsedMilliseconds);
            if (link?.CombatEnemyIndex == enemyIndex &&
                link.Value.Action == LiveWallpaperLinkRouteAction.Attack)
            {
                var direction = DirectionTo(
                    enemy.EntityX, enemy.EntityY,
                    link.Value.MapX * 16f, link.Value.MapY * 16f);
                if (link.Value.ActionProgress >= 0.42f)
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
                LiveWallpaperMapEnemyKind.Leever => ResolveBurrower(
                    map, enemy, enemyIndex, elapsed),
                LiveWallpaperMapEnemyKind.RiverZora => ResolveRiverZora(
                    map, enemy, elapsed),
                LiveWallpaperMapEnemyKind.Pincer => ResolvePincer(enemy, elapsed, link),
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
            LiveWallpaperMap map, LiveWallpaperMapEnemy enemy,
            int enemyIndex, long elapsed)
        {
            var phase = elapsed % 5_000L;
            var action = phase switch
            {
                < 700L => LiveWallpaperEnemyAction.Spawn,
                < 3_500L => LiveWallpaperEnemyAction.Walk,
                < 4_200L => LiveWallpaperEnemyAction.Leave,
                _ => LiveWallpaperEnemyAction.Hidden
            };
            if (action != LiveWallpaperEnemyAction.Walk)
                return AtSpawn(enemy, 3, action);
            var cycle = (int)(elapsed / 5_000L);
            var direction = PositiveHash(enemyIndex, cycle) % 4;
            var distance = Math.Min(18f, Math.Max(0f, (phase - 700L) / 90f));
            var movement = DirectionVector(direction) * distance;
            var x = enemy.EntityX + movement.X;
            var y = enemy.EntityY + movement.Y;
            if (IntersectsMap(map, enemy, x, y, enemyIndex))
            {
                x = enemy.EntityX;
                y = enemy.EntityY;
            }
            return new LiveWallpaperEnemyState(x, y, direction, action);
        }

        private static LiveWallpaperEnemyState ResolveRiverZora(
            LiveWallpaperMap map, LiveWallpaperMapEnemy enemy, long elapsed)
        {
            if (!map.IsWaterAt(enemy.EntityX, enemy.EntityY))
                return AtSpawn(enemy, 3, LiveWallpaperEnemyAction.Hidden);
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
            LiveWallpaperMapEnemy enemy, long elapsed,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (!link.HasValue)
                return AtSpawn(enemy, 3, LiveWallpaperEnemyAction.Hidden);
            var linkX = link.Value.MapX * 16f;
            var linkY = link.Value.MapY * 16f;
            var delta = new Vector2(linkX - enemy.EntityX, linkY - enemy.EntityY);
            if (delta.LengthSquared() > 38f * 38f)
                return AtSpawn(enemy, 3, LiveWallpaperEnemyAction.Hidden);
            var direction = DirectionTo(enemy.EntityX, enemy.EntityY, linkX, linkY);
            var phase = elapsed % 3_200L;
            if (phase < 900L)
                return AtSpawn(enemy, direction, LiveWallpaperEnemyAction.Idle);
            var attackProgress = phase < 1_800L
                ? (phase - 900L) / 900f
                : phase < 2_500L
                    ? 1f
                    : 1f - (phase - 2_500L) / 700f;
            var movement = delta.LengthSquared() > 0.001f
                ? Vector2.Normalize(delta) * 34f * Math.Clamp(attackProgress, 0f, 1f)
                : Vector2.Zero;
            return new LiveWallpaperEnemyState(
                enemy.EntityX + movement.X, enemy.EntityY + movement.Y,
                direction, LiveWallpaperEnemyAction.Attack);
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
            var water = map.IsWaterAt(
                bodyX + enemy.BodyWidth * 0.5f,
                bodyY + enemy.BodyHeight * 0.5f);
            var invalidTerrain = map.IntersectsVoid(
                                     bodyX, bodyY, enemy.BodyWidth, enemy.BodyHeight) ||
                                 (enemy.Kind != LiveWallpaperMapEnemyKind.Ghini &&
                                  enemy.Kind != LiveWallpaperMapEnemyKind.RiverZora && water) ||
                                 (enemy.Kind == LiveWallpaperMapEnemyKind.RiverZora && !water);
            return invalidTerrain || map.IntersectsCollision(
                       bodyX, bodyY, enemy.BodyWidth, enemy.BodyHeight, includeHoles) ||
                   map.IntersectsActor(
                       bodyX, bodyY, enemy.BodyWidth, enemy.BodyHeight) ||
                   map.IntersectsEnemy(
                       bodyX, bodyY, enemy.BodyWidth, enemy.BodyHeight, enemyIndex);
        }

        private static bool IsSpawnTerrainValid(
            LiveWallpaperMap map, LiveWallpaperMapEnemy enemy)
        {
            if (map.IntersectsVoid(
                    enemy.BodyX, enemy.BodyY, enemy.BodyWidth, enemy.BodyHeight))
                return false;
            var water = map.IsWaterAt(
                enemy.BodyX + enemy.BodyWidth * 0.5f,
                enemy.BodyY + enemy.BodyHeight * 0.5f);
            return enemy.Kind switch
            {
                LiveWallpaperMapEnemyKind.RiverZora => water,
                LiveWallpaperMapEnemyKind.Ghini => true,
                _ => !water
            };
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
