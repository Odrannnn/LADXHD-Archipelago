using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ProjectZ.InGame.Controls;

namespace ProjectZ.InGame.Archipelago
{
    public readonly struct MagpieItemContribution
    {
        public MagpieItemContribution(string id, int quantity, int maximum = int.MaxValue)
        {
            Id = id;
            Quantity = quantity;
            Maximum = maximum;
        }

        public string Id { get; }
        public int Quantity { get; }
        public int Maximum { get; }
    }

    public static class MagpieTrackerProtocol
    {
        public const int DefaultPort = 17026;
        public const string Version = "1.32";
        public const string ClientName = "ladxhd-archipelago";
        public const string WebTrackerOrigin = "https://magpietracker.us";
        private const long ArchipelagoBaseId = 10000000;

        private static readonly string[] DungeonNames =
        {
            "Tail Cave", "Bottle Grotto", "Key Cavern", "Angler's Tunnel",
            "Catfish's Maw", "Face Shrine", "Eagle's Tower", "Turtle Rock", "Color Dungeon"
        };

        private static readonly HashSet<string> TradeLocationNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Trendy Game (Mabe Village)",
            "Papahl's Wife (Mabe Village)",
            "YipYip (Mabe Village)",
            "Banana Sale (Toronbo Shores)",
            "Kiki (Ukuku Prairie)",
            "Honeycomb (Ukuku Prairie)",
            "Bear Cook (Animal Village)",
            "Papahl (Tal Tal Heights)",
            "Goat (Animal Village)",
            "MrWrite (Goponga Swamp)",
            "Grandma (Animal Village)",
            "Fisher (Martha's Bay)",
            "Mermaid (Martha's Bay)",
            "Mermaid Statue (Martha's Bay)"
        };

        private static readonly Dictionary<string, MagpieItemContribution> DirectItems =
            new Dictionary<string, MagpieItemContribution>(StringComparer.Ordinal)
            {
                ["Bomb"] = BooleanItem("BOMB"),
                ["Bow"] = BooleanItem("BOW"),
                ["Hookshot"] = BooleanItem("HOOKSHOT"),
                ["Magic Rod"] = BooleanItem("MAGIC_ROD"),
                ["Pegasus Boots"] = BooleanItem("PEGASUS_BOOTS"),
                ["Ocarina"] = BooleanItem("OCARINA"),
                ["Feather"] = BooleanItem("FEATHER"),
                ["Shovel"] = BooleanItem("SHOVEL"),
                ["Magic Powder"] = BooleanItem("MAGIC_POWDER"),
                ["Boomerang"] = BooleanItem("BOOMERANG"),
                ["Toadstool"] = BooleanItem("TOADSTOOL"),
                ["Rooster"] = BooleanItem("ROOSTER"),
                ["Progressive Sword"] = new MagpieItemContribution("SWORD", 1, 2),
                ["Progressive Power Bracelet"] = new MagpieItemContribution("POWER_BRACELET", 1, 2),
                ["Progressive Shield"] = new MagpieItemContribution("SHIELD", 1, 2),
                ["BowWow"] = BooleanItem("BOWWOW"),
                ["Max Powder Upgrade"] = BooleanItem("MAX_POWDER_UPGRADE"),
                ["Max Bombs Upgrade"] = BooleanItem("MAX_BOMBS_UPGRADE"),
                ["Max Arrows Upgrade"] = BooleanItem("MAX_ARROWS_UPGRADE"),
                ["Tail Key"] = BooleanItem("TAIL_KEY"),
                ["Slime Key"] = BooleanItem("SLIME_KEY"),
                ["Angler Key"] = BooleanItem("ANGLER_KEY"),
                ["Face Key"] = BooleanItem("FACE_KEY"),
                ["Bird Key"] = BooleanItem("BIRD_KEY"),
                ["Flippers"] = BooleanItem("FLIPPERS"),
                ["Seashell"] = new MagpieItemContribution("SEASHELL", 1),
                ["Gold Leaf"] = new MagpieItemContribution("GOLD_LEAF", 1, 5),
                ["Full Moon Cello"] = BooleanItem("INSTRUMENT1"),
                ["Conch Horn"] = BooleanItem("INSTRUMENT2"),
                ["Sea Lily's Bell"] = BooleanItem("INSTRUMENT3"),
                ["Surf Harp"] = BooleanItem("INSTRUMENT4"),
                ["Wind Marimba"] = BooleanItem("INSTRUMENT5"),
                ["Coral Triangle"] = BooleanItem("INSTRUMENT6"),
                ["Organ of Evening Calm"] = BooleanItem("INSTRUMENT7"),
                ["Thunder Drum"] = BooleanItem("INSTRUMENT8"),
                ["Yoshi Doll"] = BooleanItem("TRADING_ITEM_YOSHI_DOLL"),
                ["Ribbon"] = BooleanItem("TRADING_ITEM_RIBBON"),
                ["Dog Food"] = BooleanItem("TRADING_ITEM_DOG_FOOD"),
                ["Bananas"] = BooleanItem("TRADING_ITEM_BANANAS"),
                ["Stick"] = BooleanItem("TRADING_ITEM_STICK"),
                ["Honeycomb"] = BooleanItem("TRADING_ITEM_HONEYCOMB"),
                ["Pineapple"] = BooleanItem("TRADING_ITEM_PINEAPPLE"),
                ["Hibiscus"] = BooleanItem("TRADING_ITEM_HIBISCUS"),
                ["Letter"] = BooleanItem("TRADING_ITEM_LETTER"),
                ["Broom"] = BooleanItem("TRADING_ITEM_BROOM"),
                ["Fishing Hook"] = BooleanItem("TRADING_ITEM_FISHING_HOOK"),
                ["Necklace"] = BooleanItem("TRADING_ITEM_NECKLACE"),
                ["Scale"] = BooleanItem("TRADING_ITEM_SCALE"),
                ["Magnifying Glass"] = BooleanItem("TRADING_ITEM_MAGNIFYING_GLASS"),
                ["Ballad of the Wind Fish"] = BooleanItem("SONG1"),
                ["Manbo's Mambo"] = BooleanItem("SONG2"),
                ["Frog's Song of Soul"] = BooleanItem("SONG3"),
                ["Red Tunic"] = BooleanItem("RED_TUNIC"),
                ["Blue Tunic"] = BooleanItem("BLUE_TUNIC")
            };

        public static IReadOnlyList<string> ItemIds { get; } = BuildItemIds();

        public static Uri CreateEmbeddedTrackerUri()
        {
            var address = Uri.EscapeDataString($"127.0.0.1:{DefaultPort}");
            return new Uri(
                $"{WebTrackerOrigin}/?enable_autotracking=true" +
                $"&setting_autotrackerAddress={address}" +
                "&setting_autotrackSettings=true" +
                "&flag_ap_logic=true");
        }

        public static int CalculateEmbeddedOverlayWidth(int screenWidth)
        {
            if (screenWidth <= 0)
                return 0;
            return Math.Max(1, (int)((long)screenWidth * 7 / 10));
        }

        public static bool ShouldCloseEmbeddedTracker(
            bool trackerVisible, bool isKeyDown, int repeatCount, CButtons? button)
        {
            return trackerVisible && isKeyDown && repeatCount == 0 &&
                   button is CButtons.B or CButtons.Select;
        }

        public static string GetCheckId(ArchipelagoSeedLocation location)
        {
            if (location == null)
                return null;

            var encoded = location.LocationId - ArchipelagoBaseId;
            if (encoded < 0)
                return null;

            var room = (int)(encoded % 1000);
            var suffix = encoded / 1000;
            if (room < 0 || room > 0xFFF || suffix < 0)
                return null;

            // AP 0.6.7 renamed the two shop checks while Magpie retained LADXR's shared-room IDs.
            if (suffix == 0 && room == 0x2A1)
                return "0x2A1-0";
            if (suffix == 0 && room == 0x2A7)
                return "0x2A1-1";

            var id = $"0x{room:X3}";
            if (suffix == 0)
                return id;
            if (suffix > 1)
                return $"{id}-{suffix - 1}";

            if (location.LocationName?.IndexOf("owl", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"{id}-Owl";
            if (TradeLocationNames.Contains(location.LocationName ?? string.Empty))
                return $"{id}-Trade";
            return $"{id}-0";
        }

        public static bool TryGetItemContribution(string itemName, out MagpieItemContribution contribution)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                contribution = default;
                return false;
            }

            if (DirectItems.TryGetValue(itemName, out contribution))
                return true;

            for (var index = 0; index < DungeonNames.Length; index++)
            {
                if (!itemName.EndsWith($"({DungeonNames[index]})", StringComparison.Ordinal))
                    continue;

                var dungeon = index + 1;
                if (itemName.StartsWith("Small Key ", StringComparison.Ordinal))
                    contribution = new MagpieItemContribution($"KEY{dungeon}", 1);
                else if (itemName.StartsWith("Nightmare Key ", StringComparison.Ordinal))
                    contribution = BooleanItem($"NIGHTMARE_KEY{dungeon}");
                else if (itemName.StartsWith("Dungeon Map ", StringComparison.Ordinal))
                    contribution = BooleanItem($"MAP{dungeon}");
                else if (itemName.StartsWith("Compass ", StringComparison.Ordinal))
                    contribution = BooleanItem($"COMPASS{dungeon}");
                else if (itemName.StartsWith("Stone Beak ", StringComparison.Ordinal))
                    contribution = BooleanItem($"STONE_BEAK{dungeon}");
                else
                    break;
                return true;
            }

            contribution = default;
            return false;
        }

        public static string CreateHandshakeAcknowledgement() => JsonSerializer.Serialize(new
        {
            type = "handshAck",
            version = Version,
            name = ClientName
        });

        public static string CreateItemsMessage(IEnumerable<KeyValuePair<string, int>> items, bool diff)
        {
            return JsonSerializer.Serialize(new
            {
                type = "item",
                refresh = true,
                diff,
                items = items.Select(item => new { id = item.Key, qty = item.Value })
            });
        }

        public static string CreateChecksMessage(IEnumerable<KeyValuePair<string, bool>> checks, bool diff)
        {
            return JsonSerializer.Serialize(new
            {
                type = "check",
                refresh = true,
                diff,
                checks = checks.Select(check => new { id = check.Key, @checked = check.Value })
            });
        }

        private static MagpieItemContribution BooleanItem(string id) =>
            new MagpieItemContribution(id, 1, 1);

        private static IReadOnlyList<string> BuildItemIds()
        {
            var ids = new List<string>
            {
                "BOMB", "BOW", "HOOKSHOT", "MAGIC_ROD", "PEGASUS_BOOTS", "OCARINA", "FEATHER",
                "SHOVEL", "MAGIC_POWDER", "BOOMERANG", "TOADSTOOL", "ROOSTER", "RUPEE_COUNT",
                "SWORD", "POWER_BRACELET", "SHIELD", "BOWWOW", "MAX_POWDER_UPGRADE",
                "MAX_BOMBS_UPGRADE", "MAX_ARROWS_UPGRADE", "TAIL_KEY", "SLIME_KEY", "ANGLER_KEY",
                "FACE_KEY", "BIRD_KEY", "FLIPPERS", "SEASHELL", "GOLD_LEAF"
            };

            for (var dungeon = 1; dungeon <= 8; dungeon++)
                ids.Add($"INSTRUMENT{dungeon}");

            ids.AddRange(new[]
            {
                "TRADING_ITEM_YOSHI_DOLL", "TRADING_ITEM_RIBBON", "TRADING_ITEM_DOG_FOOD",
                "TRADING_ITEM_BANANAS", "TRADING_ITEM_STICK", "TRADING_ITEM_HONEYCOMB",
                "TRADING_ITEM_PINEAPPLE", "TRADING_ITEM_HIBISCUS", "TRADING_ITEM_LETTER",
                "TRADING_ITEM_BROOM", "TRADING_ITEM_FISHING_HOOK", "TRADING_ITEM_NECKLACE",
                "TRADING_ITEM_SCALE", "TRADING_ITEM_MAGNIFYING_GLASS", "SONG1", "SONG2", "SONG3",
                "RED_TUNIC", "BLUE_TUNIC", "GREAT_FAIRY"
            });

            for (var dungeon = 1; dungeon <= 9; dungeon++)
            {
                ids.Add($"MAP{dungeon}");
                ids.Add($"COMPASS{dungeon}");
                ids.Add($"STONE_BEAK{dungeon}");
                ids.Add($"NIGHTMARE_KEY{dungeon}");
                ids.Add($"KEY{dungeon}");
            }

            return ids;
        }
    }
}
