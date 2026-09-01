using System;
using ProjectZ.InGame.Things;

namespace ProjectZ;

public static class EnemyDeathGameplay
{
    public const string ExplosionAnimation = "Particles/explosion0";
    public const int ExplosionOffset = -12;
    public const int OctorokDrop = 2, SeaUrchinDrop = 2, LeeverDrop = 4, CrabDrop = 4,
        MoblinDrop = 4, RedZolDrop = 2, RiverZoraDrop = 2, GhiniDrop = 9, PincerDrop = 1;

    public static int DropTable(LiveWallpaperMapEnemyKind kind) => kind switch
    {
        LiveWallpaperMapEnemyKind.Octorok => OctorokDrop,
        LiveWallpaperMapEnemyKind.SeaUrchin => SeaUrchinDrop,
        LiveWallpaperMapEnemyKind.Leever => LeeverDrop,
        LiveWallpaperMapEnemyKind.Crab => CrabDrop,
        LiveWallpaperMapEnemyKind.Moblin or LiveWallpaperMapEnemyKind.MoblinSword => MoblinDrop,
        LiveWallpaperMapEnemyKind.RedZol => RedZolDrop,
        LiveWallpaperMapEnemyKind.RiverZora => RiverZoraDrop,
        LiveWallpaperMapEnemyKind.Ghini => GhiniDrop,
        LiveWallpaperMapEnemyKind.Pincer => PincerDrop,
        _ => 0
    };

    // The wallpaper has no saved health/powerup counters. Use ordinary, normal-health
    // rolls; a fairy is still a fairy result, never silently replaced with another item.
    public static string RollDrop(LiveWallpaperMapEnemyKind kind, Func<int, int, int> next) =>
        ItemDropTable.RollOrdinaryDrop(DropTable(kind), false, false, next);
}
