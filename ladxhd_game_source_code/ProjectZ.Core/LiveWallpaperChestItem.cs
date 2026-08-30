using System;

namespace ProjectZ
{
    public static class ChestGameplayPresentation
    {
        public const int OpeningMilliseconds = 300;

        public static float ResolveItemHeight(float progress) =>
            MathF.Sin(Math.Clamp(progress, 0f, 1f) * MathF.PI / 1.55f) * 12f;
    }

    public readonly struct LiveWallpaperChestItemVisual
    {
        public LiveWallpaperChestItemVisual(string spriteId, int showAnimation)
        {
            SpriteId = spriteId;
            ShowAnimation = showAnimation == 2 ? 2 : 1;
        }

        public string SpriteId { get; }
        public int ShowAnimation { get; }
    }

    /// <summary>
    /// Resolves the item-atlas sprite used by ItemManager for chest contents. The wallpaper does
    /// not initialize the gameplay item manager, saves, or scripts, so aliases that normally fall
    /// back through GameItem.Name are resolved here before loading the installed item atlas.
    /// </summary>
    public static class LiveWallpaperChestItem
    {
        public const int PresentationMilliseconds = 1_500;

        public static bool TryResolve(
            string itemName, out LiveWallpaperChestItemVisual visual)
        {
            visual = default;
            if (string.IsNullOrWhiteSpace(itemName) ||
                string.Equals(itemName, "greenZol", StringComparison.Ordinal) ||
                itemName.StartsWith("dialog:", StringComparison.Ordinal))
                return false;

            var spriteId = itemName switch
            {
                "smallkeyChest" => "smallkey",
                "shellChest" => "shell",
                "shellPresent" => "shell_present",
                "potion_show" => "potion",
                "goldLeaf" => "goldLeafMenu",
                "marin" or "rooster" or "ghost" => "marin_item",
                "ruby" or "ruby10" or "ruby20" or "ruby50" or
                    "ruby100" or "ruby200" => "rubyBlue",
                "ruby5" or "ruby30" => "rubyRed",
                "heart_1" or "heart_3" => "heart",
                "heartMeterSilent" => "heartMeter",
                "shield0" or "shieldBack" => "shield",
                "mirrorShield" => "mirror shield",
                "stonelifter" => "stonelifter0",
                "stonelifter2" => "stonelifter1",
                "ocarina_frog" or "ocarina_maria" or "ocarina_manbo" =>
                    "ocarina",
                "powderTrendy" or "powder_1" or "powder_10" or "powderPD" =>
                    "powder",
                "bombChest" or "bomb_1" or "bomb_10" => "bomb",
                "arrow_1" => "arrow",
                "cloakRed" or "cloakBlue" => "cloak",
                _ => itemName
            };
            var showAnimation = itemName is
                "guardianAcorn" or "pieceOfPower" or "sword2" or "powderPD"
                ? 2
                : 1;
            visual = new LiveWallpaperChestItemVisual(spriteId, showAnimation);
            return true;
        }
    }
}
