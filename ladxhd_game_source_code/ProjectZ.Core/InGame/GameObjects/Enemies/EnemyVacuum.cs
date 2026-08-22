using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyVacuum : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;

        private Box _absorbBox;
        private Box _rangeBox;
        private Rectangle _fieldRectangle;
        private Rectangle _limitRectangle;

        private readonly string _roomName;
        private readonly string _entryId;
        private readonly bool _isPusher;
        private bool _isRotating;
        private int _lives = EnemyLives.Vacuum;
        private int _dropIndex = 2;

        public EnemyVacuum() : base("vacuum") { }

        public EnemyVacuum(Map.Map map, int posX, int posY, string roomName, string entryId, bool isPusher) : base(map)
        {
            Tags = Values.GameObjectTag.Trap;

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            ResetPosition  = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);
            CanReset = false;
            OnReset = Reset;

            _roomName = roomName;
            _entryId = entryId;

            _isPusher = isPusher;

            _rangeBox = map.GetFieldBox(posX, posY, 64, isPusher ? 0 : 8);

            // Field rectangle is the size of the room. Limit rectangle is slightly smaller so the "UpdateLeaveRoom" method is called in the doorway.
            _fieldRectangle = map.GetField(posX, posY);
            _limitRectangle = new Rectangle(_fieldRectangle.X + 15, _fieldRectangle.Y + 15, _fieldRectangle.Width - 30, _fieldRectangle.Height - 30);

            _absorbBox = new Box(posX, posY, 0, 16, 16, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/vacuum");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -8));

            _body = new BodyComponent(EntityPosition, -7, -7, 14, 14, 8)
            {
                IgnoreHoles = true
            };

            var stateIdle = new AiState { Init = InitIdle };
            if (_isPusher)
                stateIdle.Trigger.Add(new AiTriggerCountdown(1000, null, ToVacuum));
            else
                stateIdle.Trigger.Add(new AiTriggerRandomTime(ToVacuum, 1000, 1500));
            var stateVacuum = new AiState(UpdateVacuum) { Init = InitVacuum };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("vacuum", stateVacuum);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, _lives, _dropIndex)
            {
                MoveBody = false,
                ExplosionOffsetY = 4
            };

            // random start position/state
            _aiComponent.ChangeState("idle");

            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(UpdateLeaveRoom));

            if (!_isPusher)
            {
                // POLISH: center the player when he gets absorbed
                AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(posX + 3, posY + 2, 0, 10, 12, 16), Values.CollisionTypes.Hole));
                AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(new Rectangle(posX + 6, posY + 6, 4, 4), OnCollision));
            }
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerBottom));
            Map.Objects.RegisterAlwaysAnimateObject(this);
        }

        public override void Reset()
        {
            _aiComponent.ChangeState("idle");
            _aiComponent.ChangeState("idle");
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType hitType, int damage, bool pieceOfPower)
        {
            if (hitType == HitType.Hookshot ||
                hitType == HitType.MagicRod ||
                hitType == HitType.MagicPowder ||
                hitType == HitType.Bow ||
                hitType == HitType.SwordShot ||
                hitType == HitType.Boomerang)
                return Values.HitCollision.None;

            EndRotation();

            // dont draw trail particle
            return _damageState.OnHit(gameObject, direction, hitType, damage, false);
        }

        private void UpdateLeaveRoom()
        {
            if (_isRotating && !_limitRectangle.Contains(MapManager.ObjLink.CenterPosition.Position))
                EndRotation();
        }

        private void OnCollision(GameObject gameObject)
        {
            MapManager.ObjLink.HoleResetRoom = _roomName;
            MapManager.ObjLink.HoleResetEntryId = _entryId;
            MapManager.ObjLink.MapTransitionStart = null;
            MapManager.ObjLink.MapTransitionEnd = null;
            EndRotation();
        }

        private void ToVacuum()
        {
            // is the player in the current room?
            if (_fieldRectangle.Contains(MapManager.ObjLink.CenterPosition.Position))
                _aiComponent.ChangeState("vacuum");
            else
                _aiComponent.ChangeState("idle");
        }

        private void InitIdle()
        {
            if (!_isPusher)
                EndRotation();
            _animator.Play("idle");
        }

        private void InitVacuum()
        {
            _animator.Play("vacuum");
        }

        private void UpdateVacuum()
        {
            if (!_animator.IsPlaying)
            {
                _aiComponent.ChangeState("idle");
                return;
            }

            Game1.AudioManager.PlaySoundEffect("D378-31-1F", false);

            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects, (int)_rangeBox.X, (int)_rangeBox.Y,
                (int)_rangeBox.Width, (int)_rangeBox.Height, BodyComponent.Mask);

            foreach (var collidingObject in _collidingObjects)
            {
                if (collidingObject == this)
                    continue;

                var body = (BodyComponent)collidingObject.Components[BodyComponent.Index];

                // absorb enemies that fully collide with the box
                if (collidingObject.Tags == Values.GameObjectTag.Enemy && _absorbBox.Contains(body.BodyBox.Box))
                {
                    Map.Objects.DeleteObjects.Add(collidingObject);
                    continue;
                }

                if (body.IsAbsorbed || !_rangeBox.Contains(body.BodyBox.Box))
                    continue;

                var direction = EntityPosition.Position - body.BodyBox.Box.Center;

                if (direction != Vector2.Zero)
                    direction.Normalize();

                // push objects?
                if (_isPusher)
                {
                    var distance = (EntityPosition.Position - body.Position.Position).Length();
                    var multiplier = Math.Clamp((112 - distance) / 16, 0, 1.5f);

                    body.AdditionalMovementVT.X = -multiplier * direction.X;
                    body.AdditionalMovementVT.Y = -multiplier * direction.Y;
                }
                else
                {
                    // rotate player
                    if (collidingObject is ObjLink)
                    {
                        _isRotating = true;
                        MapManager.ObjLink.RotatePlayer();
                    }
                    body.AdditionalMovementVT.X = 0.5f * direction.X;
                    body.AdditionalMovementVT.Y = 0.5f * direction.Y;
                }

                body.DisableVelocityTargetMultiplier = true;
            }
        }

        private void EndRotation()
        {
            _isRotating = false;
            MapManager.ObjLink.StopRotating();
        }
    }
}