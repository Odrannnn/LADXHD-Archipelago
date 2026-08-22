using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class EnemyFlyingTileRespawner : GameObject
    {
        private int _lastFieldTime;
        private bool _respawnStart;
        private float _respawnTimer;

        Rectangle _fieldRect;
        private readonly string _strKey;
        private readonly int _index;
        private readonly int _mode;

        public EnemyFlyingTileRespawner(Map.Map map, int posX, int posY, string strKey, int index, int mode) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _strKey = strKey;
            _index  = index;
            _mode   = mode;

            _fieldRect = Map.GetField(posX, posY);
            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            Map.Objects.RegisterAlwaysAnimateObject(this);
        }

        private void Update()
        {
            if (Camera.ClassicMode)
            {
                // Classic Camera: respawn on field change
                if (MapManager.ObjLink.FieldChange)
                    _respawnStart = true;

                if (_respawnStart)
                {
                    _respawnTimer += Game1.DeltaTime;
                    if (_respawnTimer >= 250)
                    {
                        SpawnTile();
                        _respawnStart = false;
                        _respawnTimer = 0;
                    }
                }
            }
            else
            {
                // Modern Camera: respawn when re-entering field
                var updateState = Map.GetUpdateState(EntityPosition.Position);

                if (_lastFieldTime < updateState)
                    SpawnTile();
            }
        }

        private void SpawnTile()
        {
            List<GameObject> enemyTriggers = new List<GameObject>();
            EnemyFlyingTile flyingTile = new EnemyFlyingTile(Map, (int)EntityPosition.X, (int)EntityPosition.Y, _strKey, _index, _mode);

            // Remove respawner
            Map.Objects.DeleteObjects.Add(this);

            // Spawn new flying tile
            Map.Objects.SpawnObject(flyingTile);

            // If there is utility objects in the room find them.
            Map.Objects.GetGameObjectsWithTag(enemyTriggers, Values.GameObjectTag.Utility,
                (int)_fieldRect.X, (int)_fieldRect.Y, (int)_fieldRect.Width, (int)_fieldRect.Height);

            // Loop through the list of utility objects.
            foreach (var trigger in enemyTriggers) 
            {
                // If it's an enemy trigger add the Red Zol.
                if (trigger is ObjEnemyTrigger etrig)
                {
                    etrig.EnemyTriggerList.Add(flyingTile);
                }
            }
        }
    }
}
