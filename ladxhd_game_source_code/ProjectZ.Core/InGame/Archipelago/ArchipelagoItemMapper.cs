using System;
using System.Collections.Generic;

namespace ProjectZ.InGame.Archipelago
{
    public enum ArchipelagoItemEffect
    {
        None,
        BadHeartContainer,
        BowWow,
        Rooster,
        TradeStick,
        TradePineapple,
        TradeScale,
        TradeMagnifyingGlass,
        ZolAttack,
        MaxPowderUpgrade,
        MaxBombsUpgrade,
        MaxArrowsUpgrade
    }

    public readonly struct ArchipelagoItemMapping
    {
        public ArchipelagoItemMapping(string gameItemName, int count = 1, string locationBounding = null,
            ArchipelagoItemEffect effect = ArchipelagoItemEffect.None)
        {
            GameItemName = gameItemName;
            Count = count;
            LocationBounding = locationBounding;
            Effect = effect;
        }

        public string GameItemName { get; }
        public int Count { get; }
        public string LocationBounding { get; }
        public ArchipelagoItemEffect Effect { get; }
        public bool IsNoOp => string.IsNullOrEmpty(GameItemName) && Effect == ArchipelagoItemEffect.None;
    }

    public static class ArchipelagoItemMapper
    {
        private static readonly Dictionary<string, ArchipelagoItemMapping> DirectMappings =
            new Dictionary<string, ArchipelagoItemMapping>(StringComparer.Ordinal)
            {
                ["Bow"] = new ArchipelagoItemMapping("bow", 30),
                ["Hookshot"] = new ArchipelagoItemMapping("hookshot"),
                ["Magic Rod"] = new ArchipelagoItemMapping("magicRod"),
                ["Pegasus Boots"] = new ArchipelagoItemMapping("pegasusBoots"),
                ["Ocarina"] = new ArchipelagoItemMapping("ocarina"),
                ["Feather"] = new ArchipelagoItemMapping("feather"),
                ["Shovel"] = new ArchipelagoItemMapping("shovel"),
                ["Magic Powder"] = new ArchipelagoItemMapping("powder", 10),
                ["Bomb"] = new ArchipelagoItemMapping("bomb", 10),
                ["Flippers"] = new ArchipelagoItemMapping("flippers"),
                ["Medicine"] = new ArchipelagoItemMapping("potion"),
                ["Tail Key"] = new ArchipelagoItemMapping("dkey1"),
                ["Slime Key"] = new ArchipelagoItemMapping("dkey2"),
                ["Angler Key"] = new ArchipelagoItemMapping("dkey3"),
                ["Face Key"] = new ArchipelagoItemMapping("dkey4"),
                ["Bird Key"] = new ArchipelagoItemMapping("dkey5"),
                ["Gold Leaf"] = new ArchipelagoItemMapping("goldLeaf"),
                ["20 Rupees"] = new ArchipelagoItemMapping("ruby", 20),
                ["50 Rupees"] = new ArchipelagoItemMapping("ruby", 50),
                ["100 Rupees"] = new ArchipelagoItemMapping("ruby", 100),
                ["200 Rupees"] = new ArchipelagoItemMapping("ruby", 200),
                ["500 Rupees"] = new ArchipelagoItemMapping("ruby", 500),
                ["Seashell"] = new ArchipelagoItemMapping("shell"),
                ["Nothing"] = new ArchipelagoItemMapping(null),
                ["Zol Attack"] = new ArchipelagoItemMapping(null, effect: ArchipelagoItemEffect.ZolAttack),
                ["Boomerang"] = new ArchipelagoItemMapping("boomerang"),
                ["Heart Piece"] = new ArchipelagoItemMapping("heartMeter"),
                ["Heart Container"] = new ArchipelagoItemMapping("heartMeterFull", 4),
                ["Bad Heart Container"] = new ArchipelagoItemMapping(null, effect: ArchipelagoItemEffect.BadHeartContainer),
                ["BowWow"] = new ArchipelagoItemMapping(null, effect: ArchipelagoItemEffect.BowWow),
                ["10 Arrows"] = new ArchipelagoItemMapping("arrow", 10),
                ["Single Arrow"] = new ArchipelagoItemMapping("arrow", 1),
                ["Rooster"] = new ArchipelagoItemMapping("rooster", effect: ArchipelagoItemEffect.Rooster),
                ["Max Powder Upgrade"] = new ArchipelagoItemMapping(null, effect: ArchipelagoItemEffect.MaxPowderUpgrade),
                ["Max Bombs Upgrade"] = new ArchipelagoItemMapping(null, effect: ArchipelagoItemEffect.MaxBombsUpgrade),
                ["Max Arrows Upgrade"] = new ArchipelagoItemMapping(null, effect: ArchipelagoItemEffect.MaxArrowsUpgrade),
                ["Red Tunic"] = new ArchipelagoItemMapping("cloakRed"),
                ["Blue Tunic"] = new ArchipelagoItemMapping("cloakBlue"),
                ["Toadstool"] = new ArchipelagoItemMapping("toadstool"),
                ["Guardian Acorn"] = new ArchipelagoItemMapping("guardianAcorn"),
                ["Piece Of Power"] = new ArchipelagoItemMapping("pieceOfPower"),
                ["Ballad of the Wind Fish"] = new ArchipelagoItemMapping("ocarina_maria"),
                ["Manbo's Mambo"] = new ArchipelagoItemMapping("ocarina_manbo"),
                ["Frog's Song of Soul"] = new ArchipelagoItemMapping("ocarina_frog"),
                ["Full Moon Cello"] = new ArchipelagoItemMapping("instrument0"),
                ["Conch Horn"] = new ArchipelagoItemMapping("instrument1"),
                ["Sea Lily's Bell"] = new ArchipelagoItemMapping("instrument2"),
                ["Surf Harp"] = new ArchipelagoItemMapping("instrument3"),
                ["Wind Marimba"] = new ArchipelagoItemMapping("instrument4"),
                ["Coral Triangle"] = new ArchipelagoItemMapping("instrument5"),
                ["Organ of Evening Calm"] = new ArchipelagoItemMapping("instrument6"),
                ["Thunder Drum"] = new ArchipelagoItemMapping("instrument7"),
                ["Yoshi Doll"] = new ArchipelagoItemMapping("trade0"),
                ["Ribbon"] = new ArchipelagoItemMapping("trade1"),
                ["Dog Food"] = new ArchipelagoItemMapping("trade2"),
                ["Bananas"] = new ArchipelagoItemMapping("trade3"),
                ["Stick"] = new ArchipelagoItemMapping("trade4", effect: ArchipelagoItemEffect.TradeStick),
                ["Honeycomb"] = new ArchipelagoItemMapping("trade5"),
                ["Pineapple"] = new ArchipelagoItemMapping("trade6", effect: ArchipelagoItemEffect.TradePineapple),
                ["Hibiscus"] = new ArchipelagoItemMapping("trade7"),
                ["Letter"] = new ArchipelagoItemMapping("trade8"),
                ["Broom"] = new ArchipelagoItemMapping("trade9"),
                ["Fishing Hook"] = new ArchipelagoItemMapping("trade10"),
                ["Necklace"] = new ArchipelagoItemMapping("trade11"),
                ["Scale"] = new ArchipelagoItemMapping("trade12", effect: ArchipelagoItemEffect.TradeScale),
                ["Magnifying Glass"] = new ArchipelagoItemMapping("trade13", effect: ArchipelagoItemEffect.TradeMagnifyingGlass)
            };

        private static readonly Dictionary<string, string> DungeonBounds = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Tail Cave"] = "one",
            ["Bottle Grotto"] = "two",
            ["Key Cavern"] = "three",
            ["Angler's Tunnel"] = "four",
            ["Catfish's Maw"] = "five",
            ["Face Shrine"] = "six",
            ["Eagle's Tower"] = "seven",
            ["Turtle Rock"] = "eight",
            ["Color Dungeon"] = "dColor"
        };

        public static bool TryMap(string itemName, int swordLevel, int shieldLevel, int braceletLevel,
            out ArchipelagoItemMapping mapping)
        {
            if (itemName == "Progressive Sword")
            {
                mapping = new ArchipelagoItemMapping(swordLevel < 1 ? "sword1" : "sword2");
                return true;
            }
            if (itemName == "Progressive Shield")
            {
                mapping = new ArchipelagoItemMapping(shieldLevel < 1 ? "shield" : "mirrorShield");
                return true;
            }
            if (itemName == "Progressive Power Bracelet")
            {
                mapping = new ArchipelagoItemMapping(braceletLevel < 1 ? "stonelifter" : "stonelifter2");
                return true;
            }
            if (DirectMappings.TryGetValue(itemName, out mapping))
                return true;

            foreach (var dungeon in DungeonBounds)
            {
                if (!itemName.EndsWith($"({dungeon.Key})", StringComparison.Ordinal))
                    continue;

                if (itemName.StartsWith("Small Key ", StringComparison.Ordinal))
                    mapping = new ArchipelagoItemMapping("smallkey", locationBounding: dungeon.Value);
                else if (itemName.StartsWith("Nightmare Key ", StringComparison.Ordinal))
                    mapping = new ArchipelagoItemMapping("nightmarekey", locationBounding: dungeon.Value);
                else if (itemName.StartsWith("Dungeon Map ", StringComparison.Ordinal))
                    mapping = new ArchipelagoItemMapping("dmap", locationBounding: dungeon.Value);
                else if (itemName.StartsWith("Compass ", StringComparison.Ordinal))
                    mapping = new ArchipelagoItemMapping("compass", locationBounding: dungeon.Value);
                else if (itemName.StartsWith("Stone Beak ", StringComparison.Ordinal))
                    mapping = new ArchipelagoItemMapping("stonebeak", locationBounding: dungeon.Value);
                else
                    break;

                return true;
            }

            mapping = default;
            return false;
        }
    }
}
