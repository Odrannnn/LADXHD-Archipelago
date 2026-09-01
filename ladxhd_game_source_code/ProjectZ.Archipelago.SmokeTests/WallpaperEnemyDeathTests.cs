using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectZ;

internal static class WallpaperEnemyDeathTests
{
    public static void Run()
    {
        CheckDropTables();
        CheckCooldownAndStableDeath();
        CheckHiddenHurtIsNotDeath();
        CheckContinuousAttacksRespectCooldownWithoutPermanentImmunity();
        CheckSwordCooldownAndGeometry();
        CheckMoblinSwordContactKnockbackRecovers();
        Console.WriteLine("Wallpaper enemy-death checks passed.");
    }

    private static void CheckDropTables()
    {
        Check(EnemyDeathGameplay.RollDrop(LiveWallpaperMapEnemyKind.Octorok, (_, _) => 49) == "ruby" &&
              EnemyDeathGameplay.RollDrop(LiveWallpaperMapEnemyKind.Octorok, (_, _) => 50) == "",
            "Octorok uses the native normal-health 50% ruby table.");
        Check(EnemyDeathGameplay.RollDrop(LiveWallpaperMapEnemyKind.Pincer, (_, _) => 24) == "ruby" &&
              EnemyDeathGameplay.RollDrop(LiveWallpaperMapEnemyKind.Pincer, (_, _) => 25) == "",
            "Pincer uses the native normal-health 25% ruby table.");
        foreach (var kind in new[]
                 {
                     LiveWallpaperMapEnemyKind.Moblin,
                     LiveWallpaperMapEnemyKind.Crab,
                     LiveWallpaperMapEnemyKind.Leever
                 })
        {
            Check(EnemyDeathGameplay.RollDrop(kind, (_, _) => 24) == "heart" &&
                  EnemyDeathGameplay.RollDrop(kind, (_, _) => 25) == "",
                "Ordinary heart-droppers must keep their native 25% table.");
        }
        Check(EnemyDeathGameplay.RollDrop(LiveWallpaperMapEnemyKind.Ghini, (_, _) => 0) == "fairy",
            "Ghini's native fairy result must never be silently replaced with a heart.");
    }

    private static void CheckCooldownAndStableDeath()
    {
        var map = EnemyMap("e2");
        var session = new LiveWallpaperEnemySimulation.Session();
        session.Resolve(map, 0, 0, null);
        var attack = SwordAttack();
        var hit = session.Resolve(map, 0, 1, attack);
        Check(hit.DeathStartedAt < 0, "A terminal hit must first retain the native damage cooldown.");

        var duringCooldown = session.Resolve(map, 0, 250, attack);
        Check(duringCooldown.DeathStartedAt < 0 && duringCooldown.Action == LiveWallpaperEnemyAction.Hidden,
            "A damage-blink hidden sprite is not a death event.");

        var death = session.Resolve(map, 0, 500, attack);
        var enemy = map.Enemies.Single();
        Check(death.DeathStartedAt == 500 && death.Action == LiveWallpaperEnemyAction.Hidden &&
              death.DeathX == death.PixelX + enemy.BodyX - enemy.EntityX + enemy.BodyWidth / 2f &&
              death.DeathY == death.PixelY + enemy.BodyY - enemy.EntityY + enemy.BodyHeight / 2f,
            "Death must begin once the native 396 ms hit cooldown is finished.");
        var repeated = session.Resolve(map, 0, 750, attack);
        Check(repeated.DeathStartedAt == death.DeathStartedAt &&
              repeated.DeathX == death.DeathX && repeated.DeathY == death.DeathY &&
              repeated.DeathDrop == death.DeathDrop,
            "Resolving a dead enemy again must preserve one death event and one roll.");
    }

    private static void CheckHiddenHurtIsNotDeath()
    {
        var rendererHidden = new LiveWallpaperEnemyState(80, 80, 3,
            LiveWallpaperEnemyAction.Hidden);
        Check(rendererHidden.DeathStartedAt == -1 && rendererHidden.DeathDrop == null,
            "Renderer-only hidden states must not imply an enemy death.");

        var map = EnemyMap("e5");
        var session = new LiveWallpaperEnemySimulation.Session();
        session.Resolve(map, 0, 0, null);
        var hurt = session.Resolve(map, 0, 1, SwordAttack());
        Check(hurt.DeathStartedAt < 0,
            "A non-terminal Moblin hit must not create an explosion or a drop.");
    }

    private static void CheckContinuousAttacksRespectCooldownWithoutPermanentImmunity()
    {
        var map = EnemyMap("e5");
        var session = new LiveWallpaperEnemySimulation.Session();
        session.Resolve(map, 0, 0, null);
        var attack = new LiveWallpaperSimulatedLinkState(
            5.5f, 5.75f, 0, 3, LiveWallpaperLinkRouteAction.Attack,
            new LiveWallpaperLinkInput(Vector2.Zero, false), combatEnemyIndex: 0,
            actionProgress: .95f, attackBox: new LiveWallpaperAttackBox(0, 0, 192, 160));
        LiveWallpaperEnemyState state = default;
        for (var elapsed = 1L; elapsed <= 1_100; elapsed += 17)
            state = session.Resolve(map, 0, elapsed, attack);
        Check(state.DeathStartedAt >= 0,
            "Continuous blocking swings must apply a new native hit after cooldown, not leave a two-life Moblin immune forever.");
    }

    private static void CheckSwordCooldownAndGeometry()
    {
        var map = EnemyMap("e5");
        var session = new LiveWallpaperEnemySimulation.Session();
        session.Resolve(map, 0, 0, null);
        var broadAttack = new LiveWallpaperSimulatedLinkState(
            5.5f, 5.75f, 0, 3, LiveWallpaperLinkRouteAction.Attack,
            new LiveWallpaperLinkInput(Vector2.Zero, false), combatEnemyIndex: 0,
            actionProgress: .95f, attackBox: new LiveWallpaperAttackBox(0, 0, 192, 160));
        LiveWallpaperEnemyState state = default;
        for (var elapsed = 1L; elapsed <= 375; elapsed += 17)
            state = session.Resolve(map, 0, elapsed, broadAttack);
        Check(state.DeathStartedAt < 0,
            "Repeated collision samples within the native 396 ms sword cooldown must not apply a second hit.");

        foreach (var attackBox in new[]
                 {
                     default(LiveWallpaperAttackBox),
                     new LiveWallpaperAttackBox(160, 140, 8, 8)
                 })
        {
            var guardedSession = new LiveWallpaperEnemySimulation.Session();
            guardedSession.Resolve(map, 0, 0, null);
            var attack = new LiveWallpaperSimulatedLinkState(
                5.5f, 5.75f, 0, 3, LiveWallpaperLinkRouteAction.Attack,
                new LiveWallpaperLinkInput(Vector2.Zero, false), combatEnemyIndex: 0,
                actionProgress: .95f, attackBox: attackBox);
            for (var elapsed = 1L; elapsed <= 1_100; elapsed += 17)
                state = guardedSession.Resolve(map, 0, elapsed, attack);
            Check(state.DeathStartedAt < 0,
                "Invalid or nonintersecting sword rectangles must not damage an enemy.");
        }
    }

    private static void CheckMoblinSwordContactKnockbackRecovers()
    {
        var map = EnemyMap("moblinSword");
        Check(LiveWallpaperMapViewport.TryCreateCentered(192, 160, map.Width, map.Height,
                88, 96, .5f, out var viewport),
            "Moblin-contact fixture viewport must load.");
        var link = new LiveWallpaperLinkSimulation();
        var enemies = new LiveWallpaperEnemySimulation.Session();
        // Map entities use the canonical +8,+16 anchor from their object
        // origin; overlap the resolved runtime anchor, not raw object data.
        link.EnterMap(88, 96);

        // This is the renderer order: Link advances, then the resolved enemy
        // state updates the live collision view and contributes a contact hit.
        var state = link.UpdateJourney(1, 0, 0, true, map, viewport, false,
            allowViewportFollow: true);
        var enemy = enemies.Resolve(map, 0, 0, state);
        link.UpdateLiveEnemyState(map, 0, enemy);
        Check(enemy.LinkHit.Valid && link.ApplyEnemyHit(enemy.LinkHit, 0),
            $"An overlapping sword Moblin must deliver one physical contact hit (Link={state.MapX * 16f},{state.MapY * 16f}; enemy={enemy.PixelX},{enemy.PixelY}; action={enemy.Action}).");

        var initialLinkX = state.MapX * 16f;
        var separatedByKnockback = false;
        var recovered = false;
        var recoveredAt = -1L;
        var recoveryRouteAccepted = false;
        var recoveryState = "";
        for (var frame = 1; frame <= 240; frame++)
        {
            var elapsed = frame * 17L;
            state = link.UpdateJourney(1, 0, elapsed, true, map, viewport, false,
                allowViewportFollow: true);
            // The renderer supplies this animation-derived box before enemy
            // resolution. Geometry itself is covered above; this lets the Core
            // contact sequence retain the same combat handoff without Android
            // atlas inputs.
            var resolvedLink = state.Action == LiveWallpaperLinkRouteAction.Attack &&
                               state.CombatEnemyIndex == 0
                ? state.WithAttackBox(new LiveWallpaperAttackBox(0, 0, 192, 160))
                : state;
            enemy = enemies.Resolve(map, 0, elapsed, resolvedLink);
            link.UpdateLiveEnemyState(map, 0, enemy);
            if (enemy.LinkHit.Valid)
                link.ApplyEnemyHit(enemy.LinkHit, elapsed);
            if (MathF.Abs(state.MapX * 16f - initialLinkX) > 4f)
                separatedByKnockback = true;
            // A path cannot begin while Link's old body is physically embedded
            // in an enemy; after native knockback separates the two bodies, a
            // normal journey must be able to resume.
            if (frame == 20)
            {
                recoveryRouteAccepted = link.TryWalkTo(map, viewport, 136, 96);
                recoveryState = $"Link={state.MapX * 16f},{state.MapY * 16f}; enemy={enemy.PixelX},{enemy.PixelY}";
            }
            if (state.MapX * 16f >= 132f)
            {
                recovered = true;
                if (recoveredAt < 0) recoveredAt = elapsed;
            }
        }

        Check(separatedByKnockback,
            "A valid contact hit must advance Link's body even while no journey plan is available.");
        Check(recoveryRouteAccepted,
            $"The recovery route must become reachable once native knockback separates the contact bodies ({recoveryState}).");
        Check(recovered && recoveredAt < 1_500,
            $"After contact knockback, Link must resume the reachable route within a bounded recovery window (first recovery={recoveredAt}; final Link={state.MapX * 16f},{state.MapY * 16f}; enemy={enemy.PixelX},{enemy.PixelY}).");
    }

    private static LiveWallpaperSimulatedLinkState SwordAttack() => new(
        5.5f, 5.75f, 0, 3, LiveWallpaperLinkRouteAction.Attack,
        new LiveWallpaperLinkInput(Vector2.Zero, false), combatEnemyIndex: 0,
        actionProgress: .5f, attackBox: new LiveWallpaperAttackBox(80, 75, 20, 20));

    private static LiveWallpaperMap EnemyMap(string enemyTemplate)
    {
        var text = new StringBuilder("3\n0\n0\ndungeon.png\n12\n10\n1\n");
        for (var row = 0; row < 10; row++)
            text.AppendLine(string.Join(',', Enumerable.Repeat("0", 12)));
        text.Append("1\n").AppendLine(enemyTemplate).Append("1\n0;80;80\n");
        Check(LiveWallpaperMap.TryLoad(new StringReader(text.ToString()), out var map) && map.Enemies.Count == 1,
            "Enemy death fixture must load one supported enemy.");
        return map;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
