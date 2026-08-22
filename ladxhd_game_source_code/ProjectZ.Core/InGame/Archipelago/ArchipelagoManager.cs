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
            _nextReceivedIndex = Math.Max(0, _gameManager.SaveManager.GetInt(SaveReceivedIndex, 0));
            SetStatus($"Bound: {_seed.SeedName} / {_seed.SlotName}");
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
                    SetStatus($"Location queued for reconnect: {ex.Message}");
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
                ArchipelagoItemMapper.TryMap(location.ItemName, _gameManager.SwordLevel, _gameManager.ShieldLevel,
                    _gameManager.StoneGrabberLevel, out var mapping) &&
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
                SetStatus($"Goal queued for reconnect: {ex.Message}");
            }
        }

        public void Connect()
        {
            if (!IsActive || !IsConfigured)
                return;

            int generation;
            ArchipelagoConnectionSettings settings;
            ArchipelagoSeedManifest seed;
            lock (_sessionLock)
            {
                if (_connecting || _session?.Socket.Connected == true)
                    return;
                _connecting = true;
                generation = ++_connectionGeneration;
                settings = _settings;
                seed = _seed;
            }

            SetStatus($"Connecting to {settings.Server}...");
            _ = Task.Run(() => ConnectWorker(generation, settings, seed));
        }

        private void ConnectWorker(int generation, ArchipelagoConnectionSettings settings,
            ArchipelagoSeedManifest seed)
        {
            ArchipelagoSession newSession = null;
            try
            {
                newSession = ArchipelagoSessionFactory.CreateSession(settings.Server);
                newSession.Socket.ErrorReceived += (_, message) =>
                {
                    if (generation == Volatile.Read(ref _connectionGeneration))
                        SetStatus($"Network error: {message}");
                };
                newSession.Socket.SocketClosed += reason => HandleSocketClosed(generation, newSession, reason);
                newSession.Items.ItemReceived += helper =>
                {
                    var item = helper.DequeueItem();
                    if (item != null && generation == Volatile.Read(ref _connectionGeneration) && IsActive)
                        _receivedItems.Enqueue(QueuedNetworkItem.From(helper.Index - 1, item));
                };

                var login = newSession.TryConnectAndLogin(GameName, settings.Slot, ItemsHandlingFlags.AllItems,
                    version: ClientVersion, password: settings.Password, requestSlotData: true);
                if (login is LoginFailure failure)
                    throw new InvalidOperationException(string.Join("; ", failure.Errors));

                var successful = (LoginSuccessful)login;
                if (successful.SlotData.TryGetValue("seed_name", out var serverSeed) &&
                    !string.Equals(serverSeed?.ToString(), seed.SeedName, StringComparison.Ordinal))
                    throw new InvalidDataException($"Server seed '{serverSeed}' does not match '{seed.SeedName}'.");

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
                }

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
                        _nextReconnectUtc = DateTime.UtcNow.AddSeconds(5);
                    }
                }
                if (newSession?.Socket.Connected == true)
                    _ = newSession.Socket.DisconnectAsync();
                if (isCurrentGeneration)
                    SetStatus($"Connection failed: {ex.Message}");
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

            if (checkedIds.Count > 0)
                session.Locations.CompleteLocationChecks(checkedIds.Distinct().ToArray());
        }

        private bool TryApplyReceivedItem(QueuedNetworkItem queued)
        {
            if (!ArchipelagoItemMapper.TryMap(queued.ItemName, _gameManager.SwordLevel, _gameManager.ShieldLevel,
                    _gameManager.StoneGrabberLevel, out var mapping))
            {
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
            return true;
        }

        private void GrantReceivedItem(ArchipelagoItemMapping mapping, GameItemCollected receivedItem)
        {
            ApplyEffect(mapping.Effect);
            if (receivedItem == null)
                return;

            var isFirstSword = mapping.GameItemName == "sword1" && _gameManager.SwordLevel == 0;
            var slot = -1;
            if (mapping.GameItemName == "sword2" && _gameManager.SwordLevel == 1)
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

        private ArchipelagoSession GetConnectedSession()
        {
            lock (_sessionLock)
                return _session?.Socket.Connected == true ? _session : null;
        }

        private bool ShouldAttemptReconnect()
        {
            lock (_sessionLock)
                return !_connecting && _session?.Socket.Connected != true && DateTime.UtcNow >= _nextReconnectUtc;
        }

        private void HandleSocketClosed(int generation, ArchipelagoSession closedSession, string reason)
        {
            lock (_sessionLock)
            {
                if (generation != _connectionGeneration)
                    return;
                if (ReferenceEquals(_session, closedSession))
                    _session = null;
                _connecting = false;
                _nextReconnectUtc = DateTime.UtcNow.AddSeconds(5);
            }
            SetStatus($"Disconnected: {reason}; reconnecting");
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
