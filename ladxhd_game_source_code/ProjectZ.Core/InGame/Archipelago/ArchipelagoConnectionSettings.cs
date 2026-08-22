using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectZ.InGame.Archipelago
{
    public sealed class ArchipelagoConnectionSettings
    {
        public const string DirectoryName = "Archipelago";
        public const string ProfilesDirectoryName = "Profiles";
        public const string FileName = "connection.json";
        public const string DefaultSeedFileName = "seed.apladxhd";
        public const int ProfileCount = 4;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("server")]
        public string Server { get; set; } = "localhost:38281";

        [JsonPropertyName("slot")]
        public string Slot { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("seed_file")]
        public string SeedFile { get; set; } = DefaultSeedFileName;

        [JsonPropertyName("save_slot")]
        public int? SaveSlot { get; set; }

        [JsonPropertyName("auto_connect")]
        public bool AutoConnect { get; set; } = true;

        public static string GetDirectory(string userDataRoot) => Path.Combine(userDataRoot, DirectoryName);

        public static string GetPath(string userDataRoot) => Path.Combine(GetDirectory(userDataRoot), FileName);

        public static string GetProfilesDirectory(string userDataRoot) =>
            Path.Combine(GetDirectory(userDataRoot), ProfilesDirectoryName);

        public static string GetProfileDirectory(string userDataRoot, int saveSlot)
        {
            ValidateSaveSlot(saveSlot);
            return Path.Combine(GetProfilesDirectory(userDataRoot), $"Save{saveSlot + 1}");
        }

        public static string GetProfilePath(string userDataRoot, int saveSlot) =>
            Path.Combine(GetProfileDirectory(userDataRoot, saveSlot), FileName);

        public static string GetProfileSeedPath(string userDataRoot, int saveSlot) =>
            Path.Combine(GetProfileDirectory(userDataRoot, saveSlot), DefaultSeedFileName);

        public static ArchipelagoConnectionSettings Load(string userDataRoot)
        {
            return LoadPath(GetPath(userDataRoot));
        }

        public static ArchipelagoConnectionSettings LoadProfile(string userDataRoot, int saveSlot)
        {
            return LoadPath(GetProfilePath(userDataRoot, saveSlot));
        }

        public static bool DeleteProfile(string userDataRoot, int saveSlot)
        {
            try
            {
                ValidateSaveSlot(saveSlot);

                var profilesRoot = Path.GetFullPath(GetProfilesDirectory(userDataRoot))
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var profileDirectory = Path.GetFullPath(GetProfileDirectory(userDataRoot, saveSlot));
                if (!profileDirectory.StartsWith(profilesRoot, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (Directory.Exists(profileDirectory))
                    Directory.Delete(profileDirectory, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ArchipelagoConnectionSettings LoadPath(string path)
        {
            if (!File.Exists(path))
                return null;

            var settings = JsonSerializer.Deserialize<ArchipelagoConnectionSettings>(File.ReadAllText(path), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (settings == null)
                throw new InvalidDataException($"'{path}' is empty.");
            if (settings.Enabled && string.IsNullOrWhiteSpace(settings.Server))
                throw new InvalidDataException("Archipelago server is required when the integration is enabled.");
            if (settings.Enabled && string.IsNullOrWhiteSpace(settings.Slot))
                throw new InvalidDataException("Archipelago slot is required when the integration is enabled.");
            if (settings.SaveSlot is < 0 or >= ProfileCount)
                throw new InvalidDataException($"Archipelago save_slot must be between 0 and {ProfileCount - 1}.");

            return settings;
        }

        public string ResolveSeedPath(string userDataRoot)
        {
            return ResolveSeedPathFromDirectory(GetDirectory(userDataRoot));
        }

        public string ResolveProfileSeedPath(string userDataRoot, int saveSlot)
        {
            return ResolveSeedPathFromDirectory(GetProfileDirectory(userDataRoot, saveSlot));
        }

        private string ResolveSeedPathFromDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(SeedFile))
                throw new InvalidDataException("Archipelago seed_file is required.");

            return Path.GetFullPath(Path.IsPathRooted(SeedFile)
                ? SeedFile
                : Path.Combine(directory, SeedFile));
        }

        private static void ValidateSaveSlot(int saveSlot)
        {
            if (saveSlot is < 0 or >= ProfileCount)
                throw new ArgumentOutOfRangeException(nameof(saveSlot), saveSlot,
                    $"Save slot must be between 0 and {ProfileCount - 1}.");
        }
    }
}
