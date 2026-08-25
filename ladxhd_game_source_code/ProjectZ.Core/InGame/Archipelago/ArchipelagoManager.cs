using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Overlay;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Telemetry;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Archipelago
{
    public sealed class ArchipelagoManager
    {
        public const string GameName = "Links Awakening DX HD";
        public const int ZolAttackSpawnCount = 5;
        public static readonly Version ClientVersion = new Version(0, 6, 7);

        private const string SaveSeedName = "ap_seed_name";
        private const string SaveSlotName = "ap_slot_name";
        private const string SaveReceivedIndex = "ap_received_index";
        private const string SaveGoalPending = "ap_goal_pending";
        private const string SaveBowWowReceived = "ap_received_bowwow";
        private const string SaveRoosterReceived = "ap_received_rooster";
        private const string SaveBoomerangReceived = "ap_received_boomerang";
        private const string SaveProgressiveSwordCount = "ap_progressive_sword_count";
        private const string SaveProgressiveShieldCount = "ap_progressive_shield_count";
        private const string SaveProgressiveBraceletCount = "ap_progressive_bracelet_count";
        private const string SaveMaxPowderRefillApplied = "ap_max_powder_refill_applied";
        private const string SaveMaxBombsRefillApplied = "ap_max_bombs_refill_applied";
        private const string SaveMaxArrowsRefillApplied = "ap_max_arrows_refill_applied";
        private const string TarinGiftLocationKey = "script:tarin:2";
        private const string MarinSongLocationKey = "script:maria_song_repeat:1";
        private const string BoomerangGuyLocationKey = "script:npc_hidden_boomerang:2";
        private const string TrendyGameLocationKey = "item:trade0Collected";
        private const string WitchLocationKey = "script:witchTrade:22";
        private const string ColorFairyRedLocationKey = "script:color_fairy_red:1";
        private const string ColorFairyBlueLocationKey = "script:color_fairy_blue:1";
        private const string ShopShovelLocationKey = "shop:200";
        private const string ShopBowLocationKey = "shop:980";
        private const string SaveMarinSongOverride = "ap_marin_song_override";
        private const string SaveMarinSongState = "ap_marin_song_state";
        private const string SaveMarinSongDialog = "ap_marin_song_dialog";
        private const string SaveMarinSongDialogPresent = "ap_marin_song_dialog_present";
        private const int MarinMabePositionX = 368;
        private const int MarinMabePositionY = 1216;
        private const int InitialReconnectDelaySeconds = 5;
        private const int MaximumReconnectDelaySeconds = 60;
        private static readonly ArchipelagoHostedRoomResolver HostedRoomResolver = new ArchipelagoHostedRoomResolver();

        private readonly GameManager _gameManager;
        private readonly object _sessionLock = new object();
        private readonly ConcurrentQueue<QueuedNetworkItem> _receivedItems = new ConcurrentQueue<QueuedNetworkItem>();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private readonly HashSet<string> _cataloguedLocationKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<int> _replayedProgressiveIndexes = new HashSet<int>();
        private readonly MagpieTrackerBridge _magpieTracker = new MagpieTrackerBridge();

        private ArchipelagoConnectionSettings _settings;
        private ArchipelagoSeedManifest _seed;
        private ArchipelagoSession _session;
        private bool _connecting;
        private int _connectionGeneration;
        private int _nextReceivedIndex;
        private int _replayedProgressiveSwordCount;
        private int _replayedProgressiveShieldCount;
        private int _replayedProgressiveBraceletCount;
        private int _consecutiveConnectionFailures;
        private Task _sessionCleanupTask = Task.CompletedTask;
        private DateTime _nextReconnectUtc = DateTime.MinValue;
        private DateTime _connectionAttemptStartedUtc = DateTime.MinValue;
        private DateTime _connectedStartedUtc = DateTime.MinValue;
        private TimeSpan _connectedDuration;
        private int _telemetryConnectAttempts;
        private int _telemetryDisconnects;
        private int _telemetryReconnects;
        private int _telemetryChecksReported;
        private int _telemetryItemsReceived;
        private int _telemetryUnsupportedItems;
        private bool _telemetrySummaryReported;
        private string _status = "Disabled";

        public ArchipelagoManager(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public bool IsActive { get; private set; }
        public bool IsConfigured => _settings?.Enabled == true && _seed != null;
        public bool IsBoundSave => IsActive || HasSaveBinding(
            _gameManager.SaveManager.GetString(SaveSeedName),
            _gameManager.SaveManager.GetString(SaveSlotName));
        public bool CanShowEmbeddedTracker => IsActive && Game1.MagpieTrackerService.IsAvailable;
        public string Status => _status;
        public ArchipelagoSeedManifest Seed => _seed;

        public static bool HasSaveBinding(string seedName, string slotName)
        {
            return !string.IsNullOrWhiteSpace(seedName) && !string.IsNullOrWhiteSpace(slotName);
        }

        public void ShowEmbeddedTracker()
        {
            if (!CanShowEmbeddedTracker)
                return;

            if (!_magpieTracker.Start(_settings?.MagpieTrackerAllowLan == true))
                SetStatus($"Magpie tracker unavailable: port {MagpieTrackerProtocol.DefaultPort} is already in use");

            SynchronizeMagpieReceivedItems(GetConnectedSession());
            SynchronizeMagpieChecksFromSave();
            _magpieTracker.SetItemQuantity("RUPEE_COUNT", _gameManager.GetItem("ruby")?.Count ?? 0);
            Game1.MagpieTrackerService.Show();
        }

        public static int GetReconnectDelaySeconds(int consecutiveFailures)
        {
            if (consecutiveFailures <= 0)
                return 0;

            var exponent = Math.Min(consecutiveFailures - 1, 4);
            return Math.Min(MaximumReconnectDelaySeconds, InitialReconnectDelaySeconds << exponent);
        }

        public static TelemetryDisconnectReason ClassifySocketFailure(Exception exception)
        {
            var root = exception?.GetBaseException();
            if (root is JsonException ||
                root?.GetType().Namespace?.StartsWith("Newtonsoft.Json", StringComparison.Ordinal) == true)
                return TelemetryDisconnectReason.Protocol;

            if (root is WebSocketException || root is SocketException || root is IOException ||
                root is TimeoutException)
                return TelemetryDisconnectReason.Network;

            return TelemetryDisconnectReason.Unknown;
        }

        public static bool ShouldUseBoomerangGiftBehavior(bool boundSave)
        {
            return boundSave;
        }

        public static bool ShouldReplaceToadstoolWithPowder(bool boundSave)
        {
            // The APWorld treats Toadstool and Magic Powder as independent items. The Witch
            // script consumes the Toadstool when its location is checked; an unrelated Powder
            // receipt must not consume it first.
            return !boundSave;
        }

        public static bool ShouldUseColorFairyMultiReward(
            bool archipelagoActive, bool hasRedLocation, bool hasBlueLocation)
        {
            return archipelagoActive && hasRedLocation && hasBlueLocation;
        }

        public static int GetNextTunic(int currentTunic, bool ownsBlueTunic, bool ownsRedTunic)
        {
            if (currentTunic == GameManager.CloakGreen && ownsBlueTunic)
                return GameManager.CloakBlue;
            if (currentTunic != GameManager.CloakRed && ownsRedTunic)
                return GameManager.CloakRed;
            return GameManager.CloakGreen;
        }

        public static bool ShouldUseGhostHouseShellPot(
            bool boundSave, string mapName, int positionX, int positionY)
        {
            return boundSave &&
                   string.Equals(mapName, "hauntedhouse.map", StringComparison.Ordinal) &&
                   positionX == 128 && positionY == 96;
        }

        public static int ResolveArchipelagoShopItemState(
            bool archipelagoActive,
            bool hasShovelLocation, bool shovelLocationComplete,
            bool hasBowLocation, bool bowLocationComplete,
            int vanillaState)
        {
            if (!archipelagoActive)
                return vanillaState;
            if (hasShovelLocation && !shovelLocationComplete)
                return 0;
            if (hasBowLocation && !bowLocationComplete)
                return 1;
            return hasShovelLocation || hasBowLocation ? 2 : vanillaState;
        }

        public static bool IsShopPurchaseAtCapacity(
            bool randomizedLocationPending, int ownedCount, int maxCount)
        {
            // A randomized shelf is a location check, not another copy of the vanilla item.
            // The player must be able to pay for and collect that check even when the shelf's
            // original Shovel or Bow is already at its inventory maximum.
            return !randomizedLocationPending && ownedCount >= maxCount;
        }

        public bool IsLocationCheckPending(string sourceLocationKey)
        {
            return IsActive && !string.IsNullOrEmpty(sourceLocationKey) &&
                   _seed.LocationsByGameKey.ContainsKey(sourceLocationKey) &&
                   !IsLocationCheckComplete(sourceLocationKey);
        }

        public string ResolveShopItemSpawnerValue(string key, string vanillaValue)
        {
            if (!string.Equals(key, "shopItem0", StringComparison.Ordinal))
                return vanillaValue;

            var vanillaState = int.TryParse(vanillaValue, out var parsedState) ? parsedState : 0;
            var hasShovelLocation = _seed?.LocationsByGameKey.ContainsKey(ShopShovelLocationKey) == true;
            var hasBowLocation = _seed?.LocationsByGameKey.ContainsKey(ShopBowLocationKey) == true;
            return ResolveArchipelagoShopItemState(
                IsActive,
                hasShovelLocation, hasShovelLocation && IsPersistentLocationCheckComplete(ShopShovelLocationKey),
                hasBowLocation, hasBowLocation && IsPersistentLocationCheckComplete(ShopBowLocationKey),
                vanillaState).ToString();
        }

        public bool TryCycleTunicAtTelephone(string dialogName)
        {
            if (!IsActive || !string.Equals(dialogName, "ulrira", StringComparison.Ordinal))
                return false;

            var ownsBlueTunic = _gameManager.GetItem("cloakBlue") != null;
            var ownsRedTunic = _gameManager.GetItem("cloakRed") != null;
            if (!ownsBlueTunic && !ownsRedTunic)
                return false;

            _gameManager.CloakType = GetNextTunic(
                _gameManager.CloakType, ownsBlueTunic, ownsRedTunic);
            SaveGameSaveLoad.SaveGame(_gameManager, false);

            var tunicName = _gameManager.CloakType == GameManager.CloakBlue
                ? "Blue Tunic"
                : _gameManager.CloakType == GameManager.CloakRed
                    ? "Red Tunic"
                    : "Green Tunic";
            AchievementOverlay.PushArchipelagoItem("Equipped", tunicName, "At", "Telephone booth");
            return true;
        }

        public bool TryHandleColorFairyRewards()
        {
            var hasRedLocation = _seed?.LocationsByGameKey.ContainsKey(ColorFairyRedLocationKey) == true;
            var hasBlueLocation = _seed?.LocationsByGameKey.ContainsKey(ColorFairyBlueLocationKey) == true;
            if (!ShouldUseColorFairyMultiReward(IsActive, hasRedLocation, hasBlueLocation))
                return false;

            var changed = false;
            foreach (var sourceLocationKey in new[]
                     {
                         ColorFairyRedLocationKey,
                         ColorFairyBlueLocationKey
                     })
            {
                if (IsLocationCheckComplete(sourceLocationKey))
                    continue;

                changed |= TryHandleLocationCheck(new GameItemCollected("pieceOfPower")
                {
                    SourceLocationKey = sourceLocationKey
                });
            }

            if (changed)
                SaveGameSaveLoad.SaveGame(_gameManager, false);
            return true;
        }

        public static bool ShouldRepairToadstoolReceipt(
            bool witchCheckComplete, bool ownsToadstool)
        {
            return !witchCheckComplete && !ownsToadstool;
        }

        public static bool ShouldDismissMarinFollower(
            bool boundSave, bool historyEnabled, string marinState)
        {
            return boundSave && !historyEnabled &&
                   string.Equals(marinState, "3", StringComparison.Ordinal);
        }

        public static bool ShouldTreatMarinSongAsUnlearned(
            bool boundSave, string dialogKey, string itemName,
            bool locationMapped, bool locationComplete)
        {
            // Receiving Ballad from another AP location must not complete Marin's independent
            // teaching check. Hide only that item from her ownership branch while her mapped
            // location is pending; every other dialog continues to see the received song.
            return boundSave && locationMapped && !locationComplete &&
                   string.Equals(dialogKey, "maria", StringComparison.Ordinal) &&
                   string.Equals(itemName, "ocarina_maria", StringComparison.Ordinal);
        }

        public static bool ShouldRepairBoomerangReceipt(
            string receivedMarker, string storeMarker, bool ownsBoomerang)
        {
            return !string.Equals(receivedMarker, "1", StringComparison.Ordinal) ||
                   !string.Equals(storeMarker, "1", StringComparison.Ordinal) ||
                   !ownsBoomerang;
        }

        public static bool ShouldRestoreBoomerangTradeItem(string itemName, bool ownsItem)
        {
            return !ownsItem && itemName is "shovel" or "feather" or "magicRod" or "hookshot";
        }

        public static bool IsTrendyGamePrize(string saveKey)
        {
            return string.Equals(saveKey, "trade0Collected", StringComparison.Ordinal);
        }

        public static bool ShouldRepairTrendyPrize(
            bool boundSave, string sourceCollected, string persistentCheck)
        {
            return boundSave && string.Equals(sourceCollected, "1", StringComparison.Ordinal) &&
                   !string.Equals(persistentCheck, "1", StringComparison.Ordinal);
        }

        public static bool ShouldEnableMoblinCave(bool boundSave, string bossDefeated)
        {
            return boundSave && !string.Equals(bossDefeated, "1", StringComparison.Ordinal);
        }

        public static bool ShouldPreserveRoosterAfterDungeonSeven(
            bool boundSave, string dialogKey, string stateKey)
        {
            return boundSave &&
                   string.Equals(dialogKey, "instrument6", StringComparison.Ordinal) &&
                   (string.Equals(stateKey, "rooster", StringComparison.Ordinal) ||
                    string.Equals(stateKey, "has_rooster", StringComparison.Ordinal));
        }

        public static bool ShouldSuppressGhostAfterDungeonFour(
            bool boundSave, string dialogKey, string stateKey)
        {
            return boundSave &&
                   string.Equals(dialogKey, "instrument3", StringComparison.Ordinal) &&
                   string.Equals(stateKey, "spawn_ghost", StringComparison.Ordinal);
        }

        public static bool ShouldRepairGhostFollowerState(
            string spawnGhost, string hasGhost, bool ghostItemOwned)
        {
            return string.Equals(spawnGhost, "1", StringComparison.Ordinal) ||
                   string.Equals(hasGhost, "1", StringComparison.Ordinal) ||
                   ghostItemOwned;
        }

        public static bool ShouldIgnoreBowWowForDialog(
            bool boundSave, string dialogKey, string variableKey)
        {
            if (!boundSave)
                return false;

            return string.Equals(variableKey, "has_bowWow", StringComparison.Ordinal) &&
                       (string.Equals(dialogKey, "castle_monkey", StringComparison.Ordinal) ||
                        string.Equals(dialogKey, "npc_frog_boy", StringComparison.Ordinal) ||
                        string.Equals(dialogKey, "npc09", StringComparison.Ordinal)) ||
                   string.Equals(dialogKey, "npc09", StringComparison.Ordinal) &&
                       string.Equals(variableKey, "bowWow", StringComparison.Ordinal);
        }

        public static bool ShouldAllowSecretBookWithoutLens(
            bool boundSave, string dialogKey, string itemName)
        {
            return boundSave &&
                   string.Equals(dialogKey, "book8", StringComparison.Ordinal) &&
                   string.Equals(itemName, "trade13", StringComparison.Ordinal);
        }

        public static bool ShouldSuppressBombDrop(
            bool boundSave, bool hasBombs, string itemName)
        {
            return boundSave && !hasBombs &&
                   string.Equals(itemName, "bomb_1", StringComparison.Ordinal);
        }

        public static int ReconcileProgressiveCount(int savedCount, int replayedCount, int ownedLevel)
        {
            return Math.Max(0, Math.Max(savedCount, Math.Max(replayedCount, ownedLevel)));
        }

        public static int GetUpgradeAmmoCount(ArchipelagoItemEffect effect)
        {
            return effect == ArchipelagoItemEffect.MaxPowderUpgrade ? 40 :
                effect == ArchipelagoItemEffect.MaxBombsUpgrade ||
                effect == ArchipelagoItemEffect.MaxArrowsUpgrade ? 60 : 0;
        }

        public static bool IsSeashellMansionComplete(
            bool boundSave, string hasLevelTwoSword, string mansionSourceCollected)
        {
            return boundSave
                ? string.Equals(mansionSourceCollected, "1", StringComparison.Ordinal)
                : string.Equals(hasLevelTwoSword, "1", StringComparison.Ordinal);
        }

        public static bool ShouldRecoverSeashellMansionPresents(
            bool boundSave, bool unmissables, int saveFileVersion)
        {
            return saveFileVersion >= 1 && (boundSave || unmissables);
        }

        public static bool ShouldSpawnSeashellMansionPresent(
            bool recoverMissedPresents, int shellCount, int collectedPresentCount)
        {
            return recoverMissedPresents
                ? shellCount >= 5 && collectedPresentCount == 0 ||
                  shellCount >= 10 && collectedPresentCount < 2
                : shellCount == 5 || shellCount == 10;
        }

        public static bool ShouldKeepSeashellMansionActive(
            bool mansionComplete, bool recoverMissedPresents,
            int shellCount, int collectedPresentCount)
        {
            return !mansionComplete || ShouldSpawnSeashellMansionPresent(
                recoverMissedPresents, shellCount, collectedPresentCount);
        }

        public static bool ShouldSetLevelTwoSwordFlag(int swordLevel, string currentFlag)
        {
            return swordLevel >= 2 && !string.Equals(currentFlag, "1", StringComparison.Ordinal);
        }

        public static bool ShouldRepairRoosterReceipt(
            string receivedMarker, string followerFlag, bool itemOwned)
        {
            return !string.Equals(receivedMarker, "1", StringComparison.Ordinal) ||
                   !string.Equals(followerFlag, "1", StringComparison.Ordinal) ||
                   !itemOwned;
        }

        public static bool ShouldCompleteRoosterLocationWithoutResurrection(
            bool archipelagoActive, bool locationMapped,
            bool locationComplete, bool ownsRooster)
        {
            return archipelagoActive && locationMapped && !locationComplete && ownsRooster;
        }

        public bool ShouldTreatMarinSongAsUnlearned(string dialogKey, string itemName)
        {
            var locationMapped = _seed?.LocationsByGameKey.ContainsKey(MarinSongLocationKey) == true;
            return ShouldTreatMarinSongAsUnlearned(
                IsBoundSave, dialogKey, itemName, locationMapped,
                locationMapped && IsPersistentLocationCheckComplete(MarinSongLocationKey));
        }

        public bool ShouldCompleteRoosterLocationWithoutResurrection()
        {
            var sourceLocationKey = ArchipelagoLocationKey.Event("rooster");
            var locationMapped = _seed?.LocationsByGameKey.ContainsKey(sourceLocationKey) == true;
            return ShouldCompleteRoosterLocationWithoutResurrection(
                IsActive, locationMapped,
                locationMapped && IsPersistentLocationCheckComplete(sourceLocationKey),
                HasOwnedItem("rooster"));
        }

        public static bool ShouldOverrideRaccoonSpawnCondition(
            bool archipelagoActive,
            string conditionKey,
            string conditionValue,
            string spawnObjectId,
            string currentValue,
            string raccoonTransformedValue)
        {
            // The AP logic always models Raccoon Tarin as the forest obstacle, even before the
            // Start House item is collected. Keep the house Tarin available for that location by
            // overriding only this map spawner instead of prematurely advancing tarin_state.
            return archipelagoActive &&
                   string.Equals(conditionKey, "tarin_state", StringComparison.Ordinal) &&
                   string.Equals(conditionValue, "1", StringComparison.Ordinal) &&
                   string.Equals(spawnObjectId, "raccoon", StringComparison.Ordinal) &&
                   !string.Equals(currentValue, "1", StringComparison.Ordinal) &&
                   !string.Equals(currentValue, "2", StringComparison.Ordinal) &&
                   !string.Equals(raccoonTransformedValue, "1", StringComparison.Ordinal);
        }

        public void PrepareFiles()
        {
            try
            {
                _settings = null;
                _seed = null;
                var userDataRoot = Game1.UserDataPaths.UserDataRoot;
                var profileCount = Enumerable.Range(0, ArchipelagoConnectionSettings.ProfileCount)
                    .Count(slot => File.Exists(ArchipelagoConnectionSettings.GetProfilePath(userDataRoot, slot)));
                if (profileCount > 0)
                {
                    SetStatus($"Ready: {profileCount} Archipelago profile(s); select a save");
                    return;
                }

                if (File.Exists(ArchipelagoConnectionSettings.GetPath(userDataRoot)))
                {
                    SetStatus("Ready: legacy Archipelago profile; select a save");
                    return;
                }

                SetStatus($"Disabled (import a randomizer or create {ArchipelagoConnectionSettings.DirectoryName}/{ArchipelagoConnectionSettings.FileName})");
            }
            catch (Exception ex)
            {
                _settings = null;
                _seed = null;
                SetStatus($"Configuration error: {ex.Message}");
            }
        }

        public void OnBeforeSaveChange()
        {
            ReportTelemetrySummary();
            RestoreMarinSongState();
            _magpieTracker.Stop();
            IsActive = false;
            _nextReceivedIndex = 0;
            ResetReplayedProgressiveCounts();
            while (_receivedItems.TryDequeue(out _)) { }
            Disconnect();
        }

        public void BindNewSave(int saveSlot)
        {
            if (!LoadConfigurationForSave(saveSlot))
                return;

            _gameManager.SaveManager.SetString(SaveSeedName, _seed.SeedName);
            _gameManager.SaveManager.SetString(SaveSlotName, _seed.SlotName);
            _gameManager.SaveManager.SetInt(SaveReceivedIndex, 0);
            _gameManager.SaveManager.SetString(SaveGoalPending, "0");
            ActivateBoundSave();
        }

        public void OnSaveLoaded()
        {
            IsActive = false;
            if (!LoadConfigurationForSave(_gameManager.SaveSlot))
                return;

            var saveSeed = _gameManager.SaveManager.GetString(SaveSeedName);
            var saveSlot = _gameManager.SaveManager.GetString(SaveSlotName);
            if (string.IsNullOrEmpty(saveSeed) && string.IsNullOrEmpty(saveSlot))
            {
                SetStatus("Configured, but the selected save is vanilla");
                return;
            }

            if (!string.Equals(saveSeed, _seed.SeedName, StringComparison.Ordinal) ||
                !string.Equals(saveSlot, _seed.SlotName, StringComparison.Ordinal))
            {
                SetStatus($"Save binding mismatch: save is {saveSeed} / {saveSlot}");
                return;
            }

            RepairTarinForestState(IsPersistentLocationCheckComplete(TarinGiftLocationKey));
            ActivateBoundSave();
        }

        public void RepairBoundSaveBeforeMapLoad()
        {
            var saveSeed = _gameManager.SaveManager.GetString(SaveSeedName);
            var saveSlot = _gameManager.SaveManager.GetString(SaveSlotName);
            if (string.IsNullOrWhiteSpace(saveSeed) || string.IsNullOrWhiteSpace(saveSlot))
                return;

            // This hook runs before map objects are constructed. Load this save's manifest now
            // so a prematurely hidden Trendy prize can be repaired in time to spawn.
            var configurationLoaded = LoadConfigurationForSave(_gameManager.SaveSlot);

            // Older AP builds granted the first progressive sword without completing the
            // non-cutscene part of the beach sword event. Repair those saves before map objects
            // choose their music, otherwise every map continues to select the intro track.
            if (_gameManager.SwordLevel > 0 &&
                _gameManager.SaveManager.GetString("introMusic", "0") == "1")
                _gameManager.SaveManager.SetString("introMusic", "0");

            if (ShouldSetLevelTwoSwordFlag(
                    _gameManager.SwordLevel,
                    _gameManager.SaveManager.GetString("hasSword2", "0")))
                _gameManager.SaveManager.SetString("hasSword2", "1");

            RepairMoblinCaveState();

            if (_gameManager.SaveManager.GetString(SaveBowWowReceived, "0") == "1")
                ApplyEffect(ArchipelagoItemEffect.BowWow);
            if (_gameManager.SaveManager.GetString(SaveRoosterReceived, "0") == "1")
                EnsureRoosterReceivedState();
            if (_gameManager.SaveManager.GetString(SaveBoomerangReceived, "0") == "1")
                EnsureBoomerangReceivedState();

            RepairBoomerangGuyTradeState();

            if (configurationLoaded)
                RepairTrendyGamePrizeState();

            // The updated overworld gates Raccoon Tarin on tarin_state=1, which vanilla writes
            // during the beach sword sequence. AP does not require the randomized Sword before
            // this forest route, so repair saves where Tarin's opening gift already advanced his
            // dialog but the separate overworld state was never written.
            RepairTarinForestState(tarinGiftCompleted: false);
            RepairGhostFollowerState();

            // AP presents received items without running their vanilla pickup dialog. Repair
            // saves made by older builds where a trade item was granted but the dialog's world
            // state changes were therefore never applied.
            if (HasOwnedItem("trade4"))
                ApplyEffect(ArchipelagoItemEffect.TradeStick);
            if (HasOwnedItem("trade6"))
                ApplyEffect(ArchipelagoItemEffect.TradePineapple);
            if (HasOwnedItem("trade12"))
                ApplyEffect(ArchipelagoItemEffect.TradeScale);
            if (HasOwnedItem("trade13"))
                ApplyEffect(ArchipelagoItemEffect.TradeMagnifyingGlass);
        }

        private bool LoadConfigurationForSave(int saveSlot)
        {
            _settings = null;
            _seed = null;

            try
            {
                if (saveSlot is < 0 or >= ArchipelagoConnectionSettings.ProfileCount)
                {
                    SetStatus($"Invalid save position {saveSlot + 1}");
                    return false;
                }

                var userDataRoot = Game1.UserDataPaths.UserDataRoot;
                var profilePath = ArchipelagoConnectionSettings.GetProfilePath(userDataRoot, saveSlot);
                var usingProfile = File.Exists(profilePath);
                var settings = usingProfile
                    ? ArchipelagoConnectionSettings.LoadProfile(userDataRoot, saveSlot)
                    : ArchipelagoConnectionSettings.Load(userDataRoot);

                if (settings == null)
                {
                    SetStatus($"No Archipelago profile for Save {saveSlot + 1}");
                    return false;
                }
                if (!settings.Enabled)
                {
                    SetStatus($"Archipelago disabled for Save {saveSlot + 1}");
                    return false;
                }
                if (settings.SaveSlot.HasValue && settings.SaveSlot.Value != saveSlot)
                {
                    SetStatus($"Archipelago profile targets Save {settings.SaveSlot.Value + 1}, not Save {saveSlot + 1}");
                    return false;
                }

                var seedPath = usingProfile
                    ? settings.ResolveProfileSeedPath(userDataRoot, saveSlot)
                    : settings.ResolveSeedPath(userDataRoot);
                var seed = ArchipelagoSeedManifest.Load(seedPath);
                if (!string.Equals(settings.Slot, seed.SlotName, StringComparison.Ordinal))
                    throw new InvalidDataException($"connection.json slot '{settings.Slot}' does not match seed slot '{seed.SlotName}'.");

                _settings = settings;
                _seed = seed;
                SetStatus($"Ready: {seed.SeedName} / {seed.SlotName} (Save {saveSlot + 1})");
                return true;
            }
            catch (Exception ex)
            {
                _settings = null;
                _seed = null;
                SetStatus($"Save {saveSlot + 1} configuration error: {ex.Message}");
                return false;
            }
        }

        private void ActivateBoundSave()
        {
            IsActive = true;
            ResetReplayedProgressiveCounts();
            RepairMoblinCaveState();
            ResetTelemetrySession();
            _nextReceivedIndex = Math.Max(0, _gameManager.SaveManager.GetInt(SaveReceivedIndex, 0));
            SetStatus($"Bound: {_seed.SeedName} / {_seed.SlotName}");
            RecordRandomizerManifest();
            _magpieTracker.Configure(
                _settings.MagpieTrackerEnabled, _settings.MagpieTrackerAllowLan, _seed);
            SynchronizeMagpieChecksFromSave();
            _gameManager.MapManager?.CurrentMap?.Objects?.TriggerKeyChange();
            if (_settings.AutoConnect)
                Connect();
        }

        private void RepairMoblinCaveState()
        {
            // AP exposes the Moblin Cave check independently of Tail Cave. Vanilla only enables
            // this encounter when the first instrument sequence advances mc_enemies.
            if (ShouldEnableMoblinCave(IsBoundSave,
                    _gameManager.SaveManager.GetString("mc_boss", "0")))
                _gameManager.SaveManager.SetString("mc_enemies", "1");
        }

        private void RepairGhostFollowerState()
        {
            var saveManager = _gameManager.SaveManager;
            if (!ShouldRepairGhostFollowerState(
                    saveManager.GetString("spawn_ghost", "0"),
                    saveManager.GetString("has_ghost", "0"),
                    HasOwnedItem("ghost")))
                return;

            saveManager.SetString("spawn_ghost", "0");
            saveManager.SetString("has_ghost", "0");
            saveManager.SetString("ghost_blockade", "0");
            _gameManager.RemoveItem("ghost", 99);
        }

        public void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    SetStatus($"Archipelago update error: {ex.Message}");
                }
            }

            if (IsActive && _settings?.AutoConnect == true && ShouldAttemptReconnect())
                Connect();

            if (IsActive && _magpieTracker.BoundPort > 0)
            {
                _magpieTracker.SetItemQuantity("RUPEE_COUNT", _gameManager.GetItem("ruby")?.Count ?? 0);
                UpdateMagpieLocation();
            }

            // Event overrides may inspect the active map, Link, and save state. During file
            // selection and save loading those managers are not guaranteed to exist yet.
            if (!IsActive || Game1.UiManager?.CurrentScreen != Values.ScreenNameGame ||
                _gameManager?.MapManager?.CurrentMap == null || MapManager.ObjLink == null ||
                _gameManager?.SaveManager?.HistoryEnabled != false)
                return;

            if (RepairMarinFollowerState())
                SaveGameSaveLoad.SaveGame(_gameManager, false);

            UpdateMarinSongAccess();

            var repairedReplayState = false;
            while (_receivedItems.TryPeek(out var queued) && queued.Index < _nextReceivedIndex)
            {
                repairedReplayState |= RepairPreviouslyReceivedItem(queued.Index, queued.ItemName);
                _receivedItems.TryDequeue(out _);
            }

            if (repairedReplayState)
                SaveGameSaveLoad.SaveGame(_gameManager, false);

            if (!_receivedItems.TryPeek(out var next) || next.Index != _nextReceivedIndex)
                return;

            if (!TryApplyReceivedItem(next))
                return;

            _receivedItems.TryDequeue(out next);
            _nextReceivedIndex = next.Index + 1;
            _gameManager.SaveManager.SetInt(SaveReceivedIndex, _nextReceivedIndex);

            // Item replay is part of the AP protocol. Persist each applied index immediately so
            // consumables and traps cannot be granted twice after an unclean shutdown.
            SaveGameSaveLoad.SaveGame(_gameManager, false);
        }

        private void UpdateMagpieLocation()
        {
            var map = _gameManager?.MapManager?.CurrentMap;
            var link = MapManager.ObjLink;
            if (map == null || link == null)
                return;

            var isInterior = map.IsHouse || map.IsCave || map.Is2dMap || map.IsCastle ||
                             map.IsEgg || map.IsFinalMap;
            if (MagpieTrackerLocationMapper.TryCreate(
                    map.IsOverworld, map.IsDungeon, isInterior,
                    map.LocationName, map.MapName, map.MapOffsetX, map.MapOffsetY,
                    link.PosX, link.PosY, out var location))
                _magpieTracker.SetLocation(location);
        }

        public bool TryHandleLocationCheck(GameItemCollected item)
        {
            if (!IsActive || item == null || string.IsNullOrEmpty(item.SourceLocationKey))
                return false;
            if (!_seed.LocationsByGameKey.TryGetValue(item.SourceLocationKey, out var location))
            {
                SetStatus($"Unmapped game location: {item.SourceLocationKey}");
                return false;
            }

            _gameManager.SaveManager.SetString(ArchipelagoLocationKey.PersistentCheck(location.LocationId), "1");
            _magpieTracker.RecordCheck(location);
            Interlocked.Increment(ref _telemetryChecksReported);

            if (string.Equals(item.SourceLocationKey, TarinGiftLocationKey, StringComparison.Ordinal))
                RepairTarinForestState(tarinGiftCompleted: true);

            if (string.Equals(item.SourceLocationKey, MarinSongLocationKey, StringComparison.Ordinal))
                RestoreMarinSongState();

            if (string.Equals(item.SourceLocationKey, BoomerangGuyLocationKey, StringComparison.Ordinal))
                ClearBoomerangTradeFlags();

            var recipientName = !string.IsNullOrWhiteSpace(location.ItemPlayerName)
                ? location.ItemPlayerName
                : location.ItemPlayer == location.LocalPlayer
                    ? _seed.SlotName
                    : $"Player {location.ItemPlayer}";
            AchievementOverlay.PushArchipelagoItem("Found", location.ItemName, "For", recipientName);

            var session = GetConnectedSession();
            if (session != null)
            {
                try
                {
                    session.Locations.CompleteLocationChecks(location.LocationId);
                }
                catch (Exception ex)
                {
                    HandleCurrentSessionFailure(session, $"Location queued for reconnect: {ex.Message}");
                }
            }
            else
            {
                SetStatus($"Checked offline: {location.LocationName}");
            }

            // The source object stores its normal save key after this method returns. On every
            // successful connection, all mapped save keys are resubmitted; checks are idempotent.
            return true;
        }

        private void UpdateMarinSongAccess()
        {
            var locationPending = _seed?.LocationsByGameKey.ContainsKey(MarinSongLocationKey) == true &&
                                  !IsLocationCheckComplete(MarinSongLocationKey);
            var map = _gameManager.MapManager.CurrentMap;
            var link = MapManager.ObjLink;
            var inMabeVillage = map?.MapName == "overworld.map" && link != null &&
                                map.GetField(new Vector2(MarinMabePositionX, MarinMabePositionY))
                                   .Contains(link.CenterPosition.Position);

            if (locationPending && HasOwnedItem("ocarina") && inMabeVillage)
                ApplyMarinSongState();
            else
                RestoreMarinSongState();
        }

        private bool RepairMarinFollowerState()
        {
            var saveManager = _gameManager.SaveManager;
            if (!ShouldDismissMarinFollower(
                    IsBoundSave, saveManager.HistoryEnabled,
                    saveManager.GetString("maria_state", "0")))
                return false;

            // The APWorld moves the Walrus and does not require Marin to clear the desert.
            // If the vanilla beach sequence was started, advance directly to the normal
            // post-Walrus state once its dialog history closes so Marin cannot follow forever.
            saveManager.SetString("maria_state", "4");
            saveManager.SetString("has_marin", "0");
            saveManager.SetString("maria_dungeon", "0");
            _gameManager.RemoveItem("marin", 99);

            foreach (var gameObject in _gameManager.MapManager.CurrentMap.Objects
                         .GetObjectsOfType(typeof(ObjMarin)))
                ((ObjMarin)gameObject).DismissArchipelagoFollower();

            return true;
        }

        private void ApplyMarinSongState()
        {
            var saveManager = _gameManager.SaveManager;
            if (saveManager.GetString(SaveMarinSongOverride, "0") == "1")
                return;

            // The AP 0.6.7 LADX rules expose Marin's Mabe Village song check whenever Link has
            // the Ocarina. Preserve the current vanilla story state while Link is in her Mabe
            // field so later Marin, beach, and Animal Village sequences can resume unchanged.
            saveManager.SetString(SaveMarinSongState, saveManager.GetString("maria_state", "0"));
            var dialogState = saveManager.GetString("maria");
            saveManager.SetString(SaveMarinSongDialogPresent, dialogState == null ? "0" : "1");
            if (dialogState != null)
                saveManager.SetString(SaveMarinSongDialog, dialogState);
            saveManager.SetString(SaveMarinSongOverride, "1");
            saveManager.SetString("maria_state", "1");
            saveManager.RemoveString("maria");
        }

        private void RestoreMarinSongState()
        {
            var saveManager = _gameManager.SaveManager;
            if (saveManager.GetString(SaveMarinSongOverride, "0") != "1")
                return;

            saveManager.SetString("maria_state", saveManager.GetString(SaveMarinSongState, "0"));
            if (saveManager.GetString(SaveMarinSongDialogPresent, "0") == "1")
                saveManager.SetString("maria", saveManager.GetString(SaveMarinSongDialog, ""));
            else
                saveManager.RemoveString("maria");

            saveManager.RemoveString(SaveMarinSongOverride);
            saveManager.RemoveString(SaveMarinSongState);
            saveManager.RemoveString(SaveMarinSongDialog);
            saveManager.RemoveString(SaveMarinSongDialogPresent);
        }

        public bool IsLocationCheckComplete(string sourceLocationKey)
        {
            return IsActive && !string.IsNullOrEmpty(sourceLocationKey) &&
                   _seed.LocationsByGameKey.TryGetValue(sourceLocationKey, out var location) &&
                   _gameManager.SaveManager.GetString(ArchipelagoLocationKey.PersistentCheck(location.LocationId)) == "1";
        }

        public string ResolveLocationItemName(string sourceLocationKey, string vanillaItemName,
            string mapName = null, int? positionX = null, int? positionY = null)
        {
            RecordLocationSource(sourceLocationKey, vanillaItemName, mapName, positionX, positionY);

            if (!IsActive || string.IsNullOrEmpty(sourceLocationKey) ||
                !_seed.LocationsByGameKey.TryGetValue(sourceLocationKey, out var location))
                return vanillaItemName;

            if (location.ItemPlayer == location.LocalPlayer &&
                string.Equals(location.ItemGame, GameName, StringComparison.Ordinal) &&
                ArchipelagoItemMapper.TryMap(location.ItemName, GetOwnedEquipmentLevel("sword1", "sword2"),
                    GetOwnedEquipmentLevel("shield", "mirrorShield"),
                    GetOwnedEquipmentLevel("stonelifter", "stonelifter2"), out var mapping) &&
                !string.IsNullOrEmpty(mapping.GameItemName) && _gameManager.ItemManager[mapping.GameItemName] != null)
                return mapping.GameItemName;

            // Foreign and trap items use an existing neutral pickup sprite. The original item is
            // never granted here; TryHandleLocationCheck reports the check to the server instead.
            return "pieceOfPower";
        }

        private void RecordLocationSource(string sourceLocationKey, string vanillaItemName,
            string mapName, int? positionX, int? positionY)
        {
            if (!IsActive || string.IsNullOrWhiteSpace(sourceLocationKey) ||
                !_cataloguedLocationKeys.Add(sourceLocationKey))
                return;

            try
            {
                var directory = ArchipelagoConnectionSettings.GetDirectory(Game1.UserDataPaths.UserDataRoot);
                Directory.CreateDirectory(directory);
                var entry = new Dictionary<string, object>
                {
                    ["game_key"] = sourceLocationKey,
                    ["vanilla_item"] = vanillaItemName,
                    ["map"] = mapName,
                    ["x"] = positionX,
                    ["y"] = positionY,
                    ["mapped"] = _seed.LocationsByGameKey.ContainsKey(sourceLocationKey)
                };
                File.AppendAllText(Path.Combine(directory, "location-catalog.jsonl"),
                    JsonSerializer.Serialize(entry) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Archipelago] Could not record location source: {ex.Message}");
            }
        }

        public void ReportGoal()
        {
            if (!IsActive)
                return;

            _gameManager.SaveManager.SetString(SaveGoalPending, "1");
            var session = GetConnectedSession();
            if (session == null)
                return;

            try
            {
                session.SetGoalAchieved();
                _gameManager.SaveManager.SetString(SaveGoalPending, "0");
                SetStatus("Goal reported");
            }
            catch (Exception ex)
            {
                HandleCurrentSessionFailure(session, $"Goal queued for reconnect: {ex.Message}");
            }
        }

        public void Connect()
        {
            if (!IsActive || !IsConfigured)
                return;

            int generation;
            int attempt;
            ArchipelagoConnectionSettings settings;
            ArchipelagoSeedManifest seed;
            lock (_sessionLock)
            {
                if (_connecting || _session?.Socket.Connected == true || !_sessionCleanupTask.IsCompleted)
                    return;
                _session = null;
                _connecting = true;
                generation = ++_connectionGeneration;
                attempt = ++_telemetryConnectAttempts;
                if (attempt > 1)
                    _telemetryReconnects++;
                _connectionAttemptStartedUtc = DateTime.UtcNow;
                settings = _settings;
                seed = _seed;
            }

            TelemetryManager.Client?.RecordConnectAttempt(attempt);
            SetStatus(string.IsNullOrWhiteSpace(settings.RoomUrl)
                ? $"Connecting to {settings.Server}..."
                : "Waking Archipelago hosted room...");
            _ = Task.Run(() => ConnectWorkerAsync(generation, attempt, settings, seed));
        }

        private async Task ConnectWorkerAsync(int generation, int attempt, ArchipelagoConnectionSettings settings,
            ArchipelagoSeedManifest seed)
        {
            ArchipelagoSession newSession = null;
            var errorCategory = TelemetryConnectionError.Network;
            Exception roomRecoveryFailure = null;
            string persistenceWarning = null;
            try
            {
                var server = settings.Server?.Trim();
                if (!string.IsNullOrWhiteSpace(settings.RoomUrl))
                {
                    try
                    {
                        var resolvedServer = await HostedRoomResolver.ResolveServerAsync(settings.RoomUrl)
                            .ConfigureAwait(false);
                        if (generation != Volatile.Read(ref _connectionGeneration) || !IsActive)
                            return;

                        if (!string.Equals(server, resolvedServer, StringComparison.OrdinalIgnoreCase))
                        {
                            server = resolvedServer;
                            persistenceWarning = ApplyResolvedHostedRoomServer(
                                generation, settings, resolvedServer);
                        }
                    }
                    catch (Exception ex)
                    {
                        roomRecoveryFailure = ex;
                        if (string.IsNullOrWhiteSpace(server))
                            throw new InvalidOperationException(
                                $"Could not wake the hosted room: {ex.Message}", ex);

                        SetStatus($"Room wake failed; trying saved endpoint {server}...");
                    }
                }

                if (generation != Volatile.Read(ref _connectionGeneration) || !IsActive)
                    return;
                if (string.IsNullOrWhiteSpace(server))
                    throw new InvalidDataException("No Archipelago server endpoint is available.");

                SetStatus($"Connecting to {server}...");
                newSession = ArchipelagoSessionFactory.CreateSession(server);
                newSession.Socket.ErrorReceived += (exception, _) =>
                {
                    var reason = ClassifySocketFailure(exception);
                    var label = reason == TelemetryDisconnectReason.Protocol ? "Protocol error" : "Network error";
                    HandleSocketFailure(generation, newSession, reason, $"{label}; reconnecting");
                };
                newSession.Socket.SocketClosed += reason => HandleSocketFailure(
                    generation, newSession, TelemetryDisconnectReason.Server,
                    $"Disconnected: {reason}; reconnecting");
                newSession.Items.ItemReceived += helper =>
                {
                    var item = helper.DequeueItem();
                    if (item != null && generation == Volatile.Read(ref _connectionGeneration) && IsActive)
                    {
                        _magpieTracker.RecordReceivedItem(helper.Index - 1, item.ItemName);
                        _receivedItems.Enqueue(QueuedNetworkItem.From(helper.Index - 1, item));
                    }
                };
                newSession.Locations.CheckedLocationsUpdated += locations =>
                {
                    if (generation != Volatile.Read(ref _connectionGeneration) || !IsActive)
                        return;
                    foreach (var locationId in locations)
                        RecordMagpieServerCheck(seed, locationId);
                };

                var login = newSession.TryConnectAndLogin(GameName, settings.Slot, ItemsHandlingFlags.AllItems,
                    version: ClientVersion, password: settings.Password, requestSlotData: true);
                if (login is LoginFailure failure)
                {
                    errorCategory = TelemetryConnectionError.Authentication;
                    throw new InvalidOperationException(string.Join("; ", failure.Errors));
                }

                var successful = (LoginSuccessful)login;
                if (successful.SlotData.TryGetValue("seed_name", out var serverSeed) &&
                    !string.Equals(serverSeed?.ToString(), seed.SeedName, StringComparison.Ordinal))
                {
                    errorCategory = TelemetryConnectionError.SeedMismatch;
                    throw new InvalidDataException($"Server seed '{serverSeed}' does not match '{seed.SeedName}'.");
                }

                int durationMs;
                lock (_sessionLock)
                {
                    if (generation != _connectionGeneration || !IsActive)
                    {
                        _sessionCleanupTask = CleanupSessionAsync(newSession);
                        return;
                    }
                    _session = newSession;
                    _connecting = false;
                    _consecutiveConnectionFailures = 0;
                    _nextReconnectUtc = DateTime.MaxValue;
                    durationMs = ElapsedMilliseconds(_connectionAttemptStartedUtc);
                    _connectionAttemptStartedUtc = DateTime.MinValue;
                    _connectedStartedUtc = DateTime.UtcNow;
                }

                TelemetryManager.Client?.RecordConnectSuccess(attempt, durationMs, seed.WorldVersion);

                SynchronizeMagpieReceivedItems(newSession);
                foreach (var locationId in newSession.Locations.AllLocationsChecked)
                    RecordMagpieServerCheck(seed, locationId);

                _mainThreadActions.Enqueue(() =>
                {
                    if (generation != Volatile.Read(ref _connectionGeneration) || !IsActive)
                        return;
                    SetStatus(string.IsNullOrWhiteSpace(persistenceWarning)
                        ? $"Connected: {seed.SlotName}"
                        : $"Connected: {seed.SlotName}; {persistenceWarning}");
                    ResubmitCheckedLocations();
                    if (_gameManager.SaveManager.GetString(SaveGoalPending, "0") == "1")
                        ReportGoal();
                });
            }
            catch (Exception ex)
            {
                var isCurrentGeneration = false;
                var reconnectDelaySeconds = 0;
                lock (_sessionLock)
                {
                    _sessionCleanupTask = CleanupSessionAsync(newSession);
                    if (generation == _connectionGeneration)
                    {
                        isCurrentGeneration = true;
                        _connecting = false;
                        reconnectDelaySeconds = GetReconnectDelaySeconds(++_consecutiveConnectionFailures);
                        _nextReconnectUtc = DateTime.UtcNow + TimeSpan.FromSeconds(reconnectDelaySeconds);
                    }
                }
                if (isCurrentGeneration)
                {
                    if (errorCategory == TelemetryConnectionError.Network)
                        errorCategory = ClassifyConnectionFailure(ex);
                    TelemetryManager.Client?.RecordConnectFailure(
                        attempt, ElapsedMilliseconds(_connectionAttemptStartedUtc), errorCategory);
                    TelemetryManager.Client?.RecordReconnectScheduled(attempt + 1, reconnectDelaySeconds);
                    var failureMessage = roomRecoveryFailure == null || ReferenceEquals(ex, roomRecoveryFailure)
                        ? ex.Message
                        : $"{ex.Message} (room recovery also failed: {roomRecoveryFailure.Message})";
                    SetStatus($"Connection failed: {failureMessage}");
                }
            }
        }

        private string ApplyResolvedHostedRoomServer(int generation,
            ArchipelagoConnectionSettings settings, string resolvedServer)
        {
            lock (_sessionLock)
            {
                if (generation != _connectionGeneration || !ReferenceEquals(settings, _settings))
                    return null;
                settings.Server = resolvedServer;
            }

            try
            {
                settings.SaveCurrentProfile(Game1.UserDataPaths.UserDataRoot);
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Archipelago] Could not persist hosted-room port: {ex.Message}");
                return "new room port could not be saved";
            }
        }

        private void ResubmitCheckedLocations()
        {
            var session = GetConnectedSession();
            if (session == null)
                return;

            var checkedIds = new List<long>();
            foreach (var location in _seed.Locations)
            {
                var persistentlyChecked = _gameManager.SaveManager.GetString(
                    ArchipelagoLocationKey.PersistentCheck(location.LocationId)) == "1";
                var sourceObjectChecked = TryGetSaveKey(location.GameKey, out var saveKey) &&
                                          _gameManager.SaveManager.GetString(saveKey) == "1";
                if (persistentlyChecked || sourceObjectChecked)
                    checkedIds.Add(location.LocationId);
            }

            if (checkedIds.Count == 0)
                return;

            try
            {
                session.Locations.CompleteLocationChecks(checkedIds.Distinct().ToArray());
            }
            catch (Exception ex)
            {
                HandleCurrentSessionFailure(session, $"Check recovery queued for reconnect: {ex.Message}");
            }
        }

        private void SynchronizeMagpieChecksFromSave()
        {
            if (_seed == null)
                return;

            foreach (var location in _seed.Locations)
            {
                var persistentlyChecked = _gameManager.SaveManager.GetString(
                    ArchipelagoLocationKey.PersistentCheck(location.LocationId)) == "1";
                var sourceObjectChecked = TryGetSaveKey(location.GameKey, out var saveKey) &&
                                          _gameManager.SaveManager.GetString(saveKey) == "1";
                if (persistentlyChecked || sourceObjectChecked)
                    _magpieTracker.RecordCheck(location);
            }
        }

        private void SynchronizeMagpieReceivedItems(ArchipelagoSession session)
        {
            if (session == null)
                return;

            // ItemReceived is normally raised once per received item, but the session's complete
            // history is authoritative. Replaying it here repairs any callback missed before the
            // tracker bridge started and makes opening Magpie a deterministic full resync.
            _magpieTracker.SynchronizeReceivedItems(
                session.Items.AllItemsReceived.Select(item => item.ItemName));
        }

        private void RecordMagpieServerCheck(ArchipelagoSeedManifest seed, long locationId)
        {
            var location = seed?.Locations?.FirstOrDefault(candidate => candidate.LocationId == locationId);
            if (location != null)
                _magpieTracker.RecordCheck(location);
        }

        private bool TryApplyReceivedItem(QueuedNetworkItem queued)
        {
            var swordCount = GetProgressiveReceiptCount(
                "Progressive Sword", GetOwnedEquipmentLevel("sword1", "sword2"));
            var shieldCount = GetProgressiveReceiptCount(
                "Progressive Shield", GetOwnedEquipmentLevel("shield", "mirrorShield"));
            var braceletCount = GetProgressiveReceiptCount(
                "Progressive Power Bracelet", GetOwnedEquipmentLevel("stonelifter", "stonelifter2"));

            if (!ArchipelagoItemMapper.TryMap(
                    queued.ItemName, swordCount, shieldCount, braceletCount, out var mapping))
            {
                Interlocked.Increment(ref _telemetryItemsReceived);
                Interlocked.Increment(ref _telemetryUnsupportedItems);
                SetStatus($"Unsupported item skipped: {queued.ItemName}");
                return true;
            }

            GameItemCollected receivedItem = null;
            if (!string.IsNullOrEmpty(mapping.GameItemName))
            {
                receivedItem = new GameItemCollected(mapping.GameItemName)
                {
                    Count = mapping.Count,
                    LocationBounding = mapping.LocationBounding
                };
            }

            if (!MapManager.ObjLink.TryPresentArchipelagoItem(receivedItem, () =>
                GrantReceivedItem(queued.ItemName, mapping, receivedItem)))
                return false;

            AchievementOverlay.PushArchipelagoItem("Received", queued.ItemName, "From", queued.SenderName);
            SetStatus($"Received: {queued.ItemName} from {queued.SenderName}");
            Interlocked.Increment(ref _telemetryItemsReceived);
            return true;
        }

        private void GrantReceivedItem(
            string archipelagoItemName, ArchipelagoItemMapping mapping, GameItemCollected receivedItem)
        {
            ApplyEffect(mapping.Effect);
            if (receivedItem == null)
                return;

            var ownedSwordLevel = GetOwnedEquipmentLevel("sword1", "sword2");
            var isFirstSword = mapping.GameItemName == "sword1" && ownedSwordLevel == 0;
            var slot = -1;
            if (mapping.GameItemName == "sword2" && ownedSwordLevel == 1)
            {
                slot = _gameManager.GetEquipmentSlot("sword1");
                _gameManager.RemoveItem("sword1", 99);
            }
            else if (mapping.GameItemName == "mirrorShield" && _gameManager.ShieldLevel == 1)
            {
                slot = _gameManager.GetEquipmentSlot("shield");
                _gameManager.RemoveItem("shield", 99);
            }
            else if (mapping.GameItemName == "stonelifter2" && _gameManager.StoneGrabberLevel == 1)
            {
                slot = _gameManager.GetEquipmentSlot("stonelifter");
                _gameManager.RemoveItem("stonelifter", 99);
            }

            _gameManager.CollectItem(receivedItem, slot);
            if (string.Equals(archipelagoItemName, "Boomerang", StringComparison.Ordinal))
            {
                _gameManager.SaveManager.SetString(SaveBoomerangReceived, "1");
                _gameManager.SaveManager.SetString("store_boomerang", "1");
            }
            if (mapping.GameItemName == "sword2")
                _gameManager.SaveManager.SetString("hasSword2", "1");
            IncrementProgressiveReceiptCount(archipelagoItemName);
            if (isFirstSword)
                MapManager.ObjLink.CompleteArchipelagoFirstSwordMusic();
        }

        private void ApplyEffect(ArchipelagoItemEffect effect)
        {
            switch (effect)
            {
                case ArchipelagoItemEffect.BadHeartContainer:
                    _gameManager.MaxHearts = Math.Max(1, _gameManager.MaxHearts - 1);
                    _gameManager.CurrentHealth = Math.Min(_gameManager.CurrentHealth, _gameManager.MaxHearts * 4);
                    break;
                case ArchipelagoItemEffect.BowWow:
                    _gameManager.SaveManager.SetString(SaveBowWowReceived, "1");
                    _gameManager.SaveManager.SetString("bowWow", "2");
                    _gameManager.SaveManager.SetString("has_bowWow", "1");
                    break;
                case ArchipelagoItemEffect.Rooster:
                    // The vanilla rooster pickup dialog normally writes these story flags.
                    // Remote AP items deliberately skip local pickup scripts, so mirror the
                    // persistent ownership state here while leaving the grave location's own
                    // save key untouched until that check is actually completed.
                    _gameManager.SaveManager.SetString(SaveRoosterReceived, "1");
                    PromoteStringState("chicken_dude", 1);
                    _gameManager.SaveManager.SetString("has_rooster", "1");
                    PromoteStringState("ulrira_d7", 2);
                    break;
                case ArchipelagoItemEffect.TradeStick:
                    ApplyStickEventState();
                    break;
                case ArchipelagoItemEffect.TradePineapple:
                    ApplyPineappleEventState();
                    break;
                case ArchipelagoItemEffect.TradeScale:
                    _gameManager.SaveManager.SetString("npc_mermaid_leave", "1");
                    _gameManager.SaveManager.SetString("npc_mermaid_gone", "1");
                    break;
                case ArchipelagoItemEffect.TradeMagnifyingGlass:
                    PromoteStringState("npc_painter", 2);
                    break;
                case ArchipelagoItemEffect.ZolAttack:
                    SpawnZolAttack();
                    break;
                case ArchipelagoItemEffect.GuardianAcorn:
                    Game1.AudioManager.InitGuardianAcorn();
                    break;
                case ArchipelagoItemEffect.PieceOfPower:
                    Game1.AudioManager.InitPieceOfPower();
                    break;
                case ArchipelagoItemEffect.MaxPowderUpgrade:
                    _gameManager.SaveManager.SetString("upgradePowder", "1");
                    RefillAmmo("powder", GetUpgradeAmmoCount(effect), createIfMissing: false);
                    _gameManager.SaveManager.SetString(SaveMaxPowderRefillApplied, "1");
                    break;
                case ArchipelagoItemEffect.MaxBombsUpgrade:
                    _gameManager.SaveManager.SetString("upgradeBomb", "1");
                    RefillAmmo("bomb", GetUpgradeAmmoCount(effect), createIfMissing: true);
                    _gameManager.SaveManager.SetString(SaveMaxBombsRefillApplied, "1");
                    break;
                case ArchipelagoItemEffect.MaxArrowsUpgrade:
                    _gameManager.SaveManager.SetString("upgradeBow", "1");
                    RefillArrows(GetUpgradeAmmoCount(effect));
                    _gameManager.SaveManager.SetString(SaveMaxArrowsRefillApplied, "1");
                    break;
            }
        }

        private void RefillAmmo(string itemName, int count, bool createIfMissing)
        {
            var item = _gameManager.GetItem(itemName);
            if (item != null)
            {
                item.Count = count;
                return;
            }

            if (createIfMissing)
                _gameManager.CollectItem(new GameItemCollected(itemName) { Count = count });
        }

        private void RefillArrows(int count)
        {
            var arrows = _gameManager.GetItem("bow") ?? _gameManager.GetItem("arrow");
            if (arrows != null)
                arrows.Count = count;
            else
                _gameManager.CollectItem(new GameItemCollected("arrow") { Count = count });
        }

        private void SpawnZolAttack()
        {
            var map = _gameManager.MapManager?.CurrentMap;
            var link = MapManager.ObjLink;
            if (map?.Objects == null || link == null)
                return;

            var offsets = new[]
            {
                new Point(-24, -16),
                new Point(24, -16),
                new Point(-24, 16),
                new Point(24, 16),
                new Point(0, -32)
            };
            for (var index = 0; index < ZolAttackSpawnCount; index++)
            {
                var offset = offsets[index];
                map.Objects.SpawnObject(new EnemyGreenZol(
                    map,
                    (int)link.Position.X + offset.X - 8,
                    (int)link.Position.Y + offset.Y - 13,
                    24,
                    true));
            }
        }

        private bool RepairPreviouslyReceivedItem(int itemIndex, string itemName)
        {
            if (TryGetProgressiveSaveKey(itemName, out var saveKey))
            {
                if (!_replayedProgressiveIndexes.Add(itemIndex))
                    return false;

                var replayedCount = IncrementReplayedProgressiveCount(itemName);
                var savedCount = _gameManager.SaveManager.GetInt(saveKey, 0);
                var reconciledCount = ReconcileProgressiveCount(savedCount, replayedCount, 0);
                if (reconciledCount == savedCount)
                    return false;

                _gameManager.SaveManager.SetInt(saveKey, reconciledCount);
                return true;
            }

            if (string.Equals(itemName, "Rooster", StringComparison.Ordinal))
            {
                var roosterNeedsRepair = ShouldRepairRoosterReceipt(
                    _gameManager.SaveManager.GetString(SaveRoosterReceived, "0"),
                    _gameManager.SaveManager.GetString("has_rooster", "0"),
                    HasOwnedItem("rooster"));
                if (roosterNeedsRepair)
                    EnsureRoosterReceivedState();
                return roosterNeedsRepair;
            }

            if (string.Equals(itemName, "Boomerang", StringComparison.Ordinal))
            {
                var boomerangNeedsRepair = ShouldRepairBoomerangReceipt(
                    _gameManager.SaveManager.GetString(SaveBoomerangReceived, "0"),
                    _gameManager.SaveManager.GetString("store_boomerang", "0"),
                    HasOwnedItem("boomerang"));
                if (boomerangNeedsRepair)
                    EnsureBoomerangReceivedState();
                return boomerangNeedsRepair;
            }

            if (string.Equals(itemName, "Toadstool", StringComparison.Ordinal))
            {
                var toadstoolNeedsRepair = ShouldRepairToadstoolReceipt(
                    IsPersistentLocationCheckComplete(WitchLocationKey),
                    HasOwnedItem("toadstool"));
                if (toadstoolNeedsRepair)
                    _gameManager.CollectItem(new GameItemCollected("toadstool") { Count = 1 });
                return toadstoolNeedsRepair;
            }

            if (TryGetAmmoUpgradeReplay(itemName, out var upgradeEffect, out var appliedKey))
            {
                if (_gameManager.SaveManager.GetString(appliedKey, "0") == "1")
                    return false;

                ApplyEffect(upgradeEffect);
                return true;
            }

            if (!string.Equals(itemName, "BowWow", StringComparison.Ordinal))
                return false;

            var needsRepair = _gameManager.SaveManager.GetString(SaveBowWowReceived, "0") != "1" ||
                              _gameManager.SaveManager.GetString("has_bowWow", "0") != "1" ||
                              _gameManager.SaveManager.GetString("bowWow", "0") != "2";
            if (needsRepair)
                ApplyEffect(ArchipelagoItemEffect.BowWow);
            return needsRepair;
        }

        private void EnsureRoosterReceivedState()
        {
            ApplyEffect(ArchipelagoItemEffect.Rooster);
            if (!HasOwnedItem("rooster"))
                _gameManager.CollectItem(new GameItemCollected("rooster") { Count = 1 });
        }

        private void EnsureBoomerangReceivedState()
        {
            _gameManager.SaveManager.SetString(SaveBoomerangReceived, "1");
            _gameManager.SaveManager.SetString("store_boomerang", "1");
            if (!HasOwnedItem("boomerang"))
                _gameManager.CollectItem(new GameItemCollected("boomerang") { Count = 1 });
        }

        private void RepairBoomerangGuyTradeState()
        {
            var saveManager = _gameManager.SaveManager;
            var tradedItem = saveManager.GetString("tradded_item");
            if (tradedItem is "shovel" or "feather" or "magicRod" or "hookshot" &&
                ShouldRestoreBoomerangTradeItem(tradedItem, HasOwnedItem(tradedItem)))
            {
                _gameManager.CollectItem(new GameItemCollected(tradedItem) { Count = 1 });
                saveManager.SetString("store_" + tradedItem, "1");
            }

            ClearBoomerangTradeFlags();
        }

        private void ClearBoomerangTradeFlags()
        {
            _gameManager.SaveManager.RemoveString("boomerang_trade");
            _gameManager.SaveManager.RemoveString("boomerang_trade_return");
            _gameManager.SaveManager.RemoveString("tradded_item");
        }

        private void RepairTrendyGamePrizeState()
        {
            if (_seed?.LocationsByGameKey.TryGetValue(TrendyGameLocationKey, out var location) != true)
                return;

            var saveManager = _gameManager.SaveManager;
            if (!ShouldRepairTrendyPrize(
                    IsBoundSave,
                    saveManager.GetString("trade0Collected", "0"),
                    saveManager.GetString(ArchipelagoLocationKey.PersistentCheck(location.LocationId), "0")))
                return;

            // Older builds wrote trade0Collected as soon as the crane touched the randomized
            // prize. If the player quit before Link collected it, the object vanished forever.
            saveManager.RemoveString("trade0Collected");
            saveManager.RemoveString("trendy_5");
        }

        private int GetProgressiveReceiptCount(string itemName, int ownedLevel)
        {
            if (!TryGetProgressiveSaveKey(itemName, out var saveKey))
                return ownedLevel;

            var savedCount = _gameManager.SaveManager.GetInt(saveKey, 0);
            var replayedCount = GetReplayedProgressiveCount(itemName);
            var reconciledCount = ReconcileProgressiveCount(savedCount, replayedCount, ownedLevel);
            if (reconciledCount != savedCount)
                _gameManager.SaveManager.SetInt(saveKey, reconciledCount);
            return reconciledCount;
        }

        private void IncrementProgressiveReceiptCount(string itemName)
        {
            if (!TryGetProgressiveSaveKey(itemName, out var saveKey))
                return;

            var currentCount = _gameManager.SaveManager.GetInt(saveKey, 0);
            _gameManager.SaveManager.SetInt(saveKey, currentCount + 1);
        }

        private static bool TryGetProgressiveSaveKey(string itemName, out string saveKey)
        {
            saveKey = itemName switch
            {
                "Progressive Sword" => SaveProgressiveSwordCount,
                "Progressive Shield" => SaveProgressiveShieldCount,
                "Progressive Power Bracelet" => SaveProgressiveBraceletCount,
                _ => null
            };
            return saveKey != null;
        }

        private static bool TryGetAmmoUpgradeReplay(
            string itemName, out ArchipelagoItemEffect effect, out string appliedKey)
        {
            (effect, appliedKey) = itemName switch
            {
                "Max Powder Upgrade" =>
                    (ArchipelagoItemEffect.MaxPowderUpgrade, SaveMaxPowderRefillApplied),
                "Max Bombs Upgrade" =>
                    (ArchipelagoItemEffect.MaxBombsUpgrade, SaveMaxBombsRefillApplied),
                "Max Arrows Upgrade" =>
                    (ArchipelagoItemEffect.MaxArrowsUpgrade, SaveMaxArrowsRefillApplied),
                _ => (ArchipelagoItemEffect.None, null)
            };
            return appliedKey != null;
        }

        private int IncrementReplayedProgressiveCount(string itemName)
        {
            return itemName switch
            {
                "Progressive Sword" => ++_replayedProgressiveSwordCount,
                "Progressive Shield" => ++_replayedProgressiveShieldCount,
                "Progressive Power Bracelet" => ++_replayedProgressiveBraceletCount,
                _ => 0
            };
        }

        private int GetReplayedProgressiveCount(string itemName)
        {
            return itemName switch
            {
                "Progressive Sword" => _replayedProgressiveSwordCount,
                "Progressive Shield" => _replayedProgressiveShieldCount,
                "Progressive Power Bracelet" => _replayedProgressiveBraceletCount,
                _ => 0
            };
        }

        private void ResetReplayedProgressiveCounts()
        {
            _replayedProgressiveIndexes.Clear();
            _replayedProgressiveSwordCount = 0;
            _replayedProgressiveShieldCount = 0;
            _replayedProgressiveBraceletCount = 0;
        }

        private void ApplyStickEventState()
        {
            // The hive sequence advances Tarin to state 5. Never move him back to the hive if
            // that sequence (or a later story event) has already happened.
            var tarinState = GetStringState("tarin_state");
            if (tarinState > 4 || _gameManager.SaveManager.GetString("ow_honeycomb_fallen", "0") == "1")
                return;

            _gameManager.SaveManager.SetString("tarin", "5");
            _gameManager.SaveManager.SetString("tarin_state", "4");
        }

        private void ApplyPineappleEventState()
        {
            // State 2 means Papahl has already eaten the pineapple. Only establish the missing
            // pre-trade state so reconnects and old-save repairs cannot replay or undo the trade.
            if (GetStringState("npc_lost_boy_state") >= 2)
                return;

            if (GetStringState("maria_state") < 2)
            {
                _gameManager.SaveManager.SetString("maria_state", "2");
                _gameManager.SaveManager.SetString("maria", "6");
            }

            _gameManager.SaveManager.SetString("npc_lost_boy_state", "1");
            _gameManager.SaveManager.SetString("ow_npc_bag", "1");
            _gameManager.SaveManager.SetString("npc07", "1");
            _gameManager.SaveManager.SetString("spawned_npc_boy_2", "1");
        }

        private void PromoteStringState(string key, int minimum)
        {
            if (GetStringState(key) < minimum)
                _gameManager.SaveManager.SetString(key, minimum.ToString());
        }

        private void RepairTarinForestState(bool tarinGiftCompleted)
        {
            // Never replace state 2 (raccoon cured), state 4 (honeycomb ready), or any later
            // story state. The exact dialog state check also avoids moving Tarin out of the
            // opening house before his Archipelago location has been collected.
            if (GetStringState("tarin_state") != 0 ||
                (!tarinGiftCompleted && GetStringState("tarin") != 1))
                return;

            _gameManager.SaveManager.SetString("tarin_state", "1");
        }

        private bool IsPersistentLocationCheckComplete(string sourceLocationKey)
        {
            return _seed?.LocationsByGameKey.TryGetValue(sourceLocationKey, out var location) == true &&
                   _gameManager.SaveManager.GetString(
                       ArchipelagoLocationKey.PersistentCheck(location.LocationId)) == "1";
        }

        private int GetStringState(string key)
        {
            return int.TryParse(_gameManager.SaveManager.GetString(key, "0"), out var state) ? state : 0;
        }

        private bool HasOwnedItem(string itemName)
        {
            return _gameManager.Equipment.Any(item => item?.Name == itemName && item.Count > 0) ||
                   _gameManager.CollectedItems.Any(item => item?.Name == itemName && item.Count > 0);
        }

        private ArchipelagoSession GetConnectedSession()
        {
            lock (_sessionLock)
                return _session?.Socket.Connected == true ? _session : null;
        }

        private int GetOwnedEquipmentLevel(string levelOneItem, string levelTwoItem)
        {
            if (_gameManager.GetItem(levelTwoItem)?.Count > 0)
                return 2;
            return _gameManager.GetItem(levelOneItem)?.Count > 0 ? 1 : 0;
        }

        private bool ShouldAttemptReconnect()
        {
            var scheduledSilentDisconnect = false;
            var reconnectDelaySeconds = 0;
            lock (_sessionLock)
            {
                if (_connecting || _session?.Socket.Connected == true)
                    return false;

                // Some Android network changes leave the socket disconnected without raising
                // SocketClosed. A successful login set the deadline to MaxValue, which used to
                // suppress reconnect forever in that state until the entire game was restarted.
                if (_nextReconnectUtc == DateTime.MaxValue)
                {
                    var disconnectedSession = _session;
                    _session = null;
                    _connectionGeneration++;
                    reconnectDelaySeconds = GetReconnectDelaySeconds(++_consecutiveConnectionFailures);
                    _nextReconnectUtc = DateTime.UtcNow + TimeSpan.FromSeconds(reconnectDelaySeconds);
                    _sessionCleanupTask = CleanupSessionAsync(disconnectedSession);
                    scheduledSilentDisconnect = true;
                }

                if (!scheduledSilentDisconnect)
                    return _sessionCleanupTask.IsCompleted && DateTime.UtcNow >= _nextReconnectUtc;
            }

            RecordDisconnect(TelemetryDisconnectReason.Unknown);
            TelemetryManager.Client?.RecordReconnectScheduled(
                Math.Max(1, _telemetryConnectAttempts + 1), reconnectDelaySeconds);
            return false;
        }

        private void HandleCurrentSessionFailure(ArchipelagoSession failedSession, string status)
        {
            int generation;
            lock (_sessionLock)
            {
                if (!ReferenceEquals(_session, failedSession))
                    return;

                generation = _connectionGeneration;
            }

            HandleSocketFailure(generation, failedSession, TelemetryDisconnectReason.Network, status);
        }

        private void HandleSocketFailure(int generation, ArchipelagoSession failedSession,
            TelemetryDisconnectReason reason, string status)
        {
            int reconnectDelaySeconds;
            lock (_sessionLock)
            {
                if (generation != _connectionGeneration)
                    return;

                // Invalidate every callback belonging to this socket immediately. ErrorReceived
                // is not guaranteed to be followed by SocketClosed, and send failures can happen
                // while Socket.Connected still reports its last-known true state.
                _connectionGeneration++;
                _session = null;
                _connecting = false;
                reconnectDelaySeconds = GetReconnectDelaySeconds(++_consecutiveConnectionFailures);
                _nextReconnectUtc = DateTime.UtcNow + TimeSpan.FromSeconds(reconnectDelaySeconds);
                _sessionCleanupTask = CleanupSessionAsync(failedSession);
            }

            RecordDisconnect(reason);
            TelemetryManager.Client?.RecordReconnectScheduled(
                Math.Max(1, _telemetryConnectAttempts + 1), reconnectDelaySeconds);
            SetStatus(status);
        }

        private void Disconnect()
        {
            ArchipelagoSession oldSession;
            lock (_sessionLock)
            {
                _connectionGeneration++;
                _connecting = false;
                oldSession = _session;
                _session = null;
                _nextReconnectUtc = DateTime.MaxValue;
                _consecutiveConnectionFailures = 0;
                _sessionCleanupTask = CleanupSessionAsync(oldSession);
            }

            FinalizeConnectedPeriod();
        }

        private static Task CleanupSessionAsync(ArchipelagoSession session)
        {
            if (session == null)
                return Task.CompletedTask;

            // Run outside the socket callback. DisconnectAsync waits for the socket workers, and
            // awaiting it from one of those workers would otherwise make cleanup wait on itself.
            return Task.Run(async () =>
            {
                try
                {
                    await session.Socket.DisconnectAsync().ConfigureAwait(false);
                }
                catch
                {
                    // The patched socket helper already aborts and disposes before it awaits its
                    // workers, so an exception here cannot leave the old session alive.
                }
            });
        }

        private static TelemetryConnectionError ClassifyConnectionFailure(Exception exception)
        {
            var root = exception?.GetBaseException();
            if (root is TimeoutException)
                return TelemetryConnectionError.Timeout;

            return ClassifySocketFailure(root) == TelemetryDisconnectReason.Protocol
                ? TelemetryConnectionError.Protocol
                : TelemetryConnectionError.Network;
        }

        public void OnApplicationStopping()
        {
            _magpieTracker.Stop();
            ReportTelemetrySummary();
        }

        private void ResetTelemetrySession()
        {
            lock (_sessionLock)
            {
                _connectionAttemptStartedUtc = DateTime.MinValue;
                _connectedStartedUtc = DateTime.MinValue;
                _connectedDuration = TimeSpan.Zero;
                _telemetryConnectAttempts = 0;
                _telemetryDisconnects = 0;
                _telemetryReconnects = 0;
                _telemetryChecksReported = 0;
                _telemetryItemsReceived = 0;
                _telemetryUnsupportedItems = 0;
                _telemetrySummaryReported = false;
            }
        }

        private void RecordRandomizerManifest()
        {
            TelemetryManager.Client?.RecordRandomizerManifest(
                _seed.WorldVersion,
                GetLogicOption(),
                GetBooleanOption("tradequest"),
                GetBooleanOption("rooster"),
                GetBooleanOption("warp_to_start"));
        }

        private string GetLogicOption()
        {
            if (_seed.Options.TryGetValue("logic", out var logic))
            {
                if (logic.ValueKind == JsonValueKind.String)
                {
                    var value = logic.GetString()?.ToLowerInvariant();
                    if (value is "normal" or "hard" or "glitched" or "hell")
                        return value;
                }
                if (logic.ValueKind == JsonValueKind.Number && logic.TryGetInt32(out var choice))
                    return choice switch { 0 => "normal", 1 => "hard", 2 => "glitched", 3 => "hell", _ => "unknown" };
            }
            return "unknown";
        }

        private bool? GetBooleanOption(string name)
        {
            if (!_seed.Options.TryGetValue(name, out var option))
                return null;
            if (option.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return option.GetBoolean();
            if (option.ValueKind == JsonValueKind.Number && option.TryGetInt32(out var number))
                return number != 0;
            return null;
        }

        private void RecordDisconnect(TelemetryDisconnectReason reason)
        {
            var connectedSeconds = FinalizeConnectedPeriod();
            Interlocked.Increment(ref _telemetryDisconnects);
            TelemetryManager.Client?.RecordDisconnected(connectedSeconds, reason);
        }

        private int FinalizeConnectedPeriod()
        {
            lock (_sessionLock)
            {
                var period = TimeSpan.Zero;
                if (_connectedStartedUtc != DateTime.MinValue)
                {
                    period = DateTime.UtcNow - _connectedStartedUtc;
                    _connectedDuration += period;
                    _connectedStartedUtc = DateTime.MinValue;
                }
                return (int)Math.Min(period.TotalSeconds, 604800);
            }
        }

        private int GetTotalConnectedDuration()
        {
            lock (_sessionLock)
            {
                if (_connectedStartedUtc != DateTime.MinValue)
                {
                    _connectedDuration += DateTime.UtcNow - _connectedStartedUtc;
                    _connectedStartedUtc = DateTime.UtcNow;
                }
                return (int)Math.Min(_connectedDuration.TotalSeconds, 604800);
            }
        }

        private void ReportTelemetrySummary()
        {
            lock (_sessionLock)
            {
                if (_telemetrySummaryReported || !IsActive)
                    return;
                _telemetrySummaryReported = true;
            }

            TelemetryManager.Client?.RecordSessionSummary(
                GetTotalConnectedDuration(),
                Volatile.Read(ref _telemetryDisconnects),
                Volatile.Read(ref _telemetryReconnects),
                Volatile.Read(ref _telemetryChecksReported),
                Volatile.Read(ref _telemetryItemsReceived),
                Volatile.Read(ref _telemetryUnsupportedItems));
        }

        private static int ElapsedMilliseconds(DateTime startedUtc)
        {
            if (startedUtc == DateTime.MinValue)
                return 0;
            return (int)Math.Min(Math.Max((DateTime.UtcNow - startedUtc).TotalMilliseconds, 0), 3600000);
        }

        private static bool TryGetSaveKey(string gameKey, out string saveKey)
        {
            saveKey = null;
            if (string.IsNullOrWhiteSpace(gameKey))
                return false;

            var separator = gameKey.IndexOf(':');
            if (separator < 0 || separator == gameKey.Length - 1)
                return false;
            if (gameKey.Substring(0, separator) != "chest" && gameKey.Substring(0, separator) != "item")
                return false;

            saveKey = gameKey.Substring(separator + 1);
            return true;
        }

        private void SetStatus(string status)
        {
            _status = status;
            Debug.WriteLine($"[Archipelago] {status}");
        }

        private readonly struct QueuedNetworkItem
        {
            private QueuedNetworkItem(int index, long itemId, string itemName, string senderName)
            {
                Index = index;
                ItemId = itemId;
                ItemName = itemName;
                SenderName = senderName;
            }

            public int Index { get; }
            public long ItemId { get; }
            public string ItemName { get; }
            public string SenderName { get; }

            public static QueuedNetworkItem From(int index, ItemInfo item) => new QueuedNetworkItem(
                index,
                item.ItemId,
                item.ItemDisplayName,
                item.Player?.Name ?? $"player {item.Player?.Slot}");
        }
    }
}
