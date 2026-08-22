using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Identifiers
{
    internal class ObjOverworld : GameObject
    {
        private readonly Vector2 _offset;

        public ObjOverworld() : base("editor overworld") { }

        public ObjOverworld(Map.Map map, int posX, int posY) : base(map)
        {
            map.IsOverworld = true;
            _offset = new Vector2(posX, posY);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));

            // When loading the overworld, if the signpost game was not complete then reset the state of the signs.
            if (Game1.GameManager.SaveManager.GetString("shieldGameStairs", "0") != "1")
                Game1.GameManager.SaveManager.SetString("sgame", "0");
        }

        public void Update()
        {
            // update the players position on the map
            Game1.GameManager.SetMapPosition(new Point((int)(MapManager.ObjLink.CenterPosition.X - _offset.X) / Values.FieldWidth, (int)(MapManager.ObjLink.CenterPosition.Y - _offset.Y) / Values.FieldHeight));
        }
    }
}
