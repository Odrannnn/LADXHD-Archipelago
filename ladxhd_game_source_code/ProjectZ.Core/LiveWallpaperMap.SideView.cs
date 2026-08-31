using System;
using Microsoft.Xna.Framework;

namespace ProjectZ;

public sealed partial class LiveWallpaperMap
{
    // Side-view air is not a top-down void/hole. Only the installed collision
    // objects block it; ladder tops and one-way platforms retain their direction.
    internal bool SideViewCollision(Vector2 feet, int direction, bool ladderTops,
        out Rectangle bounds, bool laddersOnly = false)
    {
        var position = feet;
        return FindSideViewCollision(feet, direction, ladderTops, out bounds,
            laddersOnly, null, ref position, 0);
    }

    internal bool SideViewMovementCollision(ref SideViewBody body, Vector2 target,
        int direction, bool ladderTops, out Rectangle bounds) =>
        FindSideViewCollision(target, direction, ladderTops, out bounds, false,
            body.Position, ref body.Position, body.FallVelocity);

    private bool FindSideViewCollision(Vector2 feet, int direction, bool ladderTops,
        out Rectangle bounds, bool laddersOnly, Vector2? previousFeet,
        ref Vector2 bodyPosition, float fallVelocity)
    {
        bounds = default;
        var x = feet.X - 4f;
        var y = feet.Y - 10f;
        var firstX = Math.Clamp((int)MathF.Floor(x / 16f), 0, Width - 1);
        var firstY = Math.Clamp((int)MathF.Floor(y / 16f), 0, Height - 1);
        var lastX = Math.Clamp((int)MathF.Floor((x + 7.999f) / 16f), 0, Width - 1);
        var lastY = Math.Clamp((int)MathF.Floor((y + 9.999f) / 16f), 0, Height - 1);
        for (var row = firstY; row <= lastY; row++)
        for (var column = firstX; column <= lastX; column++)
        {
            var entries = _collisionGrid?[column, row];
            if (entries == null) continue;
            foreach (var entry in entries)
            {
                if (!entry.Intersects(x, y, 8, 10)) continue;
                if (laddersOnly)
                {
                    if (entry.Kind is not (CollisionKind.Ladder or CollisionKind.LadderTop) ||
                        !SideViewGameplayMotion.LadderCollides(entry.Kind == CollisionKind.LadderTop, direction))
                        continue;
                }
                else if (entry.Kind is CollisionKind.Ladder or CollisionKind.Hole or CollisionKind.NpcWall ||
                         entry.Kind == CollisionKind.LadderTop && (!ladderTops || direction != 3) ||
                         entry.Direction >= 0 && entry.Direction != direction)
                    continue;
                if (previousFeet.HasValue)
                {
                    // ObjColliderOneWay's callback runs before ObjectManager
                    // filters old-body overlaps, and may raise Link above its
                    // flat surface even when that overlap is then ignored.
                    if (entry.Direction == 3)
                    {
                        var pushedY = SideViewGameplayMotion.OneWayPusherY(
                            true, bodyPosition.Y, fallVelocity, entry.Y);
                        if (pushedY.HasValue) bodyPosition.Y = pushedY.Value;
                    }
                    // ObjLink retains BodyComponent.IgnoreInsideCollision:
                    // a collider touching the old body is not a new wall.
                    var old = previousFeet.Value;
                    if (entry.Intersects(old.X - 4, old.Y - 10, 8, 10)) continue;
                }
                bounds = new Rectangle(entry.X, entry.Y, entry.Width, entry.Height);
                return true;
            }
        }
        return false;
    }

    internal bool TouchesSideViewLadder(Vector2 feet, int direction = 1) =>
        SideViewCollision(feet, direction, false, out _, laddersOnly: true);

    internal bool SideViewPositionInBounds(Vector2 feet) =>
        feet.X >= 4 && feet.X <= Width * 16 - 4 && feet.Y >= 0 && feet.Y <= Height * 16;
}
