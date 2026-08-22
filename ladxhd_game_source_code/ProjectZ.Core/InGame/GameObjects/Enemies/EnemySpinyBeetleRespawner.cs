using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Effects;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class EnemySpinyBeetleRespawner : GameObject
    {
        // 0 = Grass, 1 = Stone, 2 = Skull
        private readonly int _type;
        private readonly int _posX;
        private readonly int _posY;

        private RectangleF _field;
        private bool _respawnStart;
        private float _respawnTimer;

        public EnemySpinyBeetleRespawner(Map.Map map, int posX, int posY, int type, RectangleF field) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _posX = posX;
            _posY = posY;
            _type = type;
            _field = field;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            Map.Objects.RegisterAlwaysAnimateObject(this);
        }

        private void Update()
        {
            // Classic Camera: respawn after field transition
            if (Camera.ClassicMode)
            {
                if (MapManager.ObjLink.FieldChange)
                    _respawnStart = true;

                if (_respawnStart)
                {
                    _respawnTimer += Game1.DeltaTime;
                    if (_respawnTimer >= 250)
                    {
                        SpawnBeetle();
                        _respawnStart = false;
                        _respawnTimer = 0;
                    }
                }
            }
            // Modern Camera: Respawn when player leaves the field.
            else
            {
                if (!_field.Contains(MapManager.ObjLink.CenterPosition.Position))
                    SpawnBeetle();
            }
        }

        private void SpawnBeetle()
        {
            // Remove the respawner itself
            Map.Objects.DeleteObjects.Add(this);

            // If not in classic camera, spawn a smoke effect.
            if (!Camera.ClassicMode) 
            {
                var explosionAnimation = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerTop, "Particles/spawn", "run", true);
                Map.Objects.SpawnObject(explosionAnimation);
                Game1.AudioManager.PlaySoundEffect("D360-47-2F");
            }
            // Spawn the original enemy
            Map.Objects.SpawnObject(new EnemySpinyBeetle(Map, _posX, _posY, _type));
        }
    }
}
