using ProjectZ.InGame.Archipelago;
using ProjectZ.InGame.Assets;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Overlay;
using ProjectZ.InGame.Telemetry;
using ProjectZ.InGame.Things;
using Archipelago.MultiClient.Net.Helpers;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task SendWebSocketText(ClientWebSocket socket, string text)
{
    var bytes = Encoding.UTF8.GetBytes(text);
    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
        CancellationToken.None);
}

static async Task<string> ReceiveWebSocketText(ClientWebSocket socket)
{
    var buffer = new byte[4096];
    using var stream = new MemoryStream();
    while (true)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(2));
        Assert(result.MessageType == WebSocketMessageType.Text,
            "Magpie bridge returned a non-text WebSocket message.");
        stream.Write(buffer, 0, result.Count);
        if (result.EndOfMessage)
            return Encoding.UTF8.GetString(stream.ToArray());
    }
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
Assert(ArchipelagoItemMapper.TryMap("Zol Attack", 0, 0, 0, out var zolAttack) &&
       zolAttack.Effect == ArchipelagoItemEffect.ZolAttack &&
       ArchipelagoManager.ZolAttackSpawnCount == 5,
       "Zol Attack must spawn the five enemies used by the official AP trap.");
Assert(ArchipelagoItemMapper.TryMap("Guardian Acorn", 0, 0, 0, out var guardianAcorn) &&
       guardianAcorn.Effect == ArchipelagoItemEffect.GuardianAcorn &&
       ArchipelagoItemMapper.TryMap("Piece Of Power", 0, 0, 0, out var pieceOfPower) &&
       pieceOfPower.Effect == ArchipelagoItemEffect.PieceOfPower,
       "Remote temporary powerups must activate their normal gameplay effects.");
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
Assert(MagpieTrackerProtocol.GetCheckId(new ArchipelagoSeedLocation
       {
           LocationId = 10000673,
           LocationName = "Shop 200 Item (Mabe Village)"
       }) == "0x2A1-0" &&
       MagpieTrackerProtocol.GetCheckId(new ArchipelagoSeedLocation
       {
           LocationId = 10001259,
           LocationName = "Spiked Beetle Owl (Tail Cave)"
       }) == "0x103-Owl" &&
       MagpieTrackerProtocol.GetCheckId(new ArchipelagoSeedLocation
       {
           LocationId = 10001672,
           LocationName = "Trendy Game (Mabe Village)"
       }) == "0x2A0-Trade",
       "Magpie check IDs must reverse AP's numeric encoding and preserve LADXR suffixes.");
Assert(MagpieTrackerProtocol.TryGetItemContribution("Progressive Sword", out var magpieSword) &&
       magpieSword.Id == "SWORD" && magpieSword.Quantity == 1 && magpieSword.Maximum == 2 &&
       MagpieTrackerProtocol.TryGetItemContribution("Small Key (Color Dungeon)", out var magpieKey) &&
       magpieKey.Id == "KEY9" && magpieKey.Maximum == int.MaxValue &&
       !MagpieTrackerProtocol.TryGetItemContribution("Zol Attack", out _),
       "Magpie inventory mapping must count progressive and dungeon items without tracking traps.");
var embeddedTrackerUri = MagpieTrackerProtocol.CreateEmbeddedTrackerUri();
Assert(embeddedTrackerUri.Scheme == Uri.UriSchemeHttps &&
       embeddedTrackerUri.Host == "magpietracker.us" &&
       embeddedTrackerUri.Query.Contains("enable_autotracking=true", StringComparison.Ordinal) &&
       embeddedTrackerUri.Query.Contains("setting_autotrackerAddress=127.0.0.1%3A17026", StringComparison.Ordinal) &&
       embeddedTrackerUri.Query.Contains("setting_autotrackSettings=true", StringComparison.Ordinal) &&
       embeddedTrackerUri.Query.Contains("flag_ap_logic=true", StringComparison.Ordinal),
       "Embedded Magpie URL must enable AP autotracking against the local bridge.");
Assert(!new ProjectZ.UnavailableMagpieTrackerService().IsAvailable,
       "Non-Android platforms must not expose the embedded tracker pause command.");
using (var magpieHandshake = System.Text.Json.JsonDocument.Parse(
           MagpieTrackerProtocol.CreateHandshakeAcknowledgement()))
{
    Assert(magpieHandshake.RootElement.GetProperty("type").GetString() == "handshAck" &&
           magpieHandshake.RootElement.GetProperty("version").GetString() == "1.32",
           "Magpie handshake acknowledgement must use the third-party API protocol.");
}
var magpieLocation = new ArchipelagoSeedLocation
{
    LocationId = 10000673,
    LocationName = "Shop 200 Item (Mabe Village)",
    ItemName = "Boomerang"
};
var magpieSeed = new ArchipelagoSeedManifest
{
    FormatVersion = ArchipelagoSeedManifest.CurrentFormatVersion,
    Game = ArchipelagoManager.GameName,
    SeedName = "Magpie Smoke Seed",
    SlotName = "Link",
    WorldVersion = "0.1.0",
    MappingComplete = true,
    Locations = new List<ArchipelagoSeedLocation> { magpieLocation }
};
magpieSeed.Validate();
using (var magpieBridge = new MagpieTrackerBridge(0))
{
    magpieBridge.Configure(enabled: false, allowLan: false, seed: magpieSeed);
    Assert(magpieBridge.BoundPort == 0,
        "A disabled Magpie profile must not start the listener before the tracker is opened.");
    Assert(magpieBridge.Start(allowLan: false),
        "Opening the embedded tracker must be able to start its local bridge on demand.");
    Assert(magpieBridge.BoundPort > 0, "Magpie bridge did not bind its loopback listener.");
    using var magpieSocket = new ClientWebSocket();
    await magpieSocket.ConnectAsync(
        new Uri($"ws://127.0.0.1:{magpieBridge.BoundPort}/"), CancellationToken.None);
    await SendWebSocketText(magpieSocket, "{\"type\":\"handshake\",\"features\":[\"items\",\"checks\"]}");
    using (var acknowledgement = System.Text.Json.JsonDocument.Parse(
               await ReceiveWebSocketText(magpieSocket)))
        Assert(acknowledgement.RootElement.GetProperty("type").GetString() == "handshAck",
            "Magpie bridge did not acknowledge a live WebSocket handshake.");
    using (var slotData = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(slotData.RootElement.GetProperty("slot_data").GetProperty("seed_name").GetString() ==
               "Magpie Smoke Seed", "Magpie bridge did not send sanitized seed slot data.");

    await SendWebSocketText(magpieSocket, "{\"type\":\"sendFull\"}");
    using (var fullItems = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(!fullItems.RootElement.GetProperty("diff").GetBoolean(),
            "Magpie full inventory response was incorrectly marked as a diff.");
    using (var fullChecks = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(!fullChecks.RootElement.GetProperty("diff").GetBoolean(),
            "Magpie full check response was incorrectly marked as a diff.");

    magpieBridge.RecordReceivedItem(0, "Boomerang");
    using (var itemDiff = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(itemDiff.RootElement.GetProperty("items")[0].GetProperty("id").GetString() == "BOOMERANG" &&
               itemDiff.RootElement.GetProperty("items")[0].GetProperty("qty").GetInt32() == 1,
            "Magpie bridge did not stream an AP item receipt as a differential update.");

    magpieBridge.RecordCheck(magpieLocation);
    using (var checkDiff = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(checkDiff.RootElement.GetProperty("checks")[0].GetProperty("id").GetString() == "0x2A1-0" &&
               checkDiff.RootElement.GetProperty("checks")[0].GetProperty("checked").GetBoolean(),
            "Magpie bridge did not stream a completed location as a differential update.");
}
Assert(ArchipelagoManager.ClientVersion == new Version(0, 6, 7),
       "The client handshake must advertise Archipelago 0.6.7 compatibility.");
Assert(ArchipelagoManager.GetReconnectDelaySeconds(0) == 0 &&
       ArchipelagoManager.GetReconnectDelaySeconds(1) == 5 &&
       ArchipelagoManager.GetReconnectDelaySeconds(2) == 10 &&
       ArchipelagoManager.GetReconnectDelaySeconds(3) == 20 &&
       ArchipelagoManager.GetReconnectDelaySeconds(5) == 60 &&
       ArchipelagoManager.GetReconnectDelaySeconds(20) == 60,
       "Reconnect delays must back off from five seconds and remain capped at one minute.");
Assert(ArchipelagoManager.ClassifySocketFailure(new WebSocketException()) ==
           TelemetryDisconnectReason.Network &&
       ArchipelagoManager.ClassifySocketFailure(new System.Text.Json.JsonException()) ==
           TelemetryDisconnectReason.Protocol &&
       ArchipelagoManager.ClassifySocketFailure(new InvalidOperationException()) ==
           TelemetryDisconnectReason.Unknown,
       "Socket telemetry must distinguish transport, protocol, and unknown failures.");

var fakeSocket = new BlockingWebSocket();
var socketHelper = new BaseArchipelagoSocketHelper<BlockingWebSocket>(fakeSocket);
socketHelper.StartPolling();
await fakeSocket.ReceiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
var socketCleanupStarted = DateTime.UtcNow;
await socketHelper.DisconnectAsync();
Assert(fakeSocket.Aborted && fakeSocket.Disposed &&
       DateTime.UtcNow - socketCleanupStarted < TimeSpan.FromSeconds(1),
       "Disconnect must cancel idle socket workers and dispose the old WebSocket promptly.");
Assert(ArchipelagoManager.HasSaveBinding("Seed", "Link") &&
       !ArchipelagoManager.HasSaveBinding("", "Link") &&
       !ArchipelagoManager.HasSaveBinding("Seed", null),
       "Offline gameplay overrides must follow the persistent AP save binding.");
Assert(ArchipelagoManager.ShouldUseBoomerangGiftBehavior(true) &&
       !ArchipelagoManager.ShouldUseBoomerangGiftBehavior(false),
       "Boomerang Guy must use gift behavior only for an AP-bound save.");
Assert(!ArchipelagoManager.ShouldReplaceToadstoolWithPowder(true) &&
       ArchipelagoManager.ShouldReplaceToadstoolWithPowder(false),
       "An independent AP Magic Powder receipt must preserve the Toadstool for the Witch check.");
Assert(ArchipelagoManager.ShouldRepairToadstoolReceipt(false, false) &&
       !ArchipelagoManager.ShouldRepairToadstoolReceipt(false, true) &&
       !ArchipelagoManager.ShouldRepairToadstoolReceipt(true, false),
       "A replayed Toadstool must be restored only while the Witch check is still pending.");
Assert(ArchipelagoManager.ShouldDismissMarinFollower(true, false, "3") &&
       !ArchipelagoManager.ShouldDismissMarinFollower(false, false, "3") &&
       !ArchipelagoManager.ShouldDismissMarinFollower(true, true, "3") &&
       !ArchipelagoManager.ShouldDismissMarinFollower(true, false, "8"),
       "The removed-Walrus repair must dismiss only the completed AP beach escort.");
Assert(ArchipelagoManager.ShouldRepairBoomerangReceipt("0", "0", false) &&
       ArchipelagoManager.ShouldRepairBoomerangReceipt("1", "0", true) &&
       ArchipelagoManager.ShouldRepairBoomerangReceipt("1", "1", false) &&
       !ArchipelagoManager.ShouldRepairBoomerangReceipt("1", "1", true),
       "AP replay must recover a received boomerang missing from save state or inventory.");
Assert(ArchipelagoManager.ShouldRestoreBoomerangTradeItem("shovel", false) &&
       ArchipelagoManager.ShouldRestoreBoomerangTradeItem("hookshot", false) &&
       !ArchipelagoManager.ShouldRestoreBoomerangTradeItem("hookshot", true) &&
       !ArchipelagoManager.ShouldRestoreBoomerangTradeItem("sword1", false),
       "Old AP saves must restore only equipment removed by the vanilla boomerang trade.");
Assert(ArchipelagoManager.IsTrendyGamePrize("trade0Collected") &&
       !ArchipelagoManager.IsTrendyGamePrize("pieceOfHeartCollected"),
       "The randomized Trendy prize must be recognized by its stable source key.");
Assert(ArchipelagoManager.ShouldRepairTrendyPrize(true, "1", "0") &&
       !ArchipelagoManager.ShouldRepairTrendyPrize(true, "1", "1") &&
       !ArchipelagoManager.ShouldRepairTrendyPrize(true, "0", "0") &&
       !ArchipelagoManager.ShouldRepairTrendyPrize(false, "1", "0"),
       "Only an AP Trendy prize hidden before its persistent check should be respawned.");
Assert(ArchipelagoManager.ShouldEnableMoblinCave(true, "0") &&
       !ArchipelagoManager.ShouldEnableMoblinCave(true, "1") &&
       !ArchipelagoManager.ShouldEnableMoblinCave(false, "0"),
       "AP must enable the Moblin Cave encounter before Tail Cave without respawning its boss.");
Assert(ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument6", "rooster") &&
       ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument6", "has_rooster") &&
       !ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(false, "instrument6", "rooster") &&
       !ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument5", "rooster"),
       "Dungeon 7 must not remove an AP-delivered rooster or its follower state.");
Assert(ArchipelagoManager.ShouldSuppressGhostAfterDungeonFour(true, "instrument3", "spawn_ghost") &&
       !ArchipelagoManager.ShouldSuppressGhostAfterDungeonFour(false, "instrument3", "spawn_ghost") &&
       !ArchipelagoManager.ShouldSuppressGhostAfterDungeonFour(true, "instrument4", "spawn_ghost"),
       "The randomized Dungeon 4 reward must not start the vanilla ghost follower quest.");
Assert(ArchipelagoManager.ShouldRepairGhostFollowerState("1", "0", false) &&
       ArchipelagoManager.ShouldRepairGhostFollowerState("0", "1", false) &&
       ArchipelagoManager.ShouldRepairGhostFollowerState("0", "0", true) &&
       !ArchipelagoManager.ShouldRepairGhostFollowerState("0", "0", false),
       "Older AP saves must discard a ghost follower spawned before the randomizer fix.");
var owlType = typeof(GameManager).Assembly.GetType("ProjectZ.InGame.GameObjects.NPCs.ObjOwl");
var owlInventoryPolicy = owlType?.GetMethod(
    "ShouldDisableInventory", BindingFlags.Static | BindingFlags.NonPublic);
Assert(owlInventoryPolicy != null &&
       (bool)owlInventoryPolicy.Invoke(null, new object[] { "enter" }) &&
       (bool)owlInventoryPolicy.Invoke(null, new object[] { "talk" }) &&
       !(bool)owlInventoryPolicy.Invoke(null, new object[] { "leave" }) &&
       !(bool)owlInventoryPolicy.Invoke(null, new object[] { "wait" }),
       "Owl encounters must use a transient inventory lock only while entering and talking.");
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
Assert(ArchipelagoManager.ReconcileProgressiveCount(1, 0, 0) == 1 &&
       ArchipelagoManager.ReconcileProgressiveCount(0, 2, 0) == 2 &&
       ArchipelagoManager.ReconcileProgressiveCount(0, 0, 1) == 1,
       "Progressive tiers must follow monotonic AP receipt history, not removable inventory alone.");
Assert(ArchipelagoManager.GetUpgradeAmmoCount(ArchipelagoItemEffect.MaxPowderUpgrade) == 40 &&
       ArchipelagoManager.GetUpgradeAmmoCount(ArchipelagoItemEffect.MaxBombsUpgrade) == 60 &&
       ArchipelagoManager.GetUpgradeAmmoCount(ArchipelagoItemEffect.MaxArrowsUpgrade) == 60,
       "Capacity upgrades must refill to the official AP powder, bomb, and arrow limits.");
Assert(ArchipelagoItemMapper.TryMap("Max Powder Upgrade", 0, 0, 0, out var maxPowder) &&
       maxPowder.Effect == ArchipelagoItemEffect.MaxPowderUpgrade &&
       ArchipelagoItemMapper.TryMap("Max Bombs Upgrade", 0, 0, 0, out var maxBombs) &&
       maxBombs.Effect == ArchipelagoItemEffect.MaxBombsUpgrade &&
       ArchipelagoItemMapper.TryMap("Max Arrows Upgrade", 0, 0, 0, out var maxArrows) &&
       maxArrows.Effect == ArchipelagoItemEffect.MaxArrowsUpgrade,
       "Capacity upgrade replay must retain distinct refill effects.");
Assert(ArchipelagoManager.IsSeashellMansionComplete(true, "0", "1") &&
       !ArchipelagoManager.IsSeashellMansionComplete(true, "1", "0") &&
       ArchipelagoManager.IsSeashellMansionComplete(false, "1", "0"),
       "The AP Seashell Mansion sequence must follow its checked source location.");
Assert(ArchipelagoManager.ShouldSetLevelTwoSwordFlag(2, "0") &&
       !ArchipelagoManager.ShouldSetLevelTwoSwordFlag(1, "0") &&
       !ArchipelagoManager.ShouldSetLevelTwoSwordFlag(2, "1"),
       "A remotely received level-two sword must retain its native ownership state.");
Assert(ArchipelagoManager.ShouldRepairRoosterReceipt("0", "0", false) &&
       ArchipelagoManager.ShouldRepairRoosterReceipt("1", "0", true) &&
       ArchipelagoManager.ShouldRepairRoosterReceipt("1", "1", false) &&
       !ArchipelagoManager.ShouldRepairRoosterReceipt("1", "1", true),
       "Replayed AP history must restore a rooster lost by an older save.");
Assert(GameManager.EquipmentSlots == 16,
       "The expanded inventory must retain every independently randomized equipment item.");
Assert(CheatSystem.IsIndependentGiveAllItem("boomerang") &&
       !CheatSystem.IsIndependentGiveAllItem("rooster"),
       "Give All Items must include the boomerang without treating follower items as equipment.");
const int inventoryMapX = 118;
const int inventoryMapY = 58;
const int inventoryMapWidth = 144;
const int inventoryMapHeight = 144;
static bool RectanglesIntersect(
    int firstX, int firstY, int firstWidth, int firstHeight,
    int secondX, int secondY, int secondWidth, int secondHeight) =>
    firstX < secondX + secondWidth && firstX + firstWidth > secondX &&
    firstY < secondY + secondHeight && firstY + firstHeight > secondY;
foreach (var sixButtons in new[] { false, true })
{
    var layout = InventoryOverlayLayout.GetEquipmentLayout(sixButtons, GameManager.EquipmentSlots);
    var storageSlots = GameManager.EquipmentSlots - (sixButtons ? 6 : 4);

    Assert(layout.Columns * layout.Rows >= storageSlots,
           "The compact inventory layout must retain every expanded storage slot.");
    Assert(layout.CellWidth >= 16 && layout.CellHeight >= 16,
           "Expanded inventory cells must remain large enough for item sprites.");
    Assert(!RectanglesIntersect(
               layout.X,
               layout.Y + InventoryOverlayLayout.ContentOffsetY,
               layout.Width,
               layout.Height,
               inventoryMapX,
               inventoryMapY,
               inventoryMapWidth,
               inventoryMapHeight),
           "Expanded inventory storage must not be covered by the minimap.");
}
Assert(!RectanglesIntersect(
           InventoryOverlayLayout.RoosterX,
           InventoryOverlayLayout.RoosterY,
           InventoryOverlayLayout.RoosterWidth,
           InventoryOverlayLayout.RoosterHeight,
           inventoryMapX,
           inventoryMapY,
           inventoryMapWidth,
           inventoryMapHeight),
       "The Rooster ownership indicator must remain visible outside the minimap.");
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
      "save_slot": 3,
      "magpie_tracker_enabled": true,
      "magpie_tracker_allow_lan": true
    }
    """);

    var save1 = ArchipelagoConnectionSettings.LoadProfile(profileRoot, 0);
    var save4 = ArchipelagoConnectionSettings.LoadProfile(profileRoot, 3);
    Assert(save1.Server == "seed-one.example:38281" && save1.SaveSlot == 0,
        "Save 1 profile did not load independently.");
    Assert(save4.Server == "seed-four.example:48281" && save4.SaveSlot == 3,
        "Save 4 profile did not load independently.");
    Assert(!save1.MagpieTrackerEnabled && !save1.MagpieTrackerAllowLan &&
           save4.MagpieTrackerEnabled && save4.MagpieTrackerAllowLan,
        "Magpie settings must remain profile-specific and default to disabled.");
    Assert(save1.ResolveProfileSeedPath(profileRoot, 0) ==
           Path.GetFullPath(Path.Combine(save1Directory, "seed.apladxhd")),
        "Save 1 relative seed path did not resolve inside its profile.");
    Assert(save4.ResolveProfileSeedPath(profileRoot, 3) ==
           Path.GetFullPath(Path.Combine(save4Directory, "four.apladxhd")),
        "Save 4 relative seed path did not resolve inside its profile.");

    File.WriteAllText(Path.Combine(save1Directory, "seed.apladxhd"), "seed one");
    File.WriteAllText(Path.Combine(save4Directory, "four.apladxhd"), "seed four");
    Assert(ArchipelagoProfileCatalog.LoadInstalled(profileRoot).Count == 0,
        "The manual setup catalog must hide profiles whose seed identity cannot be validated.");
    File.WriteAllText(Path.Combine(save4Directory, "four.apladxhd"), """
    {
      "format_version": 1,
      "game": "Links Awakening DX HD",
      "seed_name": "Catalog Seed",
      "slot_name": "LinkFour",
      "world_version": "0.1.0",
      "mapping_complete": true,
      "unmapped_locations": [],
      "locations": [],
      "options": {}
    }
    """);
    var installedProfiles = ArchipelagoProfileCatalog.LoadInstalled(profileRoot);
    Assert(installedProfiles.Count == 1 &&
           installedProfiles[0].SaveSlot == 3 &&
           installedProfiles[0].SeedName == "Catalog Seed" &&
           installedProfiles[0].SlotName == "LinkFour" &&
           installedProfiles[0].Server == "seed-four.example:48281",
        "The manual setup catalog did not expose the verified installed profile.");
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

sealed class BlockingWebSocket : WebSocket
{
    private WebSocketState _state = WebSocketState.Open;

    public TaskCompletionSource<bool> ReceiveStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Aborted { get; private set; }
    public bool Disposed { get; private set; }
    public override WebSocketCloseStatus? CloseStatus => null;
    public override string CloseStatusDescription => null;
    public override WebSocketState State => _state;
    public override string SubProtocol => null;

    public override void Abort()
    {
        Aborted = true;
        _state = WebSocketState.Aborted;
    }

    public override Task CloseAsync(
        WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(
        WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
    {
        _state = WebSocketState.CloseSent;
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        Disposed = true;
        _state = WebSocketState.Closed;
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        ReceiveStarted.TrySetResult(true);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("The blocking receive should only end through cancellation.");
    }

    public override Task SendAsync(
        ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
