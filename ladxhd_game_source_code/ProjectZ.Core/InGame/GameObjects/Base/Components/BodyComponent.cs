using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class BodyComponent : Component
    {
        public delegate void MoveCollisionFunction(Values.BodyCollision collision);
        public delegate void HoleAbsorbFunction();
        public delegate void HoleOnPullFunction(Vector2 direction, float percentage);
        public delegate void DeepWaterFunction();

        public MoveCollisionFunction MoveCollision;
        public HoleAbsorbFunction HoleAbsorb;
        public HoleOnPullFunction HoleOnPull;
        public DeepWaterFunction OnDeepWaterFunction;

        public CBox BodyBox;
        public CPosition Position;

        public Vector3 Velocity;
        public Vector2 VelocityTarget;
        public Vector2 LastVelocityTarget;
        public Vector2 SlideOffset;
        public Vector2 HoleAbsorption;

        // used for the rolling bands
        // could probably be done in a better way (interface?)
        public Vector2 AdditionalMovementVT;
        public Vector2 LastAdditionalMovementVT;

        public RectangleF FieldRectangle = RectangleF.Empty;

        public MapStates.FieldStates CurrentFieldState = MapStates.FieldStates.Init;
        public Values.CollisionTypes CollisionTypes = Values.CollisionTypes.Normal;
        public Values.CollisionTypes CollisionTypesIgnore;
        public Values.CollisionTypes AvoidTypes;

        public Values.BodyCollision VelocityCollision;
        public Values.BodyCollision LastVelocityCollision;

        // Pushes a body to the side when colliding with a corner within a certain
        // pixel range. Used exclusively on Link but could be added to other "bodies".
        public bool CornerCorrection = false;
        public float CornerCorrectionThreshold = 2f;

        // Allows walking up "pixel stairs" that are 1-pixel high. This used to be used in 2D underground
        // sections, but goes largely unused due to useage of 1-way colliders and their "push up" mechanic.
        public bool EnableStepUp;
        public int MaxStepHeight = 1;

        public float JumpStartHeight;
        public float MaxJumpHeight = 4;
        public float Drag = 0.8f;
        public float DragAir = 0.9f;
        public float DragWater = 0.95f;
        public float Gravity = -0.25f;
        public float Gravity2D = 0.1f;
        public float Gravity2DWater = 0.025f;
        public float Bounciness = 0;
        public float Bounciness2D = 0;
        public float SpeedMultiply = 1;
        public float AbsorbPercentage = 0.85f;
        public float AbsorbStop = 0.15f;
        public float MaxSlideDistance = 6.0f;
        public float InsideCollisionEscape = 0.5f;

        public float Width
        {
            get => BodyBox.Box.Width;
            set => BodyBox.Box.Width = value;
        }
        public float Height
        {
            get => BodyBox.Box.Height;
            set => BodyBox.Box.Height = value;
        }
        public float Depth
        {
            get => BodyBox.Box.Depth;
            set => BodyBox.Box.Depth = value;
        }
        public float OffsetX
        {
            get => BodyBox.OffsetX;
            set => BodyBox.OffsetX = value;
        }
        public float OffsetY
        {
            get => BodyBox.OffsetY;
            set => BodyBox.OffsetY = value;
        }

        public const int DefaultDeepWaterOffset = -3;
        public int DeepWaterOffset = DefaultDeepWaterOffset;

        public int Level = 0;
        
        public bool SimpleMovement = false;             // Used to make the X/Y movement happen in one step.
        public bool IgnoreCollision = false;            // Outright ignore all collision.
        public bool IgnoreInsideCollision = true;       // If already inside a collider, ignore remaining collision.

        public bool IsActive = true;
        public bool IsGrounded = true;
        public bool WasGrounded = true;
        public bool IgnoresZ;
        // Keep horizontal collision on a fixed elevation while the sprite/body is
        // visually raised. Used by rooster flight so solid walls remain solid.
        public bool LockCollisionHeight;
        public bool IgnoreHoles;
        public bool IsPusher;
        public bool IgnoreHeight;
        public bool IsSlider;
        public bool IsAbsorbed;
        public bool WasHolePulled;
        public bool DisableVelocityTargetMultiplier;    // This is used for the vacuum.
        public bool RestAdditionalMovement = true;
        public bool SplashEffect = true;
        public bool UpdateFieldState = true;

        public new static int Index = 2;
        public static int Mask = 0x01 << Index;

        public BodyComponent(CPosition position, int offsetX, int offsetY, int width, int height, int depth)
        {
            Position = position;
            BodyBox = new CBox(position, offsetX, offsetY, width, height, depth);
        }
    }
}
