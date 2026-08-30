using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.GameObjects.Base
{
    public class GameObjectFollower : GameObject
    {
        public GameObjectFollower(string spriteId) : base(spriteId) { }

        public GameObjectFollower(Map.Map map) : base(map) { }

        public virtual void SetPosition(Vector2 position) { }

        public virtual void SetFacingDirection(int direction) { }

        internal static void PlaceAtMapArrival(System.Collections.Generic.IReadOnlyList<GameObjectFollower> followers,
            GameObject spriteShadow, Vector2 position)
        {
            for (var index = 0; index < followers.Count; index++)
                followers[index].EntityPosition.Set(position);
            if (spriteShadow != null)
                spriteShadow.EntityPosition.Set(position);
        }
    }
}
