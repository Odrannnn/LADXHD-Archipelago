using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class CrystalRespawner : GameObject
    {
        private readonly float _posX;
        private readonly float _posY;
        private readonly string _spriteId;
        private readonly string _dialogPath;
        private readonly bool _isHardCrystal;
        private readonly int _color;

        private int _lastFieldTime;
        private bool _respawnStart;
        private float _respawnTimer;

        public CrystalRespawner(Map.Map map, int posX, int posY, string spriteId, string dialogPath, bool isHardCrystal, int color) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _posX = posX;
            _posY = posY;
            _spriteId = spriteId;
            _dialogPath = dialogPath;
            _isHardCrystal = isHardCrystal;
            _color = color;

            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);

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
                // of the crystals respawn even when they are off the screen.
                if (_respawnStart)
                {
                    _respawnTimer += Game1.DeltaTime;
                    if (_respawnTimer >= 200)
                    {
                        SpawnCrystal();
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
                    SpawnCrystal();
            }
        }

        private void SpawnCrystal()
        {
            // Delete the respawn object and respawn the crystal object.
            Map.Objects.DeleteObjects.Add(this);
            Map.Objects.SpawnObject(new ObjCrystal(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 
                _spriteId, _color, _isHardCrystal, _dialogPath));
        }
    }
}
