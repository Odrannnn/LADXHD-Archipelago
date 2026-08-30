using Microsoft.Xna.Framework;

namespace ProjectZ;

// Shared ObjDoor geometry. Stairs use the inset trigger, not the full visual tile.
public static class DoorGameplayGeometry
{
    public static Rectangle GetTrigger(int x, int y, int width, int height,
        int mode, bool is2dMap)
    {
        var rectangle = mode == 1 && !is2dMap
            ? new Rectangle(x + 6, y + 6, width - 12, height - 12)
            : new Rectangle(x, y, width, height);
        if (mode == 4)
            rectangle.Height = 10;
        return rectangle;
    }

    public static Vector2 GetWalkingSpawn(Rectangle trigger, int direction,
        int mode, bool is2dMap, float bodyWidth, float bodyHeight)
    {
        var offset = mode == 1 && !is2dMap ? 4 : 0;
        var position = new Vector2(trigger.X + trigger.Width / 2f, trigger.Y + trigger.Height / 2f + bodyHeight / 2f);
        if (direction == 0)
            position.X = trigger.X - System.MathF.Ceiling(bodyWidth / 2f) - offset;
        else if (direction == 1)
            position.Y = trigger.Y - offset;
        else if (direction == 2)
            position.X = trigger.Right + System.MathF.Ceiling(bodyWidth / 2f) + offset;
        else if (direction == 3)
            position.Y = trigger.Bottom + bodyHeight + offset;
        if (is2dMap)
        {
            if (direction % 2 == 0)
                position.Y = trigger.Bottom;
            else
                position.Y += direction == 1 ? -4 : 4;
        }
        return position;
    }
}
