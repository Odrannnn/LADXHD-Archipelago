using ProjectZ.InGame.Archipelago;
using ProjectZ.InGame.Assets;
using ProjectZ.InGame.Telemetry;
using ProjectZ.InGame.Things;
using System.Net;
using System.Net.Http.Json;

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
Assert(ArchipelagoItemMapper.TryMap("Rooster", 0, 0, 0, out var rooster) &&
       rooster.GameItemName == "rooster" && rooster.Effect == ArchipelagoItemEffect.Rooster,
       "Rooster mapping must apply its follower ownership state.");
Assert(ArchipelagoItemMapper.TryMap("Stick", 0, 0, 0, out var stick) &&
       stick.GameItemName == "trade4" && stick.Effect == ArchipelagoItemEffect.TradeStick,
       "Stick mapping must spawn Tarin at the honeycomb tree.");
Assert(ArchipelagoItemMapper.TryMap("Pineapple", 0, 0, 0, out var pineapple) &&
       pineapple.GameItemName == "trade6" && pineapple.Effect == ArchipelagoItemEffect.TradePineapple,
       "Pineapple mapping must spawn Papahl in Tal Tal Heights.");
Assert(ArchipelagoItemMapper.TryMap("Scale", 0, 0, 0, out var scale) &&
       scale.GameItemName == "trade12" && scale.Effect == ArchipelagoItemEffect.TradeScale,
       "Scale mapping must complete the mermaid departure state.");
Assert(ArchipelagoItemMapper.TryMap("Magnifying Glass", 0, 0, 0, out var lens) &&
       lens.GameItemName == "trade13" && lens.Effect == ArchipelagoItemEffect.TradeMagnifyingGlass,
       "Magnifying Glass mapping must complete the photographer state.");
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
Assert(ArchipelagoManager.HasSaveBinding("Seed", "Link") &&
       !ArchipelagoManager.HasSaveBinding("", "Link") &&
       !ArchipelagoManager.HasSaveBinding("Seed", null),
       "Offline gameplay overrides must follow the persistent AP save binding.");
Assert(ArchipelagoManager.ShouldEnableMoblinCave(true, "0") &&
       !ArchipelagoManager.ShouldEnableMoblinCave(true, "1") &&
       !ArchipelagoManager.ShouldEnableMoblinCave(false, "0"),
       "AP must enable the Moblin Cave encounter before Tail Cave without respawning its boss.");
Assert(ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument6", "rooster") &&
       ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument6", "has_rooster") &&
       !ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(false, "instrument6", "rooster") &&
       !ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument5", "rooster"),
       "Dungeon 7 must not remove an AP-delivered rooster or its follower state.");
Assert(ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "npc09", "bowWow") &&
       ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "npc09", "has_bowWow") &&
       ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "castle_monkey", "has_bowWow") &&
       ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "npc_frog_boy", "has_bowWow") &&
       !ArchipelagoManager.ShouldIgnoreBowWowForDialog(false, "npc09", "bowWow") &&
       !ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "photo_mouse_house", "has_bowWow"),
       "AP BowWow must not be returned or block the Kiki and Richard sequences.");
Assert(ArchipelagoManager.ShouldAllowSecretBookWithoutLens(true, "book8", "trade13") &&
       !ArchipelagoManager.ShouldAllowSecretBookWithoutLens(false, "book8", "trade13") &&
       !ArchipelagoManager.ShouldAllowSecretBookWithoutLens(true, "book7", "trade13"),
       "The AP egg-maze book must not require the trade quest's Magnifying Glass.");
Assert(ArchipelagoManager.ShouldSuppressBombDrop(true, false, "bomb_1") &&
       !ArchipelagoManager.ShouldSuppressBombDrop(true, true, "bomb_1") &&
       !ArchipelagoManager.ShouldSuppressBombDrop(false, false, "bomb_1") &&
       !ArchipelagoManager.ShouldSuppressBombDrop(true, false, "heart"),
       "Enemy drops must not grant Bombs before AP delivers the Bomb item.");
Assert(GameManager.EquipmentSlots == 16,
       "The expanded inventory must retain every independently randomized equipment item.");
Assert(ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "0", "0") &&
       ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "4", "0") &&
       ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "5", "0"),
       "An active AP save must spawn Raccoon Tarin before his cure, including out-of-order trade states.");
Assert(!ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           false, "tarin_state", "1", "raccoon", "0", "0") &&
       !ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "0", "1") &&
       !ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "2", "0"),
       "The Raccoon Tarin override must not affect vanilla or cured states.");

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

var telemetryRoot = Path.Combine(Path.GetTempPath(), $"ladxhd-telemetry-{Guid.NewGuid():N}");
try
{
    var handler = new CapturingHandler();
    using var telemetry = new TelemetryClient(new TelemetryClientOptions
    {
        Endpoint = new Uri("https://telemetry.example/v1/events"),
        StorageRoot = telemetryRoot,
        AppVersion = "2.0.7-ap1",
        Platform = "android",
        DiagnosticsEnabled = true,
        RandomizerEnabled = true,
        HttpClient = new HttpClient(handler),
        FlushInterval = TimeSpan.FromHours(1),
    });

    Exception diagnosticException;
    try
    {
        _ = new TelemetryClient(null);
        throw new InvalidOperationException("TelemetryClient unexpectedly accepted null options.");
    }
    catch (ArgumentNullException exception)
    {
        diagnosticException = exception;
    }
    telemetry.RecordCrash(diagnosticException, TelemetryGameState.Gameplay, fatal: true);
    telemetry.RecordConnectFailure(2, 3500, TelemetryConnectionError.Network);
    Assert(telemetry.PendingCount == 2 && telemetry.HasPendingCrash,
        "Telemetry events were not durably queued.");

    await telemetry.FlushAsync();
    Assert(handler.Body != null, "Telemetry flush did not send a request.");
    Assert(!handler.Body.Contains("options", StringComparison.OrdinalIgnoreCase) &&
           !handler.Body.Contains("Program.cs", StringComparison.OrdinalIgnoreCase),
        "Crash telemetry leaked exception messages, argument names, or paths.");
    Assert(handler.Body.Contains("stack_hash", StringComparison.Ordinal) &&
           handler.Body.Contains("build_id", StringComparison.Ordinal) &&
           handler.Body.Contains("frames", StringComparison.Ordinal) &&
           handler.Body.Contains("ProjectZ.Core", StringComparison.Ordinal) &&
           handler.Body.Contains("System.ArgumentNullException", StringComparison.Ordinal),
        "Sanitized crash diagnostics were not uploaded.");
    Assert(telemetry.PendingCount == 0, "Accepted telemetry was not removed from the queue.");

    telemetry.RecordRandomizerManifest("private-seed-name", "normal", false, null, null);
    await telemetry.FlushAsync();
    Assert(handler.Body.Contains("randomizer_manifest", StringComparison.Ordinal) &&
           !handler.Body.Contains("private-seed-name", StringComparison.Ordinal),
        "Manifest telemetry leaked a non-version seed value.");

    telemetry.SetConsent(diagnosticsEnabled: false, randomizerEnabled: true);
    telemetry.RecordCrash(new Exception("must remain local"), TelemetryGameState.Unknown, fatal: false);
    telemetry.RecordConnectAttempt(1);
    Assert(telemetry.PendingCount == 1, "Disabled diagnostic telemetry was queued.");
    telemetry.SetConsent(diagnosticsEnabled: false, randomizerEnabled: false);
    Assert(telemetry.PendingCount == 0, "Consent withdrawal did not purge the local queue.");
}
finally
{
    if (Directory.Exists(telemetryRoot))
        Directory.Delete(telemetryRoot, true);
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

sealed class CapturingHandler : HttpMessageHandler
{
    public string Body { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post)
            throw new InvalidOperationException("Telemetry must use POST.");
        if (request.RequestUri != new Uri("https://telemetry.example/v1/events"))
            throw new InvalidOperationException("Telemetry used an unexpected endpoint.");
        Body = await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = JsonContent.Create(new { accepted = 2 }),
        };
    }
}
