using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjStoneRespawner : GameObject
    {
        private readonly float _posX;
        private readonly float _posY;
        private readonly string _spriteId;
        private readonly string _spawnItem;
        private readonly string _pickupKey;
        private readonly string _dialogPath;
        private readonly bool _isHeavy;
        private readonly bool _potMessage;

        private int _lastFieldTime;
        private bool _respawnStart;
        private float _respawnTimer;
        private bool _respawnedStone;

        public ObjStoneRespawner(Map.Map map, int posX, int posY, string spriteId, string spawnItem, string pickupKey, string dialogPath, bool isHeavy, bool potMessage, bool fromSpawner) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _posX = posX;
            _posY = posY;
            _spriteId = spriteId;
            _pickupKey = pickupKey;
            _dialogPath = dialogPath;
            _isHeavy = isHeavy;
            _potMessage = potMessage;
            _respawnedStone = fromSpawner;

            // Only the first pot should contain the fairy.
            if (spawnItem != "fairy")
                _spawnItem = spawnItem;
            else
                _spawnItem = "";

            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            Map.Objects.RegisterAlwaysAnimateObject(this);
        }

        private void Update()
        {
            // If the object has already been spawned from a spawner, delete this wrapper immediately.
            if (_respawnedStone)
                DeleteThis();

            // If classic camera mode is active, respawn when leaving the current field.
            if (Camera.ClassicMode)
            {
                // If the field has changed, then start the respawn.
                if (MapManager.ObjLink.FieldChange)
                    _respawnStart = true;

                // Respawn after a slight delay. Always animate list makes sure that all
                // of the stones respawn even when they are off the screen.
                if (_respawnStart)
                {
                    _respawnTimer += Game1.DeltaTime;
                    if (_respawnTimer >= 200)
                    {
                        SpawnStone();
                        _respawnStart = false;
                        _respawnTimer = 0;
                    }
                }
            }
            else
            {
                // Overworld / smooth camera mode: respawn when field update time changes
                var updateState = Map.GetUpdateState(EntityPosition.Position);

                if (_lastFieldTime < updateState)
                    SpawnStone();
            }
        }

        private void DeleteThis()
        {
            Map.Objects.DeleteObjects.Add(this);
        }

        private void SpawnStone()
        {
            // Remove the respawner object.
            Map.Objects.DeleteObjects.Add(this);

            // Respawn original stone type
            Map.Objects.SpawnObject(new ObjStone(Map, (int)EntityPosition.X, (int)EntityPosition.Y, _spriteId, _spawnItem, _pickupKey, _dialogPath, _isHeavy, _potMessage));
        }
    }
}
