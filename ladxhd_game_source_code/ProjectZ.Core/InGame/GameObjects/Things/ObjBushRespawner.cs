using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjBushRespawner : GameObject
    {
        private readonly float _posX;
        private readonly float _posY;
        private readonly string _spawnItem;
        private readonly string _spriteId;
        private readonly bool _hasCollider;
        private readonly bool _drawShadow;
        private readonly bool _setGrassField;
        private readonly int _drawLayer;
        private readonly string _pickupKey;

        private int _lastFieldTime;

        private bool _respawnStart;
        private float _respawnTimer;

        public ObjBushRespawner(Map.Map map, int posX, int posY, string spawnItem, string spriteId,
            bool hasCollider, bool drawShadow, bool setGrassField, int drawLayer, string pickupKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _posX = posX;
            _posY = posY;
            _spawnItem = spawnItem;
            _spriteId = spriteId;
            _hasCollider = hasCollider;
            _drawShadow = drawShadow;
            _setGrassField = setGrassField;
            _drawLayer = drawLayer;
            _pickupKey = pickupKey;

            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);

            // add key change listener
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            Map.Objects.RegisterAlwaysAnimateObject(this);
        }

        private void Update()
        {
            // If classic camera mode is active, respawn when leaving the current field.
            if (Camera.ClassicMode)
            {
                // If the field has changed, then start the respawn.
                if (MapManager.ObjLink.FieldChange)
                    _respawnStart = true;

                // Respawn after a slight delay. Always animate list makes sure that all
                // of the bushes and grass respawn even when they are off the screen.
                if (_respawnStart)
                {
                    _respawnTimer += Game1.DeltaTime;
                    if (_respawnTimer >= 200)
                    {
                        SpawnObject();
                        _respawnTimer = 0;
                        _respawnStart = false;
                    }
                }
            }
            // If the field is outside of the update range. Link must be at least 3
            // fields away from the field the object is found on for it to respawn.
            else
            {
                var updateState = Map.GetUpdateState(EntityPosition.Position);
                if (_lastFieldTime < updateState)
                    SpawnObject();
            }
        }

        private void SpawnObject()
        {
            // Delete the respawn object and respawn the bush or grass object.
            Map.Objects.DeleteObjects.Add(this);
            Map.Objects.SpawnObject(new ObjBush(Map, (int)EntityPosition.X, (int)EntityPosition.Y,
                _spawnItem, _spriteId, _hasCollider, _drawShadow, _setGrassField, _drawLayer, _pickupKey));
        }
    }
}