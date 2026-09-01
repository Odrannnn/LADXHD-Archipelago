using System;
using Microsoft.Xna.Framework;

namespace ProjectZ;

// ObjDungeonDoor's deterministic presentation, shared with the silent wallpaper.
public static class DungeonDoorGameplay
{
    public const int UnlockPushMilliseconds = 100;
    public const int SmallKeyPickupCount = 1;
    public const int SmallKeyCapacity = 9;
    public const int SmallKeyCollectWidth = 8;
    public const int SmallKeyCollectHeight = 14;
    public const int SmallKeyCollectOffsetX = -1;
    public static string RequiredItem(int mode) => mode == 1 ? "smallkey" : mode == 3 ? "nightmarekey" : null;
    public static bool HasRequiredKey(int mode, int? count) => mode == 1 ? count > 0 : mode == 3 && count.HasValue;
    public static bool IsOpenKey(string value) => value != null && value != "0";
    public static float Rotation(int direction) => (float)(Math.PI / 2 * (direction + 1));
    public static float Open(float amount, float frames) => Math.Max(0, amount - frames * 0.05f);
    public static float Close(float amount, float frames) => Math.Min(1, amount + frames * 0.1f);
    public static bool BlocksWhileOpening(float amount) => amount > 0.5f;
    public static Rectangle Variant(Rectangle source, int mode)
    {
        source.X += mode * 16;
        return source;
    }
    public static Rectangle Source(Rectangle variant, float amount)
    {
        variant.Height = (int)Math.Round(16 * amount);
        variant.Y += 16 - variant.Height;
        return variant;
    }
}
