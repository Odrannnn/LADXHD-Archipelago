using ProjectZ.InGame.Archipelago;
using ProjectZ.InGame.Assets;

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

Assert(ArchipelagoItemMapper.TryMap("Progressive Sword", 0, 0, 0, out var sword1) &&
       sword1.GameItemName == "sword1", "First progressive sword mapping failed.");
Assert(ArchipelagoItemMapper.TryMap("Progressive Sword", 1, 0, 0, out var sword2) &&
       sword2.GameItemName == "sword2", "Second progressive sword mapping failed.");
Assert(ArchipelagoItemMapper.TryMap("Small Key (Catfish's Maw)", 0, 0, 0, out var key) &&
       key.GameItemName == "smallkey" && key.LocationBounding == "five", "Dungeon key mapping failed.");
Assert(ArchipelagoItemMapper.TryMap("500 Rupees", 0, 0, 0, out var rupees) &&
       rupees.GameItemName == "ruby" && rupees.Count == 500, "Rupee mapping failed.");
Assert(!ArchipelagoItemMapper.TryMap("An Item From Another Game", 0, 0, 0, out _),
       "Unknown items must not silently map to a local item.");
Assert(ArchipelagoLocationKey.Script("marin:reward", 7) == "script:marin%3Areward:7",
       "Script location keys must be deterministic and escape separators.");
Assert(ArchipelagoLocationKey.Shop(980) == "shop:980", "Shop location key mapping failed.");
Assert(ArchipelagoLocationKey.Event("rooster:cave") == "event:rooster%3Acave",
       "Event location keys must be deterministic and escape separators.");
Assert(ArchipelagoLocationKey.PersistentCheck(1001) == "ap_location_1001",
       "Persistent check key mapping failed.");
Assert(ArchipelagoManager.ClientVersion == new Version(0, 6, 7),
       "The client handshake must advertise Archipelago 0.6.7 compatibility.");

var seedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.apladxhd");
try
{
    File.WriteAllText(seedPath, """
    {
      "format_version": 1,
      "game": "Links Awakening DX HD",
      "seed_name": "Smoke Test Seed",
      "slot_name": "Link",
      "world_version": "0.1.0",
      "mapping_complete": true,
      "unmapped_locations": [],
      "locations": [
        {
          "game_key": "item:hookshot_collected",
          "location_id": 1001,
          "location_name": "Master Stalfos Item (Catfish's Maw)",
          "item_name": "Hookshot",
          "item_game": "Links Awakening DX HD",
          "item_player": 1,
          "local_player": 1,
          "classification": 1
        }
      ],
      "options": {}
    }
    """);

    var seed = ArchipelagoSeedManifest.Load(seedPath);
    Assert(seed.LocationsByGameKey.ContainsKey("item:hookshot_collected"), "Seed lookup was not built.");
    Assert(seed.LocationsByGameKey["item:hookshot_collected"].LocationId == 1001,
           "Seed location id was not preserved.");
}
finally
{
    if (File.Exists(seedPath))
        File.Delete(seedPath);
}

var profileRoot = Path.Combine(Path.GetTempPath(), $"ladxhd-ap-profiles-{Guid.NewGuid():N}");
try
{
    var save1Directory = ArchipelagoConnectionSettings.GetProfileDirectory(profileRoot, 0);
    var save4Directory = ArchipelagoConnectionSettings.GetProfileDirectory(profileRoot, 3);
    Directory.CreateDirectory(save1Directory);
    Directory.CreateDirectory(save4Directory);

    File.WriteAllText(ArchipelagoConnectionSettings.GetProfilePath(profileRoot, 0), """
    {
      "enabled": true,
      "server": "seed-one.example:38281",
      "slot": "LinkOne",
      "seed_file": "seed.apladxhd",
      "save_slot": 0
    }
    """);
    File.WriteAllText(ArchipelagoConnectionSettings.GetProfilePath(profileRoot, 3), """
    {
      "enabled": true,
      "server": "seed-four.example:48281",
      "slot": "LinkFour",
      "seed_file": "four.apladxhd",
      "save_slot": 3
    }
    """);

    var save1 = ArchipelagoConnectionSettings.LoadProfile(profileRoot, 0);
    var save4 = ArchipelagoConnectionSettings.LoadProfile(profileRoot, 3);
    Assert(save1.Server == "seed-one.example:38281" && save1.SaveSlot == 0,
        "Save 1 profile did not load independently.");
    Assert(save4.Server == "seed-four.example:48281" && save4.SaveSlot == 3,
        "Save 4 profile did not load independently.");
    Assert(save1.ResolveProfileSeedPath(profileRoot, 0) ==
           Path.GetFullPath(Path.Combine(save1Directory, "seed.apladxhd")),
        "Save 1 relative seed path did not resolve inside its profile.");
    Assert(save4.ResolveProfileSeedPath(profileRoot, 3) ==
           Path.GetFullPath(Path.Combine(save4Directory, "four.apladxhd")),
        "Save 4 relative seed path did not resolve inside its profile.");

    File.WriteAllText(Path.Combine(save1Directory, "seed.apladxhd"), "seed one");
    File.WriteAllText(Path.Combine(save4Directory, "four.apladxhd"), "seed four");
    Assert(ArchipelagoConnectionSettings.DeleteProfile(profileRoot, 0),
        "Deleting Save 1's Archipelago profile failed.");
    Assert(!Directory.Exists(save1Directory),
        "Deleting Save 1 left its Archipelago profile data behind.");
    Assert(Directory.Exists(save4Directory),
        "Deleting Save 1 removed another save's Archipelago profile.");
    Assert(ArchipelagoConnectionSettings.DeleteProfile(profileRoot, 0),
        "Deleting an already absent Archipelago profile should be idempotent.");

    var rejectedInvalidSlot = false;
    try
    {
        ArchipelagoConnectionSettings.GetProfileDirectory(profileRoot, 4);
    }
    catch (ArgumentOutOfRangeException)
    {
        rejectedInvalidSlot = true;
    }
    Assert(rejectedInvalidSlot, "A fifth save profile must be rejected.");
}
finally
{
    if (Directory.Exists(profileRoot))
        Directory.Delete(profileRoot, true);
}

if (args.Length > 0)
{
    var generatedSeed = ArchipelagoSeedManifest.Load(args[0]);
    Assert(generatedSeed.Locations.Count > 200, "Generated APWorld manifest has too few locations.");
    Assert(generatedSeed.Game == ArchipelagoManager.GameName, "Generated APWorld manifest has the wrong game.");
}

var sourceArchive = Environment.GetEnvironmentVariable("LADXHD_V100_ZIP");
var bootstrapRoot = Environment.GetEnvironmentVariable("LADXHD_ANDROID_BOOTSTRAP");
if (!string.IsNullOrWhiteSpace(sourceArchive) && !string.IsNullOrWhiteSpace(bootstrapRoot))
{
    var migrationRoot = Path.Combine(Path.GetTempPath(), $"ladxhd-assets-{Guid.NewGuid():N}");
    try
    {
        var result = GameAssetMigrator.Migrate(sourceArchive, migrationRoot, bootstrapRoot);
        Assert(result.SourceArchiveSha256 == GameAssetMigrator.ExpectedSourceArchiveSha256,
            "Asset migration accepted the wrong source archive.");
        Assert(result.FileCount > 500, "Asset migration generated too few files.");
        Console.WriteLine($"Migrated assets: sha256={result.TreeSha256} files={result.FileCount} bytes={result.TotalBytes}");
    }
    finally
    {
        if (Directory.Exists(migrationRoot))
            Directory.Delete(migrationRoot, recursive: true);
    }
}

Console.WriteLine("Archipelago smoke tests passed.");
