using Microsoft.Xna.Framework;
using ProjectZ.InGame.Archipelago;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Identifiers
{
    internal class ObjDungeon : GameObject
    {
        public ObjDungeon() : base("editor dungeon") { }

        public ObjDungeon(Map.Map map, int posX, int posY, string dungeonName, bool updatePosition, int dungeonLevel) : base(map)
        {
            if (!string.IsNullOrEmpty(dungeonName))
                Game1.GameManager.SetDungeon(dungeonName, dungeonLevel);

            // LADXHD does not place Eagle's Tower's original entrance key as a collectible.
            // Treat entering the dungeon as the equivalent AP check so the inherited LADX
            // location pool remains complete.
            if (dungeonName == "dungeon_7")
            {
                var sourceLocationKey = ArchipelagoLocationKey.Event("d7_entrance_key");
                Game1.GameManager.ArchipelagoManager.ResolveLocationItemName(
                    sourceLocationKey, "smallkey", map?.MapName, posX, posY);
                Game1.GameManager.ArchipelagoManager.TryHandleLocationCheck(
                    new GameItemCollected("smallkey")
                    {
                        Count = 1,
                        SourceLocationKey = sourceLocationKey
                    });
            }

            // this is used in side rooms of a dungeon
            // normally this are the 2d rooms
            if (updatePosition)
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            var playerPosition = new Point(
                (int)(MapManager.ObjLink.PosX - Map.MapOffsetX * 16) / 160,
                (int)(MapManager.ObjLink.PosY - Map.MapOffsetY * 16) / 128);

            // update the player position on the dungeon map
            Game1.GameManager.DungeonUpdatePlayerPosition(playerPosition);
        }
    }
}
