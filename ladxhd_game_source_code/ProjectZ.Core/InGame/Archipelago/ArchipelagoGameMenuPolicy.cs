using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.Archipelago
{
    public readonly struct ArchipelagoWarpTarget
    {
        public ArchipelagoWarpTarget(string mapName, Vector2 position, int direction)
        {
            MapName = mapName;
            Position = position;
            Direction = direction;
        }

        public string MapName { get; }
        public Vector2 Position { get; }
        public int Direction { get; }
    }

    public static class ArchipelagoGameMenuPolicy
    {
        public static ArchipelagoWarpTarget WarpToStartTarget { get; } =
            new ArchipelagoWarpTarget("house1.map", new Vector2(70, 70), 3);

        public static bool KeepPauseOpenForEmbeddedTracker => true;
    }
}
