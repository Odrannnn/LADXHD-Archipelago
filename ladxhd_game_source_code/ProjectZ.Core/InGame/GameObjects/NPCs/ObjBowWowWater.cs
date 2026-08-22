using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.GameObjects.Effects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjBowWowWater : GameObject
    {
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        public CPosition _position;
        public ObjBowWow _host;

        private Vector2 _offset = new Vector2(-3, 11);

        private bool _drawCircle;
        private bool _effectPlayed;

        public ObjBowWowWater() : this("bowwow_water") { }

        public ObjBowWowWater(string spriteName) : base(spriteName) { }

        public ObjBowWowWater(Map.Map map, float posX, float posY, ObjBowWow host) : base(map)
        {
            EntityPosition = new CPosition(posX + _offset.X, posY + _offset.Y, 0);

            _host = host;
            _sprite = new CSprite(EntityPosition);
            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/bowwow_water");

            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -15));

            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));

            Map.Objects.SpawnObject(this);
            Map.Objects.RegisterAlwaysAnimateObject(this);

            _animator.Play("stand");
        }

        private void Update()
        {
            // If the map is null destroy the object.
            if (Map == null || _host.Map == null)
            {
                Map.Objects.RemoveObject(this);
                return;
            }
            // Check if Bow Wow is currently in the water.
            _drawCircle = SystemBody.GetFieldState(_host._body).HasFlag(MapStates.FieldStates.DeepWater);

            // If not then do nothing.
            if (!_drawCircle)
                return;

            // Update the position of the water effect.
            _position = new CPosition(_host.EntityPosition.Position.X + _offset.X, _host.EntityPosition.Position.Y + _offset.Y, 0);
            EntityPosition.Set(_position.Position);

            // Get the water state and see if he's jumping or not.
            if (_host.EntityPosition.Z <= 2.5f)
                EnableAndSplash();
            else
               _effectPlayed = false; 
        }

        private void EnableAndSplash()
        {
            if (!_effectPlayed)
            {
                var splashAnimator = new ObjAnimator(_host._body.Owner.Map, 0, 0, 0, 3, Values.LayerPlayer, "Particles/splash", "idle", true);
                splashAnimator.EntityPosition.Set(new Vector2(
                    _host._body.Position.X + _host._body.OffsetX + _host._body.Width / 2f,
                    _host._body.Position.Y + _host._body.OffsetY + _host._body.Height - _host._body.Position.Z - 3));
                Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(splashAnimator);
                _effectPlayed = true;
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_drawCircle)
                _sprite.Draw(spriteBatch);
        }
    }
}
