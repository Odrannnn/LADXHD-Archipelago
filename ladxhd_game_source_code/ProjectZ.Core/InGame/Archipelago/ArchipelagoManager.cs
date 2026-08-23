using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
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
        public static readonly Version ClientVersion = new Version(0, 6, 7);

        private const string SaveSeedName = "ap_seed_name";
        private const string SaveSlotName = "ap_slot_name";
        private const string SaveReceivedIndex = "ap_received_index";
        private const string SaveGoalPending = "ap_goal_pending";
        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

        private readonly GameManager _gameManager;
        private readonly object _sessionLock = new object();
        private readonly ConcurrentQueue<QueuedNetworkItem> _receivedItems = new ConcurrentQueue<QueuedNetworkItem>();
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private readonly HashSet<string> _cataloguedLocationKeys = new HashSet<string>(StringComparer.Ordinal);

        private ArchipelagoConnectionSettings _settings;
        private ArchipelagoSeedManifest _seed;
        private ArchipelagoSession _session;
        private bool _connecting;
        private int _connectionGeneration;
        private int _nextReceivedIndex;
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
        public string Status => _status;
        public ArchipelagoSeedManifest Seed => _seed;

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
            IsActive = false;
            _nextReceivedIndex = 0;
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

            ActivateBoundSave();
        }

        public void RepairBoundSaveBeforeMapLoad()
        {
            var saveSeed = _gameManager.SaveManager.GetString(SaveSeedName);
            var saveSlot = _gameManager.SaveManager.GetString(SaveSlotName);
            if (string.IsNullOrWhiteSpace(saveSeed) || string.IsNullOrWhiteSpace(saveSlot))
                return;

            // Older AP builds granted the first progressive sword without completing the
            // non-cutscene part of the beach sword event. Repair those saves before map objects
            // choose their music, otherwise every map continues to select the intro track.
            if (_gameManager.SwordLevel > 0 &&
                _gameManager.SaveManager.GetString("introMusic", "0") == "1")
                _gameManager.SaveManager.SetString("introMusic", "0");

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
            ResetTelemetrySession();
            _nextReceivedIndex = Math.Max(0, _gameManager.SaveManager.GetInt(SaveReceivedIndex, 0));
            SetStatus($"Bound: {_seed.SeedName} / {_seed.SlotName}");
            RecordRandomizerManifest();
            if (_settings.AutoConnect)
                Connect();
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

            if (!IsActive || Game1.UiManager.CurrentScreen != Values.ScreenNameGame ||
                _gameManager.MapManager.CurrentMap == null || MapManager.ObjLink == null ||
                _gameManager.SaveManager.HistoryEnabled)
                return;

            while (_receivedItems.TryPeek(out var queued) && queued.Index < _nextReceivedIndex)
                _receivedItems.TryDequeue(out _);

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
            Interlocked.Increment(ref _telemetryChecksReported);

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
                if (_connecting || _session?.Socket.Connected == true)
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
            SetStatus($"Connecting to {settings.Server}...");
            _ = Task.Run(() => ConnectWorker(generation, attempt, settings, seed));
        }

        private void ConnectWorker(int generation, int attempt, ArchipelagoConnectionSettings settings,
            ArchipelagoSeedManifest seed)
        {
            ArchipelagoSession newSession = null;
            var errorCategory = TelemetryConnectionError.Network;
            try
            {
                newSession = ArchipelagoSessionFactory.CreateSession(settings.Server);
                newSession.Socket.ErrorReceived += (_, message) => HandleSocketFailure(
                    generation, newSession, TelemetryDisconnectReason.Network,
                    $"Network error: {message}; reconnecting");
                newSession.Socket.SocketClosed += reason => HandleSocketFailure(
                    generation, newSession, TelemetryDisconnectReason.Server,
                    $"Disconnected: {reason}; reconnecting");
                newSession.Items.ItemReceived += helper =>
                {
                    var item = helper.DequeueItem();
                    if (item != null && generation == Volatile.Read(ref _connectionGeneration) && IsActive)
                        _receivedItems.Enqueue(QueuedNetworkItem.From(helper.Index - 1, item));
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
                        _ = newSession.Socket.DisconnectAsync();
                        return;
                    }
                    _session = newSession;
                    _connecting = false;
                    _nextReconnectUtc = DateTime.MaxValue;
                    durationMs = ElapsedMilliseconds(_connectionAttemptStartedUtc);
                    _connectionAttemptStartedUtc = DateTime.MinValue;
                    _connectedStartedUtc = DateTime.UtcNow;
                }

                TelemetryManager.Client?.RecordConnectSuccess(attempt, durationMs, seed.WorldVersion);

                _mainThreadActions.Enqueue(() =>
                {
                    if (generation != Volatile.Read(ref _connectionGeneration) || !IsActive)
                        return;
                    SetStatus($"Connected: {seed.SlotName}");
                    ResubmitCheckedLocations();
                    if (_gameManager.SaveManager.GetString(SaveGoalPending, "0") == "1")
                        ReportGoal();
                });
            }
            catch (Exception ex)
            {
                var isCurrentGeneration = false;
                lock (_sessionLock)
                {
                    if (generation == _connectionGeneration)
                    {
                        isCurrentGeneration = true;
                        _connecting = false;
                        _nextReconnectUtc = DateTime.UtcNow + ReconnectDelay;
                    }
                }
                if (newSession?.Socket.Connected == true)
                    _ = newSession.Socket.DisconnectAsync();
                if (isCurrentGeneration)
                {
                    TelemetryManager.Client?.RecordConnectFailure(
                        attempt, ElapsedMilliseconds(_connectionAttemptStartedUtc), errorCategory);
                    TelemetryManager.Client?.RecordReconnectScheduled(attempt + 1, (int)ReconnectDelay.TotalSeconds);
                    SetStatus($"Connection failed: {ex.Message}");
                }
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

        private bool TryApplyReceivedItem(QueuedNetworkItem queued)
        {
            if (!ArchipelagoItemMapper.TryMap(queued.ItemName, GetOwnedEquipmentLevel("sword1", "sword2"),
                    GetOwnedEquipmentLevel("shield", "mirrorShield"),
                    GetOwnedEquipmentLevel("stonelifter", "stonelifter2"), out var mapping))
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
                GrantReceivedItem(mapping, receivedItem)))
                return false;

            AchievementOverlay.PushArchipelagoItem("Received", queued.ItemName, "From", queued.SenderName);
            SetStatus($"Received: {queued.ItemName} from {queued.SenderName}");
            Interlocked.Increment(ref _telemetryItemsReceived);
            return true;
        }

        private void GrantReceivedItem(ArchipelagoItemMapping mapping, GameItemCollected receivedItem)
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
                    _gameManager.SaveManager.SetString("bowWow", "2");
                    _gameManager.SaveManager.SetString("has_bowWow", "1");
                    break;
                case ArchipelagoItemEffect.Rooster:
                    // The vanilla rooster pickup dialog normally writes these story flags.
                    // Remote AP items deliberately skip local pickup scripts, so mirror the
                    // persistent ownership state here while leaving the grave location's own
                    // save key untouched until that check is actually completed.
                    _gameManager.SaveManager.SetString("chicken_dude", "1");
                    _gameManager.SaveManager.SetString("has_rooster", "1");
                    _gameManager.SaveManager.SetString("ulrira_d7", "2");
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
                case ArchipelagoItemEffect.MaxPowderUpgrade:
                    _gameManager.SaveManager.SetString("upgradePowder", "1");
                    break;
                case ArchipelagoItemEffect.MaxBombsUpgrade:
                    _gameManager.SaveManager.SetString("upgradeBomb", "1");
                    break;
                case ArchipelagoItemEffect.MaxArrowsUpgrade:
                    _gameManager.SaveManager.SetString("upgradeBow", "1");
                    break;
            }
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
            var detectedSilentDisconnect = false;
            lock (_sessionLock)
            {
                if (_connecting || _session?.Socket.Connected == true)
                    return false;

                // Some Android network changes leave the socket disconnected without raising
                // SocketClosed. A successful login set the deadline to MaxValue, which used to
                // suppress reconnect forever in that state until the entire game was restarted.
                if (_nextReconnectUtc == DateTime.MaxValue)
                {
                    _session = null;
                    _connectionGeneration++;
                    _nextReconnectUtc = DateTime.UtcNow;
                    detectedSilentDisconnect = true;
                }

                var shouldReconnect = DateTime.UtcNow >= _nextReconnectUtc;
                if (!detectedSilentDisconnect)
                    return shouldReconnect;
            }

            RecordDisconnect(TelemetryDisconnectReason.Unknown);
            TelemetryManager.Client?.RecordReconnectScheduled(
                Math.Max(1, _telemetryConnectAttempts + 1), 0);
            return true;
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
                _nextReconnectUtc = DateTime.UtcNow + ReconnectDelay;
            }

            if (failedSession?.Socket.Connected == true)
                _ = failedSession.Socket.DisconnectAsync();
            RecordDisconnect(reason);
            TelemetryManager.Client?.RecordReconnectScheduled(
                Math.Max(1, _telemetryConnectAttempts + 1), (int)ReconnectDelay.TotalSeconds);
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
            }

            if (oldSession?.Socket.Connected == true)
                _ = oldSession.Socket.DisconnectAsync();
            FinalizeConnectedPeriod();
        }

        public void OnApplicationStopping()
        {
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
