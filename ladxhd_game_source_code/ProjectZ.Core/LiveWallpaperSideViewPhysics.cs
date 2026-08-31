using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ;

internal struct SideViewBody
{
    public Vector2 Position;
    public Vector2 Movement;
    public Vector2 SwimVelocity;
    public float FallVelocity;
    public bool Grounded, Climbing, Swimming, JumpHeld, VariableJump;
    public int JumpAge, Direction;
}

internal readonly record struct SideViewInput(Vector2 Move, bool Jump = false);

// The planner advances this same body with button inputs; it never invents
// vertical waypoints which the real side-view physics cannot reach.
internal static class LiveWallpaperSideViewPhysics
{
    public static SideViewBody Spawn(LiveWallpaperMap map, Vector2 position) => new()
    {
        Position = position,
        Climbing = map.TouchesSideViewLadder(position, 3),
        Grounded = map.SideViewCollision(position + Vector2.UnitY, 3, true, out _),
        JumpAge = 12,
        Direction = map.TouchesSideViewLadder(position, 3) ? 1 : 2
    };

    public static bool Step(LiveWallpaperMap map, ref SideViewBody body, SideViewInput input)
    {
        var wasClimbing = body.Climbing;
        var ladder = map.TouchesSideViewLadder(body.Position);
        if (!ladder && body.Climbing)
        {
            body.Climbing = false;
            body.FallVelocity = 0;
            // Native _lastMoveVelocity contains only horizontal ladder motion.
            body.Movement.Y = 0;
        }
        var swimming = map.IsDeepWaterAt(body.Position.X, body.Position.Y - 9);
        if (swimming && !body.Swimming)
        {
            body.SwimVelocity = new Vector2(body.Movement.X * .35f,
                body.Climbing ? body.Movement.Y * .35f : body.FallVelocity);
            body.FallVelocity = 0;
            if (body.Direction % 2 != 0) body.Direction = 0;
        }
        else if (!swimming && body.Swimming)
        {
            // ObjLink2d.UpdateWaterLava's unboosted exit hop.
            body.FallVelocity = SideViewGameplayMotion.WaterExitVelocity;
            body.Movement = new Vector2(body.SwimVelocity.X, 0);
        }
        body.Swimming = swimming;
        if (swimming) body.Climbing = false;
        else if (ladder && input.Move.Y != 0 &&
                 Math.Abs(input.Move.X) <= Math.Abs(input.Move.Y) && body.JumpAge > 10)
            body.Climbing = true;

        if (body.Swimming)
        {
            body.SwimVelocity = SideViewGameplayMotion.SwimMovement(
                body.SwimVelocity, input.Move, SideViewGameplayMotion.SwimSpeed, 1);
            body.Movement = body.SwimVelocity;
        }
        else if (body.Climbing)
        {
            body.Movement = input.Move * SideViewGameplayMotion.ClimbSpeed;
            body.FallVelocity = 0;
            body.Direction = 1;
        }
        else if (body.Grounded)
            body.Movement = new Vector2(input.Move.X * LinkGameplayMotion.WalkSpeed, 0);
        else
            body.Movement = SideViewGameplayMotion.AirMovement(
                body.Movement, input.Move, LinkGameplayMotion.WalkSpeed, 1);

        if (!body.Climbing && input.Move.X != 0) body.Direction = input.Move.X < 0 ? 0 : 2;
        if (input.Jump && !body.JumpHeld && !body.Swimming && (body.Grounded || body.Climbing))
        {
            body.FallVelocity = SideViewGameplayMotion.FeatherVelocity(
                body.Climbing, body.Movement != Vector2.Zero);
            body.Grounded = body.Climbing = false;
            body.VariableJump = true;
            body.JumpAge = 0;
            body.Movement.Y = 0;
        }
        body.JumpHeld = input.Jump;
        body.JumpAge = Math.Min(12, body.JumpAge + 1);
        SideViewGameplayMotion.ReleaseFeather(ref body.FallVelocity, ref body.VariableJump, input.Jump);

        var wasGrounded = body.Grounded;
        var lastY = body.Position.Y;
        var ladderTops = !body.Climbing && input.Move.Y <= 0;
        var collision = Move(map, ref body, body.Movement, ladderTops);
        collision |= Move(map, ref body, new Vector2(0, body.FallVelocity), ladderTops);
        body.Grounded = (collision & Values.BodyCollision.Vertical) != 0 && body.FallVelocity > 0;
        body.FallVelocity = SideViewGameplayMotion.VerticalVelocity(body.FallVelocity,
            body.Grounded, wasGrounded, 0, body.Swimming ? 0 : SideViewGameplayMotion.Gravity, 1);
        if (wasClimbing && !body.Climbing && !body.Grounded && !body.Swimming && !input.Jump)
        {
            // ObjLink2d.UpdateJump2D probes down two pixels when leaving a
            // ladder. At its top it becomes grounded with the canonical hop;
            // otherwise the successful probe is undone and falling continues.
            if (Move(map, ref body, new Vector2(0, 2), true) == Values.BodyCollision.None)
                Move(map, ref body, new Vector2(0, -2), true);
            else
            {
                body.Grounded = true;
                body.FallVelocity = SideViewGameplayMotion.LadderTopVelocity;
            }
        }
        if (body.Climbing && input.Move.Y > 0 && body.Position.Y == lastY &&
            map.SideViewCollision(body.Position + Vector2.UnitY, 3, false, out _))
        {
            body.Climbing = false;
            body.Grounded = true;
            body.Movement = Vector2.Zero;
        }
        // ObjLink2d's collision callback runs after SystemBody's gravity step.
        if ((collision & Values.BodyCollision.Bottom) != 0)
            body.VariableJump = false;
        else if ((collision & Values.BodyCollision.Top) != 0)
            body.FallVelocity = 0;
        else if ((collision & Values.BodyCollision.Horizontal) != 0)
        {
            body.Movement.X = 0;
            body.SwimVelocity.X = 0;
        }
        if ((collision & Values.BodyCollision.Vertical) != 0) body.SwimVelocity.Y = 0;
        return map.SideViewPositionInBounds(body.Position);
    }

    // SystemBody moves X then Y and aligns exactly to the collided face.
    // ObjLink disables corner correction in 2D; no top-down nudges or Z jump.
    private static Values.BodyCollision Move(LiveWallpaperMap map, ref SideViewBody body, Vector2 offset, bool ladderTops)
    {
        var collision = Values.BodyCollision.None;
        if (offset.X != 0)
        {
            var direction = offset.X < 0 ? 0 : 2;
            var next = body.Position + new Vector2(offset.X, 0);
            if (!map.SideViewMovementCollision(ref body, next, direction, ladderTops, out var box))
                body.Position.X += offset.X;
            else
            {
                collision |= Values.BodyCollision.Horizontal |
                    (direction == 0 ? Values.BodyCollision.Left : Values.BodyCollision.Right);
                var aligned = new Vector2(direction == 0 ? box.Right + 4 : box.Left - 4, body.Position.Y);
                if (Math.Abs(aligned.X - body.Position.X) < Math.Abs(offset.X) &&
                    !map.SideViewMovementCollision(ref body, aligned, direction, ladderTops, out _)) body.Position = aligned;
                // ObjLink enables SystemBody's one-pixel step-up in 2D.
                if (body.Grounded && offset.Y == 0 &&
                    !map.SideViewMovementCollision(ref body, body.Position - Vector2.UnitY, 1, ladderTops, out _) &&
                    !map.SideViewMovementCollision(ref body, body.Position + new Vector2(offset.X, -1), direction, ladderTops, out _))
                {
                    body.Position += new Vector2(offset.X, -1);
                    return Values.BodyCollision.None;
                }
            }
        }
        if (offset.Y == 0) return collision;
        var verticalDirection = offset.Y < 0 ? 1 : 3;
        var verticalNext = body.Position + new Vector2(0, offset.Y);
        if (!map.SideViewMovementCollision(ref body, verticalNext, verticalDirection, ladderTops, out var verticalBox))
        {
            body.Position.Y += offset.Y;
            return collision;
        }
        var alignedY = new Vector2(body.Position.X, verticalDirection == 1 ? verticalBox.Bottom + 10 : verticalBox.Top);
        if (Math.Abs(alignedY.Y - body.Position.Y) < Math.Abs(offset.Y) &&
            !map.SideViewMovementCollision(ref body, alignedY, verticalDirection, ladderTops, out _)) body.Position = alignedY;
        return collision | Values.BodyCollision.Vertical |
            (verticalDirection == 1 ? Values.BodyCollision.Top : Values.BodyCollision.Bottom);
    }
}
