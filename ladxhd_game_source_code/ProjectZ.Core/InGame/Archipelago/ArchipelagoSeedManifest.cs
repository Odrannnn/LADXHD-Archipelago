using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectZ.InGame.Archipelago
{
    public sealed class ArchipelagoSeedManifest
    {
        public const int CurrentFormatVersion = 1;

        [JsonPropertyName("format_version")]
        public int FormatVersion { get; set; }

        [JsonPropertyName("game")]
        public string Game { get; set; }

        [JsonPropertyName("seed_name")]
        public string SeedName { get; set; }

        [JsonPropertyName("slot_name")]
        public string SlotName { get; set; }

        [JsonPropertyName("world_version")]
        public string WorldVersion { get; set; }

        [JsonPropertyName("locations")]
        public List<ArchipelagoSeedLocation> Locations { get; set; } = new List<ArchipelagoSeedLocation>();

        [JsonPropertyName("options")]
        public Dictionary<string, JsonElement> Options { get; set; } = new Dictionary<string, JsonElement>();

        [JsonPropertyName("mapping_complete")]
        public bool? MappingComplete { get; set; }

        [JsonPropertyName("unmapped_locations")]
        public List<string> UnmappedLocations { get; set; } = new List<string>();

        [JsonIgnore]
        public IReadOnlyDictionary<string, ArchipelagoSeedLocation> LocationsByGameKey { get; private set; }

        public static ArchipelagoSeedManifest Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A seed manifest path is required.", nameof(path));

            var json = File.ReadAllText(path);
            var manifest = JsonSerializer.Deserialize<ArchipelagoSeedManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (manifest == null)
                throw new InvalidDataException("The Archipelago seed manifest is empty.");

            manifest.Validate();
            return manifest;
        }

        public void Validate()
        {
            if (FormatVersion != CurrentFormatVersion)
                throw new InvalidDataException($"Unsupported .apladxhd format version {FormatVersion}; expected {CurrentFormatVersion}.");
            if (!string.Equals(Game, ArchipelagoManager.GameName, StringComparison.Ordinal))
                throw new InvalidDataException($"The seed is for '{Game}', not '{ArchipelagoManager.GameName}'.");
            if (string.IsNullOrWhiteSpace(SeedName))
                throw new InvalidDataException("The seed manifest has no seed_name.");
            if (string.IsNullOrWhiteSpace(SlotName))
                throw new InvalidDataException("The seed manifest has no slot_name.");

            UnmappedLocations ??= new List<string>();
            if (MappingComplete == false || UnmappedLocations.Count > 0)
                throw new InvalidDataException(
                    $"The seed has {UnmappedLocations.Count} unmapped location(s) and is not supported by this build.");

            Locations ??= new List<ArchipelagoSeedLocation>();
            foreach (var location in Locations)
            {
                if (location.LocationId <= 0)
                    throw new InvalidDataException($"Location '{location.LocationName}' has an invalid id.");
                if (string.IsNullOrWhiteSpace(location.LocationName))
                    throw new InvalidDataException($"Location id {location.LocationId} has no name.");
                if (string.IsNullOrWhiteSpace(location.ItemName))
                    throw new InvalidDataException($"Location '{location.LocationName}' has no placed item name.");
            }

            var mappedLocations = Locations.Where(location => !string.IsNullOrWhiteSpace(location.GameKey)).ToList();
            var duplicateKey = mappedLocations
                .GroupBy(location => location.GameKey, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateKey != null)
                throw new InvalidDataException($"The seed maps game key '{duplicateKey.Key}' more than once.");

            var duplicateId = Locations
                .Where(location => location.LocationId > 0)
                .GroupBy(location => location.LocationId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateId != null)
                throw new InvalidDataException($"The seed contains location id {duplicateId.Key} more than once.");

            LocationsByGameKey = mappedLocations.ToDictionary(location => location.GameKey, StringComparer.Ordinal);
        }
    }

    public sealed class ArchipelagoSeedLocation
    {
        [JsonPropertyName("game_key")]
        public string GameKey { get; set; }

        [JsonPropertyName("location_id")]
        public long LocationId { get; set; }

        [JsonPropertyName("location_name")]
        public string LocationName { get; set; }

        [JsonPropertyName("item_name")]
        public string ItemName { get; set; }

        [JsonPropertyName("item_game")]
        public string ItemGame { get; set; }

        [JsonPropertyName("item_player")]
        public int ItemPlayer { get; set; }

        [JsonPropertyName("item_player_name")]
        public string ItemPlayerName { get; set; }

        [JsonPropertyName("local_player")]
        public int LocalPlayer { get; set; }

        [JsonPropertyName("classification")]
        public int Classification { get; set; }
    }
}
