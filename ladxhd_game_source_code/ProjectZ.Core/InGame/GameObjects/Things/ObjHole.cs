using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjHole : GameObject
    {
        private readonly DrawSpriteComponent _drawComponent;
        private readonly BoxCollisionComponent _collisionComponent;
        private readonly UpdateComponent _updateComponent;

        public readonly Vector2 Center;
        public readonly int Color;
        public CBox collisionBox;

        private bool _despawn;
        private float _despawnTimer;

        public ObjHole() : base("hole_0") { }

        public ObjHole(Map.Map map, int posX, int posY, int width, int height, Rectangle sourceRectangle, int offsetX, int offsetY, int color) : base(map)
        {
            Tags = Values.GameObjectTag.Hole;

            Center = new Vector2(posX + offsetX + width / 2, posY + offsetY + height / 2);
            Color = color;

            if (sourceRectangle == Rectangle.Empty)
            {
                EntityPosition = new CPosition(posX + offsetX, posY + offsetY, 0);
                EntitySize = new Rectangle(0, 0, width, height);
            }
            else
            {
                EntityPosition = new CPosition(posX, posY, 0);
                EntitySize = new Rectangle(0, 0, sourceRectangle.Width, sourceRectangle.Height);
            }
            float holePosX = posX + offsetX;
            float holePosY = posY + offsetY;
            float holeWidth = width;
            float holeHeight = height;

            collisionBox = new CBox(holePosX, holePosY, 0, holeWidth, holeHeight, 16);
            AddComponent(CollisionComponent.Index, _collisionComponent = new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Hole));
            AddComponent(UpdateComponent.Index, _updateComponent = new UpdateComponent(Update) { IsActive = false }) ;

            // visible hole?
            if (sourceRectangle != Rectangle.Empty)
            {
                _drawComponent = new DrawSpriteComponent(Resources.SprObjects, EntityPosition, sourceRectangle, new Vector2(0, 0), Values.LayerBottom);
                AddComponent(DrawComponent.Index, _drawComponent);
            }
        }

        public void BushSpawn()
        {
            // Track if spawned from a bush so it can be reset.
            Map.Objects.RegisterAlwaysAnimateObject(this);
            _updateComponent.IsActive = true;
        }

        private void Update()
        {
            // Detect a field change from Link.
            if (MapManager.ObjLink.FieldChange)
            {
                // Delay the despawn so disappearing isn't visible.
                _despawn = true;
                _despawnTimer = 250;
            }
            // Start the despawn timer.
            if (_despawn)
            {
                _despawnTimer -= Game1.DeltaTime;
                if (_despawnTimer <= 0)
                    Map.Objects.DeleteObjects.Add(this);
            }
        }

        public void SetActive(bool state)
        {
            if (_drawComponent != null)
                _drawComponent.IsActive = state;
            _collisionComponent.IsActive = state;
        }
    }
}