using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjIceBlockRespawner : GameObject
    {
        private readonly float _posX;
        private readonly float _posY;
        private int _lastFieldTime;
        private bool _respawnStart;
        private float _respawnTimer;
        private readonly bool _respawnedFromSpawner;

        public ObjIceBlockRespawner(Map.Map map, int posX, int posY, bool fromSpawner) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _posX = posX;
            _posY = posY;
            _respawnedFromSpawner = fromSpawner;
            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            Map.Objects.RegisterAlwaysAnimateObject(this);
        }

        private void Update()
        {
            // If this respawner already spawned the block this frame, delete it
            if (_respawnedFromSpawner)
                DeleteThis();

            // If classic camera mode is active, respawn when leaving the current field.
            if (Camera.ClassicMode)
            {
                // If the field has changed, then start the respawn.
                if (MapManager.ObjLink.FieldChange)
                    _respawnStart = true;

                // Respawn after a slight delay. Always animate list makes sure that all
                // of the ice blocks respawn even when they are off the screen.
                if (_respawnStart)
                {
                    _respawnTimer += Game1.DeltaTime;
                    if (_respawnTimer >= 200)
                    {
                        SpawnIceBlock();
                        _respawnStart = false;
                        _respawnTimer = 0;
                    }
                }
            }
            // If the field is outside of the update range. Link must be at least 3
            // fields away from the field the object is found on for it to respawn.
            else
            {
                var updateState = Map.GetUpdateState(EntityPosition.Position);
                if (_lastFieldTime < updateState)
                    SpawnIceBlock();
            }
        }

        private void DeleteThis()
        {
            Map.Objects.DeleteObjects.Add(this);
        }

        private void SpawnIceBlock()
        {
            // Remove respawner
            Map.Objects.DeleteObjects.Add(this);

            // Respawn Ice Block
            Map.Objects.SpawnObject(new ObjIceBlock(Map,
                (int)EntityPosition.X,
                (int)EntityPosition.Y));
        }
    }
}
