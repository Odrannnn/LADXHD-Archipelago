using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossBlainoGlove : GameObject
    {
        private readonly MBossBlaino _blaino;
        private readonly DamageFieldComponent _damageField;
        private readonly PushableComponent _pushComponent;
        private readonly HittableComponent _hitComponent;
        private readonly string _resetDoor;

        private int _hitDirection;
        private bool _knockoutMode;
        private bool _stunMode;

        public MBossBlainoGlove(Map.Map map, MBossBlaino blaino, Vector2 position, string resetDoor) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(0, 0, 11, 11);

            var damageBox = new CBox(EntityPosition, 0, 0, 0, 11, 11, 8);
            var hittableBox = new CBox(EntityPosition, 0, 0, 0, 11, 11, 8);
            var pushableBox = new CBox(EntityPosition, 0, 0, 0, 11, 11, 8);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 4) { OnDamage = DamagePlayer, PushMultiplier = 2.25f });
            AddComponent(HittableComponent.Index, _hitComponent = new HittableComponent(hittableBox, OnHit));
            AddComponent(PushableComponent.Index, _pushComponent = new PushableComponent(pushableBox, OnPush));

            _blaino = blaino;
            _resetDoor = resetDoor;
        }

        public void SetHitDirection(int direction)
        {
            _hitDirection = direction;
        }

        public void SetKnockoutMode(bool knockoutMode)
        {
            _knockoutMode = knockoutMode;
        }

        public void SetStunMode(bool stunMode)
        {
            _stunMode = stunMode;
        }

        private bool DamagePlayer()
        {
            // is the player blocking?
            if (_stunMode && (MapManager.ObjLink.IsBlockingState()) &&
                ((_hitDirection == -1 && MapManager.ObjLink.Direction != 0) ||
                (_hitDirection == 1 && MapManager.ObjLink.Direction != 2)))
            {
                _blaino.GlovePush(new Vector2(-_hitDirection * 3.5f, 0));
                MapManager.ObjLink.Body.Velocity += new Vector3(_hitDirection * 3.5f, 0, 0);
                return false;
            }

            var damagedPlayer = _damageField.DamagePlayer();

            if (_knockoutMode)
            {
                _knockoutMode = false;
                Game1.AudioManager.PlaySoundEffect("D360-11-0B");

                MapManager.ObjLink.Knockout(new Vector2(_hitDirection * 0.75f, -1), _resetDoor);
                return true;
            }

            if (_stunMode)
                MapManager.ObjLink.Stun(3500, true);

            return damagedPlayer;
        }

        public void SetPosition(Vector2 newPosition)
        {
            EntityPosition.Set(newPosition);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType hitType, int damage, bool pieceOfPower)
        {
            if (_blaino.DamageState.IsInDamageState() || _blaino.AiComponent.CurrentStateId == "damage" || _blaino.AiComponent.CurrentStateId == "dying")
                return Values.HitCollision.None;

            damage = 0;

            _blaino.NoHitKnockback(direction);
            _blaino.DamageState.OnHit(gameObject, direction, hitType, damage, pieceOfPower);
            return Values.HitCollision.None;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_stunMode)
                return false;

            _blaino.OnPush(direction, type);
            return true;
        }

        public void OnDeath()
        {
            _damageField.IsActive = false;
            _hitComponent.IsActive = false;
            _pushComponent.IsActive = false;
        }
    }
}