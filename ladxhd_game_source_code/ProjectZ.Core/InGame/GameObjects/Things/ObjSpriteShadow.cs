using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjSpriteShadow : GameObject
    {
        private DrawComponent _drawComponent;

        public GameObject _host;
        public CSprite _sprite;
        public CPosition _position;
        private int _layer;
        private Map.Map _currentMap;

        public bool ForceDraw;

        private Vector2 _offset = new Vector2(-8, -14);

        public ObjSpriteShadow() : this("sprshadows") { }

        public ObjSpriteShadow(string spriteName) : base(spriteName) { }

        public ObjSpriteShadow(Map.Map map, float posX, float posY, int layer, string spriteName) : base(map)
        {
            _currentMap = map;
            map.Objects.SpawnObject(this);

            _layer = layer;
            _position = new CPosition(posX, posY, 0);
            _sprite = new CSprite(spriteName, _position);
            EntityPosition = _position;

            AddComponent(DrawComponent.Index, _drawComponent = new DrawCSpriteComponent(_sprite, _layer));
            UpdateVisibility(!GameSettings.EnableShadows);
        }

        public ObjSpriteShadow(Map.Map map, GameObject host, float offsetX, float offsetY, int layer, string spriteName) : this(map, host, layer, spriteName)
        {
            _currentMap = map;
            _offset = new Vector2(offsetX, offsetY);
            _drawComponent.IsActive = !GameSettings.EnableShadows;
        }

        public ObjSpriteShadow(Map.Map map, GameObject host, int layer, string spriteName) : base(map)
        {
            _currentMap = map;

            // A failsafe to prevent crashes. This shouldn't happen but it could.
            if (_host == null && map == null) return;

            map.Objects.SpawnObject(this);

            _host = host;
            _layer = layer;
            _position = new CPosition(_host.EntityPosition.Position.X + _offset.X, _host.EntityPosition.Position.Y + _offset.Y, 0);
            _sprite = new CSprite(spriteName, _position);

            EntityPosition = _position;

            AddComponent(DrawComponent.Index, _drawComponent = new DrawCSpriteComponent(_sprite, _layer));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            // If the host map is null then destroy the sprite shadow.
            if (_host.Map == null || _host == null)
                Destroy();

            // Disable sprite shadows when "Dynamic Shadows" is enabled.
            _drawComponent.IsActive = !GameSettings.EnableShadows;

            // Update the position of the shadow if using sprite shadows.
            if (!GameSettings.EnableShadows)
                UpdatePosition();

            // The sprite shadow should only be visible if above ground and if the host state is visible.
            bool posZcheck = _host.EntityPosition.Z > 0;
            bool hostCheck = !(_host is IHasVisibility hostVisibility) || hostVisibility.IsVisible;
            _sprite.IsVisible = ForceDraw || (posZcheck && hostCheck && _host.IsActive);
        }

        private void UpdatePosition()
        {
            var newPostion = _host.EntityPosition.Position + _offset;
            _position = new CPosition(newPostion.X, newPostion.Y, 0);
            EntityPosition.Set(_position.Position);
        }

        public void Destroy()
        {
            _drawComponent.IsActive = false;
            _currentMap.Objects.RemoveObject(this);
        }

        public void UpdateVisibility(bool visible)
        {
            _sprite.IsVisible = visible;
            _drawComponent.IsActive = visible;
        }
    }
}