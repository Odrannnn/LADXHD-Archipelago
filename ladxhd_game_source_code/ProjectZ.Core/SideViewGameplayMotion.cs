using System;
using Microsoft.Xna.Framework;

namespace ProjectZ;

// ObjLink2d/SystemBody calculations shared with the silent wallpaper simulator.
public static class SideViewGameplayMotion
{
    public const float Gravity = 0.1f;
    public const float ClimbSpeed = 0.7f;
    public const float SwimSpeed = 0.5f;
    public const float WaterExitVelocity = -0.75f;
    public const float LadderTopVelocity = -0.5f;
    public const float LadderJumpDelayMilliseconds = 175f;

    public static float FeatherVelocity(bool climbing, bool walking) =>
        climbing ? -1.5f : walking ? -2.10f : -1.95f;

    public static Rectangle LadderBounds(int x, int y, bool top) =>
        top ? new Rectangle(x, y, 16, 16) : new Rectangle(x + 4, y, 8, 16);

    public static bool LadderCollides(bool top, int direction) => !top || direction == 3;

    // ObjColliderOneWay's flat-top pusher raises a descending Link who is
    // already below its one-pixel surface (the installed stair-step geometry).
    public static float? OneWayPusherY(bool sideView, float feetY, float velocityY, float surfaceY) =>
        sideView && feetY > surfaceY && velocityY > 0 && Math.Abs(feetY - surfaceY) > 0.1f
            ? surfaceY - 1 : null;

    public static void ReleaseFeather(ref float velocity, ref bool variableJump, bool held)
    {
        if (!held && variableJump)
        {
            var threshold = -1f;
            var replacement = -0.5f;
            for (var i = 0; i < 3; i++)
            {
                if (velocity > threshold)
                {
                    velocity = replacement;
                    variableJump = false;
                    break;
                }
                threshold -= 0.15f;
                replacement += 0.15f;
            }
        }
        if (velocity >= -0.5f) variableJump = false;
    }

    public static Vector2 AirMovement(Vector2 previous, Vector2 input,
        float speed, float timeMultiplier)
    {
        input.Y = 0;
        if (input == Vector2.Zero || (previous - input * speed).LengthSquared() <= 0)
            return previous;
        var target = Vector2.Normalize(input) * Math.Max(input.Length(), previous.Length());
        var distance = Vector2.Distance(previous, target);
        return distance <= 0 ? previous : Vector2.Lerp(previous, target,
            Math.Min(1, 0.05f * timeMultiplier / distance));
    }

    public static Vector2 SwimMovement(Vector2 previous, Vector2 input,
        float maximumSpeed, float timeMultiplier)
    {
        var length = input.Length();
        var target = length > 0 ? input / length * Math.Min(length, maximumSpeed) : Vector2.Zero;
        var distance = Vector2.Distance(previous, target);
        return distance <= 0 ? target : Vector2.Lerp(previous, target,
            Math.Min(1, 0.0225f * timeMultiplier / distance));
    }

    public static float VerticalVelocity(float velocity, bool grounded,
        bool wasGrounded, float bounciness, float gravity, float timeMultiplier)
    {
        if (grounded)
            velocity = !wasGrounded && velocity * bounciness > 0.4f
                ? -velocity * bounciness : 0;
        return velocity + gravity * timeMultiplier;
    }
}
