using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectZ.InGame.Archipelago
{
    public sealed class ArchipelagoProfileSummary
    {
        public int SaveSlot { get; init; }
        public string SeedName { get; init; }
        public string SlotName { get; init; }
        public string Server { get; init; }
    }

    public static class ArchipelagoProfileCatalog
    {
        public static IReadOnlyList<ArchipelagoProfileSummary> LoadInstalled(string userDataRoot)
        {
            var profiles = new List<ArchipelagoProfileSummary>();
            if (string.IsNullOrWhiteSpace(userDataRoot))
                return profiles;

            for (var saveSlot = 0; saveSlot < ArchipelagoConnectionSettings.ProfileCount; saveSlot++)
            {
                try
                {
                    var settings = ArchipelagoConnectionSettings.LoadProfile(userDataRoot, saveSlot);
                    if (settings?.Enabled != true ||
                        settings.SaveSlot.HasValue && settings.SaveSlot.Value != saveSlot)
                        continue;

                    var seed = ArchipelagoSeedManifest.Load(
                        settings.ResolveProfileSeedPath(userDataRoot, saveSlot));
                    if (!string.Equals(settings.Slot, seed.SlotName, StringComparison.Ordinal))
                        continue;

                    profiles.Add(new ArchipelagoProfileSummary
                    {
                        SaveSlot = saveSlot,
                        SeedName = seed.SeedName,
                        SlotName = seed.SlotName,
                        Server = settings.Server
                    });
                }
                catch (InvalidDataException)
                {
                    // Seed and connection validation failures are repaired by a full reimport.
                }
                catch (IOException)
                {
                    // A damaged or incomplete profile is not safe to edit without reimporting.
                }
                catch (UnauthorizedAccessException)
                {
                    // Treat inaccessible profiles like absent profiles in the setup chooser.
                }
                catch (System.Text.Json.JsonException)
                {
                    // Malformed connection JSON is repaired by a full reimport.
                }
                catch (ArgumentException)
                {
                    // Invalid path or setting values are repaired by a full reimport.
                }
                catch (NotSupportedException)
                {
                    // Unsupported path formats are repaired by a full reimport.
                }
            }

            return profiles;
        }
    }
}
