using Logic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    /// <summary>
    /// Tutorial system for new player guidance.
    /// Manages tutorial phases and triggers appropriate hints.
    /// Supports parallel tasks within the Explore phase.
    /// </summary>
    public class Tutorial
    {
        #region Singleton

        private static Tutorial instance;
        public static Tutorial Instance { get { if (instance == null) { instance = new Tutorial(); } return instance; } }

        #endregion

        #region Constants

        /// <summary>
        /// Tutorial phase identifiers (linear progression)
        /// </summary>
        public enum Phase
        {
            None = 0,
            WalkToSand = 1,      // Guide player to walk to sand map
            Explore = 2,         // Parallel: interact with gold mine AND defeat lizard (order flexible)
            Collect = 3,         // Collect dropped items (gold ore + raw meat)
            Offering = 4,        // Find stele and give items
            Completed = 100      // Tutorial completed
        }

        /// <summary>
        /// Explore phase sub-tasks (can be completed in any order)
        /// </summary>
        [Flags]
        public enum ExploreTask
        {
            None = 0,
            GoldMine = 1 << 0,   // Interact with gold mine
            Lizard = 1 << 1,     // Defeat lizard
            All = GoldMine | Lizard
        }

        /// <summary>
        /// Current action within explore task
        /// </summary>
        public enum ExploreAction
        {
            None = 0,
            SeeTarget = 1,       // Player sees target, guide to go
            AtTarget = 2,        // Player at target, guide to interact/attack
        }

        /// <summary>
        /// Target types for tutorial highlighting
        /// </summary>
        public enum TargetType
        {
            UI = 1,
            Map = 2,
            Creature = 3,
            Item = 4
        }

        // Database record keys for persistence
        private const string RecordPhase = "TutorialPhase";
        private const string RecordExploreCompleted = "TutorialExplore";

        // Cached config IDs (resolved at runtime)
        private int _tutorialShoreMapId;
        private int _tutorialSandMapId;
        private int _tutorialTowerMapId;
        private int _goldOreItemId;
        private int _rawMeatItemId;
        private int _goldMineItemId;
        private int _lizardLifeId;
        private int _steleItemId;

        #endregion

        #region State Storage

        /// <summary>
        /// Player tutorial state
        /// </summary>
        private class PlayerState
        {
            public Phase Phase { get; set; } = Phase.None;
            public ExploreTask ExploreCompleted { get; set; } = ExploreTask.None;
            public ExploreTask CurrentExploreTarget { get; set; } = ExploreTask.None;
            public ExploreAction CurrentAction { get; set; } = ExploreAction.None;
            public bool IsTraveling { get; set; } = false;
            public bool JustAdvanced { get; set; } = false;
        }

        // Player states (player hash -> state)
        private Dictionary<int, PlayerState> _playerStates = new Dictionary<int, PlayerState>();

        // Track which players have map change listeners registered
        private HashSet<int> _registeredPlayers = new HashSet<int>();

        #endregion

        #region Initialization

        public void Init()
        {
            // Cache config IDs for runtime lookup
            CacheConfigIds();

            // Register event listeners on Logic.Agent singleton (global scope)
            Logic.Agent.Instance.monitor.Register(Logic.Player.Event.StoryComplete, OnStoryComplete);
            Logic.Agent.Instance.monitor.Register(Logic.Item.Event.Used, OnItemUsed);
            Logic.Agent.Instance.monitor.Register(Logic.Item.Event.Picked, OnItemPicked);
            Logic.Agent.Instance.monitor.Register(Logic.Character.Event.Given, OnCharacterGiven);
            Logic.Agent.Instance.monitor.Register(Logic.Life.Event.Die, OnLifeDie);
            Logic.Agent.Instance.monitor.Register(Logic.Character.Event.GoTo, OnCharacterGoTo);

            // Register for first-seen events from Perception
            Perception.Agent.Instance.FirstSeen += OnFirstSeen;
        }

        private void CacheConfigIds()
        {
            // Lookup Design layer configs for cid
            var tutorialShore = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Map>(m => m.cid == "遗迹-岸边");
            _tutorialShoreMapId = tutorialShore?.id ?? 0;

            var tutorialSand = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Map>(m => m.cid == "遗迹-沙地");
            _tutorialSandMapId = tutorialSand?.id ?? 0;

            var tutorialTower = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Map>(m => m.cid == "遗迹-通天塔");
            _tutorialTowerMapId = tutorialTower?.id ?? 0;

            var goldOre = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Item>(i => i.cid == "金矿石");
            _goldOreItemId = goldOre?.id ?? 0;

            var rawMeat = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Item>(i => i.cid == "生肉");
            _rawMeatItemId = rawMeat?.id ?? 0;

            var goldMine = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Item>(i => i.cid == "金矿");
            _goldMineItemId = goldMine?.id ?? 0;

            var lizard = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Life>(l => l.cid == "蜥蜴");
            _lizardLifeId = lizard?.id ?? 0;

            var stele = Logic.Design.Agent.Instance.Content.Get<Logic.Design.Item>(i => i.cid == "石碑");
            _steleItemId = stele?.id ?? 0;

            Utils.Debug.Log.Info("TUTORIAL", $"[CacheConfigIds] shore={_tutorialShoreMapId}, sand={_tutorialSandMapId}, tower={_tutorialTowerMapId}, goldOre={_goldOreItemId}, rawMeat={_rawMeatItemId}, goldMine={_goldMineItemId}, lizard={_lizardLifeId}, stele={_steleItemId}");
        }

        /// <summary>
        /// Register map change listener for a player (ensures only registered once)
        /// </summary>
        private void RegisterPlayerMapListener(Player player)
        {
            int playerHash = player.GetHashCode();
            if (_registeredPlayers.Contains(playerHash))
            {
                return;
            }

            _registeredPlayers.Add(playerHash);
            Utils.Debug.Log.Info("TUTORIAL", $"[RegisterPlayerMapListener] Registering map change listener for player");

            // Register for map changes using closure to capture player reference
            player.data.after.Register(Basic.Element.Data.Parent, (object[] parentArgs) =>
            {
                OnPlayerMoved(player);
            });
        }

        #endregion

        #region Public API

        /// <summary>
        /// Tutorial entry point (called by Login.CompleteLogin for all players).
        /// Detects saved progress and handles: None=new start, incomplete=recovery, Complete=skip.
        /// </summary>
        public void Start(Player player)
        {
            if (player == null) return;

            // Load saved phase from database
            Phase savedPhase = LoadProgress(player);
            Utils.Debug.Log.Info("TUTORIAL", $"[Start] Player phase={savedPhase}");

            // Skip if tutorial already completed
            if (savedPhase == Phase.Completed)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[Start] Tutorial already completed, unlocking all UI");
                Display.Agent.Instance.UnlockAllHomePanels(player);
                return;
            }

            // Create tutorial copy (for both new players and recovery)
            if (savedPhase == Phase.None || (int)savedPhase > 0 && (int)savedPhase < 100)
            {
                var copy = CreateTutorialCopy(player);
                if (copy == null)
                {
                    Utils.Debug.Log.Warning("TUTORIAL", "[Start] Failed to create tutorial copy, skipping tutorial");
                    return;
                }

                // Register map change listener
                RegisterPlayerMapListener(player);

                // Initialize or restore state
                var state = GetOrCreateState(player);

                if (savedPhase == Phase.None)
                {
                    // New player: start from beginning
                    Utils.Debug.Log.Info("TUTORIAL", $"[Start] New player, starting tutorial");
                    Display.Agent.Instance.InitializeUILock(player);
                    state.Phase = Phase.WalkToSand;
                    copy.Start.AddAsParent(player);
                }
                else
                {
                    // Recovery: restore to saved phase
                    Utils.Debug.Log.Info("TUTORIAL", $"[Start] Recovering tutorial from phase={savedPhase}");
                    state.Phase = savedPhase;
                    state.ExploreCompleted = (ExploreTask)player.Database.GetRecord(RecordExploreCompleted);

                    // Find map matching player's saved position and place them there
                    PlacePlayerInCopy(player, copy);
                }

                // Save and send hint
                SaveProgress(player, state);
                SendPhaseHint(player, state);

                // Check if already at targets
                if (state.Phase == Phase.WalkToSand)
                {
                    CheckInitialVisibility(player, state);
                }
                else if (state.Phase == Phase.Explore)
                {
                    SelectNearestExploreTarget(player, state);
                }
            }
        }

        /// <summary>
        /// Create tutorial copy for a player.
        /// </summary>
        private Logic.Copy CreateTutorialCopy(Player player)
        {
            if (_tutorialShoreMapId == 0)
            {
                Utils.Debug.Log.Warning("TUTORIAL", "[CreateTutorialCopy] Shore map ID not cached");
                return null;
            }

            // Find the world tutorial shore map
            var tutorialMap = Logic.Agent.Instance.Content.Get<Logic.Map>(m =>
                m.Config.Id == _tutorialShoreMapId && m.Copy == null);

            if (tutorialMap?.Scene == null)
            {
                Utils.Debug.Log.Warning("TUTORIAL", "[CreateTutorialCopy] Tutorial shore map not found");
                return null;
            }

            // Save player's initial city position before entering tutorial
            var initialPos = player.Database.pos;

            // Create copy config with character placement rules
            var copyConfig = new Logic.Config.Quest.Copy
            {
                scope = 5,
                characters = new Dictionary<int, List<Logic.Config.Quest.Character>>()
            };

            // Add characters for sand map (gold mine and lizard)
            if (_tutorialSandMapId > 0)
            {
                var sandCharacters = new List<Logic.Config.Quest.Character>();
                if (_goldMineItemId > 0)
                {
                    sandCharacters.Add(new Logic.Config.Quest.Character { id = _goldMineItemId, count = 1 });
                }
                if (_lizardLifeId > 0)
                {
                    sandCharacters.Add(new Logic.Config.Quest.Character { id = _lizardLifeId, count = 1, min = 1, max = 1 });
                }
                if (sandCharacters.Count > 0)
                {
                    copyConfig.characters[_tutorialSandMapId] = sandCharacters;
                }
            }

            // Add stele for tower map
            if (_tutorialTowerMapId > 0 && _steleItemId > 0)
            {
                copyConfig.characters[_tutorialTowerMapId] = new List<Logic.Config.Quest.Character>
                {
                    new Logic.Config.Quest.Character { id = _steleItemId, count = 1 }
                };
            }

            // Create the copy
            var copy = Logic.Agent.Instance.Create<Logic.Copy>(tutorialMap, copyConfig);
            if (copy != null)
            {
                // Override Teleport with player's initial city position
                copy.Teleport = initialPos;
            }

            return copy;
        }

        /// <summary>
        /// Place player in the copy at their saved position.
        /// </summary>
        private void PlacePlayerInCopy(Player player, Logic.Copy copy)
        {
            var savedPos = player.Database.pos;
            if (savedPos == null || savedPos.Length < 3)
            {
                // Fallback to copy start
                copy.Start.AddAsParent(player);
                return;
            }

            // Find map in copy matching saved position
            var targetMap = copy.Content.Get<Logic.Map>(m =>
                m.Database.pos != null && System.Linq.Enumerable.SequenceEqual(m.Database.pos, savedPos));

            if (targetMap != null)
            {
                targetMap.AddAsParent(player);
                Utils.Debug.Log.Info("TUTORIAL", $"[PlacePlayerInCopy] Placed at saved position [{string.Join(",", savedPos)}]");
            }
            else
            {
                // Fallback to copy start
                copy.Start.AddAsParent(player);
                Utils.Debug.Log.Info("TUTORIAL", $"[PlacePlayerInCopy] Saved position not found, placed at start");
            }
        }

        /// <summary>
        /// Get current tutorial phase for player
        /// </summary>
        public Phase GetCurrentPhase(Player player)
        {
            if (player == null) return Phase.None;
            return _playerStates.TryGetValue(player.GetHashCode(), out var state) ? state.Phase : Phase.None;
        }

        /// <summary>
        /// Check if player is in tutorial
        /// </summary>
        public bool IsInTutorial(Player player)
        {
            var phase = GetCurrentPhase(player);
            return phase != Phase.None && phase != Phase.Completed;
        }

        /// <summary>
        /// Load tutorial progress from database (for data analysis, not restoration)
        /// </summary>
        public Phase LoadProgress(Player player)
        {
            int phaseValue = player.Database.GetRecord(RecordPhase);
            return Enum.IsDefined(typeof(Phase), phaseValue) ? (Phase)phaseValue : Phase.None;
        }

        /// <summary>
        /// Complete tutorial and teleport to random city
        /// </summary>
        public void Complete(Player player)
        {
            if (player == null) return;

            int playerHash = player.GetHashCode();

            // Update state
            var state = GetOrCreateState(player);
            state.Phase = Phase.Completed;
            SaveProgress(player, state);
            
            // Unlock all UI
            Display.Agent.Instance.UnlockAllHomePanels(player);

            Utils.Debug.Log.Info("TUTORIAL", $"[Complete] Tutorial completed for player");

            // Exit copy if in one
            if (player.Map?.Copy != null)
            {
                var copy = player.Map.Copy;
                copy.Release();
            }

            // Teleport to random city
            var destination = Logic.SpawnPoint.GetRandomInitialMap();
            if (destination != null)
            {
                Move.Agent.Do(player, destination);
            }

            // Clean up memory state (keep database record for analytics)
            _playerStates.Remove(playerHash);
            _registeredPlayers.Remove(playerHash);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Global event callback: Item used (interaction)
        /// </summary>
        private void OnItemUsed(params object[] args)
        {
            if (args.Length < 2) return;
            var player = args[0] as Player;
            var item = args[1] as Item;
            if (player == null || item == null) return;

            // Filter: only gold mine
            if (item.Config?.Id != _goldMineItemId) return;

            var state = GetState(player);
            if (state == null || state.Phase != Phase.Explore) return;
            if (state.CurrentExploreTarget != ExploreTask.GoldMine) return;
            if (state.CurrentAction != ExploreAction.AtTarget) return;

            Utils.Debug.Log.Info("TUTORIAL", $"[OnItemUsed] Gold mine interaction completed");

            state.ExploreCompleted |= ExploreTask.GoldMine;
            state.CurrentExploreTarget = ExploreTask.None;
            state.CurrentAction = ExploreAction.None;

            CheckExploreCompletion(player, state);
        }

        /// <summary>
        /// Global event callback: Item picked up
        /// </summary>
        private void OnItemPicked(params object[] args)
        {
            if (args.Length < 2) return;
            var player = args[0] as Player;
            var item = args[1] as Item;
            if (player == null) return;

            var state = GetState(player);
            if (state == null || state.Phase != Phase.Collect) return;

            // Check if player has both gold ore and raw meat
            bool hasGoldOre = player.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
            bool hasRawMeat = player.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);

            if (hasGoldOre && hasRawMeat)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnItemPicked] All items collected, advancing to Offering");
                AdvancePhase(player, state, Phase.Offering);
            }
        }

        /// <summary>
        /// Global event callback: Character given item
        /// </summary>
        private void OnCharacterGiven(params object[] args)
        {
            if (args.Length < 3) return;
            var player = args[0] as Player;
            var givenItem = args[1] as Item;
            var receiver = args[2] as Item;
            if (player == null || receiver == null) return;

            // Filter: only stele
            if (receiver.Config?.Id != _steleItemId) return;

            var state = GetState(player);
            if (state == null || state.Phase != Phase.Offering) return;

            // Check if stele has required items
            bool hasGoldOre = receiver.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
            bool hasRawMeat = receiver.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);

            if (hasGoldOre && hasRawMeat)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnCharacterGiven] Items given to stele, playing story");
                PlayTutorialStory(player);
            }
        }

        /// <summary>
        /// Global event callback: Life died (defeated)
        /// </summary>
        private void OnLifeDie(params object[] args)
        {
            if (args.Length < 2) return;
            var player = args[0] as Player;
            var life = args[1] as Life;
            if (player == null || life == null) return;

            // Filter: only lizard
            if (life.Config?.Id != _lizardLifeId) return;

            var state = GetState(player);
            if (state == null || state.Phase != Phase.Explore) return;
            if (state.CurrentExploreTarget != ExploreTask.Lizard) return;
            if (state.CurrentAction != ExploreAction.AtTarget) return;

            Utils.Debug.Log.Info("TUTORIAL", $"[OnLifeDie] Lizard defeated");

            state.ExploreCompleted |= ExploreTask.Lizard;
            state.CurrentExploreTarget = ExploreTask.None;
            state.CurrentAction = ExploreAction.None;

            CheckExploreCompletion(player, state);
        }

        /// <summary>
        /// Global event callback: Player going to character
        /// </summary>
        private void OnCharacterGoTo(params object[] args)
        {
            if (args.Length < 2) return;
            var player = args[0] as Player;
            var target = args[1] as Character;
            if (player == null || target == null) return;

            var state = GetState(player);
            if (state == null) return;

            int targetConfigId = Perception.Agent.GetCharacterConfigId(target);

            if (state.Phase == Phase.Explore && state.CurrentAction == ExploreAction.SeeTarget)
            {
                bool isCurrentTarget =
                    (state.CurrentExploreTarget == ExploreTask.GoldMine && targetConfigId == _goldMineItemId) ||
                    (state.CurrentExploreTarget == ExploreTask.Lizard && targetConfigId == _lizardLifeId);

                if (isCurrentTarget)
                {
                    Utils.Debug.Log.Info("TUTORIAL", $"[OnCharacterGoTo] Player going to explore target");
                    state.IsTraveling = true;
                    ClearHint(player);
                }
            }
            else if (state.Phase == Phase.Offering && targetConfigId == _steleItemId)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnCharacterGoTo] Player going to stele");
                state.IsTraveling = true;
                ClearHint(player);
            }
        }

        private void OnFirstSeen(Player player, Character character)
        {
            if (player == null || character == null) return;

            var state = GetState(player);
            if (state == null || state.Phase == Phase.None || state.Phase == Phase.Completed) return;

            int configId = Perception.Agent.GetCharacterConfigId(character);

            switch (state.Phase)
            {
                case Phase.WalkToSand:
                    // Seeing gold mine or lizard means we should enter Explore phase
                    if (configId == _goldMineItemId || configId == _lizardLifeId)
                    {
                        Utils.Debug.Log.Info("TUTORIAL", $"[OnFirstSeen] Saw explore target at WalkToSand, advancing to Explore");
                        AdvancePhase(player, state, Phase.Explore);
                    }
                    break;

                case Phase.Explore:
                    // If we don't have a current target, find the nearest visible one
                    if (state.CurrentExploreTarget == ExploreTask.None)
                    {
                        SelectNearestExploreTarget(player, state);
                    }
                    break;

                case Phase.Collect:
                    // If we see the stele, we can skip to Offering
                    if (configId == _steleItemId)
                    {
                        bool hasGoldOre = player.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
                        bool hasRawMeat = player.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);
                        if (hasGoldOre && hasRawMeat)
                        {
                            Utils.Debug.Log.Info("TUTORIAL", $"[OnFirstSeen] Saw stele with items, advancing to Offering");
                            AdvancePhase(player, state, Phase.Offering);
                        }
                    }
                    break;
            }
        }

        private void OnPlayerMoved(Player player)
        {
            if (player == null) return;

            var state = GetState(player);
            if (state == null || state.Phase == Phase.None || state.Phase == Phase.Completed) return;

            var currentMap = player.Map;
            if (currentMap == null) return;

            switch (state.Phase)
            {
                case Phase.WalkToSand:
                    if (currentMap.Config.Id == _tutorialSandMapId)
                    {
                        Utils.Debug.Log.Info("TUTORIAL", $"[OnPlayerMoved] Arrived at sand map, advancing to Explore");
                        AdvancePhase(player, state, Phase.Explore);
                    }
                    break;

                case Phase.Explore:
                    HandleExploreMoved(player, state);
                    break;

                case Phase.Offering:
                    HandleOfferingMoved(player, state);
                    break;
            }
        }

        private void HandleExploreMoved(Player player, PlayerState state)
        {
            Utils.Debug.Log.Info("TUTORIAL", $"[HandleExploreMoved] target={state.CurrentExploreTarget}, action={state.CurrentAction}, traveling={state.IsTraveling}");

            if (state.CurrentExploreTarget == ExploreTask.None)
            {
                // No target selected, try to find one
                SelectNearestExploreTarget(player, state);
                return;
            }

            // Check if player arrived at current target
            int targetConfigId = state.CurrentExploreTarget == ExploreTask.GoldMine ? _goldMineItemId : _lizardLifeId;
            bool atLocation = IsAtCharacterLocation(player, targetConfigId);
            Utils.Debug.Log.Info("TUTORIAL", $"[HandleExploreMoved] targetConfigId={targetConfigId}, atLocation={atLocation}");

            if (state.CurrentAction == ExploreAction.SeeTarget)
            {
                if (atLocation)
                {
                    Utils.Debug.Log.Info("TUTORIAL", $"[HandleExploreMoved] Arrived at {state.CurrentExploreTarget}, sending AtTarget hint");
                    state.CurrentAction = ExploreAction.AtTarget;
                    state.IsTraveling = false;
                    SendExploreHint(player, state);
                }
                else if (!state.IsTraveling)
                {
                    // Check visibility
                    bool isVisible = CanSeeCharacter(player, targetConfigId);
                    if (!isVisible && !state.JustAdvanced)
                    {
                        // Target no longer visible, clear hint and try to find another
                        ClearHint(player);
                        state.CurrentExploreTarget = ExploreTask.None;
                        state.CurrentAction = ExploreAction.None;
                        SelectNearestExploreTarget(player, state);
                    }
                }
            }
            else if (state.CurrentAction == ExploreAction.AtTarget)
            {
                // Player was at target but may have moved away
                if (!atLocation)
                {
                    Utils.Debug.Log.Info("TUTORIAL", $"[HandleExploreMoved] Left target location, checking visibility");
                    bool isVisible = CanSeeCharacter(player, targetConfigId);
                    
                    // Clear previous hint when leaving target location
                    ClearHint(player);
                    
                    if (isVisible)
                    {
                        // Target still visible, keep AtTarget state and wait for interaction
                        Utils.Debug.Log.Info("TUTORIAL", $"[HandleExploreMoved] Target still visible, keeping AtTarget state, hint cleared");
                    }
                    else
                    {
                        // No longer visible, find new target
                        Utils.Debug.Log.Info("TUTORIAL", $"[HandleExploreMoved] Target no longer visible, finding new target");
                        state.CurrentExploreTarget = ExploreTask.None;
                        state.CurrentAction = ExploreAction.None;
                        SelectNearestExploreTarget(player, state);
                    }
                }
            }

            state.JustAdvanced = false;
        }

        private void HandleOfferingMoved(Player player, PlayerState state)
        {
            // Check if player arrived at stele
            if (IsAtCharacterLocation(player, _steleItemId))
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[HandleOfferingMoved] Arrived at stele");
                state.IsTraveling = false;
                SendOfferingHint(player, true);
            }
            else if (!state.IsTraveling)
            {
                // Check if stele is visible
                bool isVisible = CanSeeCharacter(player, _steleItemId);
                if (isVisible)
                {
                    SendOfferingHint(player, false);
                }
                else
                {
                    // Guide to tower map
                    SendWalkToTowerHint(player);
                }
            }
        }

        private void OnStoryComplete(params object[] args)
        {
            if (args.Length < 1) return;

            var player = args[0] as Player;
            if (player == null) return;

            var state = GetState(player);
            if (state == null || state.Phase != Phase.Offering) return;

            Complete(player);
        }

        #endregion

        #region Private Methods

        private PlayerState GetState(Player player)
        {
            if (player == null) return null;
            return _playerStates.TryGetValue(player.GetHashCode(), out var state) ? state : null;
        }

        private PlayerState GetOrCreateState(Player player)
        {
            int hash = player.GetHashCode();
            if (!_playerStates.TryGetValue(hash, out var state))
            {
                state = new PlayerState();
                _playerStates[hash] = state;
            }
            return state;
        }

        private void SaveProgress(Player player, PlayerState state)
        {
            player.Database.record[RecordPhase] = (int)state.Phase;
            player.Database.record[RecordExploreCompleted] = (int)state.ExploreCompleted;
            Utils.Debug.Log.Info("TUTORIAL", $"[SaveProgress] Phase={state.Phase}, ExploreCompleted={state.ExploreCompleted}");
        }

        private void AdvancePhase(Player player, PlayerState state, Phase newPhase)
        {
            Utils.Debug.Log.Info("TUTORIAL", $"[AdvancePhase] {state.Phase} -> {newPhase}");

            state.Phase = newPhase;
            state.IsTraveling = false;
            state.JustAdvanced = true;

            SaveProgress(player, state);
            
            OnPhaseAdvance(player, newPhase);

            switch (newPhase)
            {
                case Phase.Explore:
                    state.ExploreCompleted = ExploreTask.None;
                    state.CurrentExploreTarget = ExploreTask.None;
                    state.CurrentAction = ExploreAction.None;
                    SelectNearestExploreTarget(player, state);
                    break;

                case Phase.Collect:
                    SendCollectHint(player);
                    // Check if items already collected
                    CheckCollectCompletion(player, state);
                    break;

                case Phase.Offering:
                    // Check if at stele or can see stele
                    if (IsAtCharacterLocation(player, _steleItemId))
                    {
                        SendOfferingHint(player, true);
                    }
                    else if (CanSeeCharacter(player, _steleItemId))
                    {
                        SendOfferingHint(player, false);
                    }
                    else
                    {
                        SendWalkToTowerHint(player);
                    }
                    break;
            }
        }

        private void OnPhaseAdvance(Player player, Phase newPhase)
        {
            switch (newPhase)
            {
                case Phase.WalkToSand:
                    // Initial state: only Scene visible (already set in InitializeUILock)
                    break;
                    
                case Phase.Explore:
                    // Unlock Area and Information panels
                    Display.Agent.Instance.UnlockHomePanels(player, 
                        Display.Agent.HomePanels.Area, 
                        Display.Agent.HomePanels.Information);
                    Utils.Debug.Log.Info("TUTORIAL", $"[OnPhaseAdvance] Unlocked Area and Information panels");
                    break;
                    
                case Phase.Completed:
                    // Unlock all Home panels
                    Display.Agent.Instance.UnlockAllHomePanels(player);
                    Utils.Debug.Log.Info("TUTORIAL", $"[OnPhaseAdvance] Unlocked all Home panels");
                    break;
            }
        }

        private void CheckExploreCompletion(Player player, PlayerState state)
        {
            SaveProgress(player, state);

            if (state.ExploreCompleted == ExploreTask.All)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[CheckExploreCompletion] All explore tasks completed, advancing to Collect");
                AdvancePhase(player, state, Phase.Collect);
            }
            else
            {
                // Find next target
                SelectNearestExploreTarget(player, state);
            }
        }

        private void CheckCollectCompletion(Player player, PlayerState state)
        {
            bool hasGoldOre = player.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
            bool hasRawMeat = player.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);

            if (hasGoldOre && hasRawMeat)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[CheckCollectCompletion] Items already collected, advancing to Offering");
                AdvancePhase(player, state, Phase.Offering);
            }
        }

        private void CheckInitialVisibility(Player player, PlayerState state)
        {
            // Check if player can already see gold mine or lizard
            bool canSeeGoldMine = CanSeeCharacter(player, _goldMineItemId);
            bool canSeeLizard = CanSeeCharacter(player, _lizardLifeId);

            // Also check if already at target location
            bool atGoldMine = IsAtCharacterLocation(player, _goldMineItemId);
            bool atLizard = IsAtCharacterLocation(player, _lizardLifeId);

            Utils.Debug.Log.Info("TUTORIAL", $"[CheckInitialVisibility] canSeeGoldMine={canSeeGoldMine}, canSeeLizard={canSeeLizard}, atGoldMine={atGoldMine}, atLizard={atLizard}");

            if (canSeeGoldMine || canSeeLizard || atGoldMine || atLizard)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[CheckInitialVisibility] Can see or at targets, advancing to Explore");
                AdvancePhase(player, state, Phase.Explore);
            }
        }

        /// <summary>
        /// Select the nearest visible explore target that hasn't been completed
        /// </summary>
        private void SelectNearestExploreTarget(Player player, PlayerState state)
        {
            var remainingTasks = ExploreTask.All & ~state.ExploreCompleted;
            if (remainingTasks == ExploreTask.None)
            {
                // All tasks completed, should not happen here
                return;
            }

            // Get visible characters
            var visibleCharacters = Perception.Agent.Instance.GetVisibleCharacters(player);

            // Find candidates
            var candidates = new List<(ExploreTask task, Character character)>();

            if (remainingTasks.HasFlag(ExploreTask.GoldMine))
            {
                var goldMine = visibleCharacters.FirstOrDefault(c => Perception.Agent.GetCharacterConfigId(c) == _goldMineItemId);
                if (goldMine != null)
                {
                    candidates.Add((ExploreTask.GoldMine, goldMine));
                }
            }

            if (remainingTasks.HasFlag(ExploreTask.Lizard))
            {
                var lizard = visibleCharacters.FirstOrDefault(c => Perception.Agent.GetCharacterConfigId(c) == _lizardLifeId);
                if (lizard != null)
                {
                    candidates.Add((ExploreTask.Lizard, lizard));
                }
            }

            if (candidates.Count == 0)
            {
                // No visible targets, send generic explore hint
                Utils.Debug.Log.Info("TUTORIAL", $"[SelectNearestExploreTarget] No visible targets");
                state.CurrentExploreTarget = ExploreTask.None;
                state.CurrentAction = ExploreAction.None;
                SendExploreWaitHint(player);
                return;
            }

            // Select nearest (or first if only one)
            ExploreTask selectedTask;
            if (candidates.Count == 1)
            {
                selectedTask = candidates[0].task;
            }
            else
            {
                // Both visible - select based on distance
                // Since we don't have position info easily, we'll use a simple heuristic:
                // Check which one is in the same map as the player
                var playerMap = player.Map;
                var goldMineInMap = playerMap?.Content.Has<Item>(i => i.Config?.Id == _goldMineItemId) ?? false;
                var lizardInMap = playerMap?.Content.Has<Life>(l => l.Config?.Id == _lizardLifeId) ?? false;

                if (goldMineInMap && !lizardInMap)
                {
                    selectedTask = ExploreTask.GoldMine;
                }
                else if (lizardInMap && !goldMineInMap)
                {
                    selectedTask = ExploreTask.Lizard;
                }
                else
                {
                    // Both or neither in same map, default to gold mine (safer for new players)
                    selectedTask = ExploreTask.GoldMine;
                }
            }

            Utils.Debug.Log.Info("TUTORIAL", $"[SelectNearestExploreTarget] Selected {selectedTask}");

            state.CurrentExploreTarget = selectedTask;
            state.JustAdvanced = true;

            // Check if already at target location - if so, go directly to AtTarget
            int targetConfigId = selectedTask == ExploreTask.GoldMine ? _goldMineItemId : _lizardLifeId;
            if (IsAtCharacterLocation(player, targetConfigId))
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[SelectNearestExploreTarget] Already at target, setting AtTarget");
                state.CurrentAction = ExploreAction.AtTarget;
            }
            else
            {
                state.CurrentAction = ExploreAction.SeeTarget;
            }

            SendExploreHint(player, state);
        }

        private bool IsAtCharacterLocation(Player player, int configId)
        {
            if (player.Map == null)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[IsAtCharacterLocation] player.Map is null");
                return false;
            }

            // Check items in current map
            bool hasItem = player.Map.Content.Has<Item>(i => i.Config?.Id == configId);
            // Check lives in current map
            bool hasLife = player.Map.Content.Has<Life>(l => l.Config?.Id == configId);

            // Get map position for better debugging
            var pos = player.Map.Database?.pos;
            string posStr = pos != null ? $"[{string.Join(",", pos)}]" : "null";

            Utils.Debug.Log.Info("TUTORIAL", $"[IsAtCharacterLocation] configId={configId}, mapId={player.Map.Config?.Id}, pos={posStr}, hasItem={hasItem}, hasLife={hasLife}");

            return hasItem || hasLife;
        }

        private bool CanSeeCharacter(Player player, int configId)
        {
            var visibleCharacters = Perception.Agent.Instance.GetVisibleCharacters(player);
            return visibleCharacters.Any(c => Perception.Agent.GetCharacterConfigId(c) == configId);
        }

        #endregion

        #region Hint Methods

        private void SendPhaseHint(Player player, PlayerState state)
        {
            switch (state.Phase)
            {
                case Phase.WalkToSand:
                    SendWalkToSandHint(player);
                    break;
                case Phase.Explore:
                    SendExploreHint(player, state);
                    break;
                case Phase.Collect:
                    SendCollectHint(player);
                    break;
                case Phase.Offering:
                    SendOfferingHint(player, false);
                    break;
            }
        }

        private void SendWalkToSandHint(Player player)
        {
            var pos = GetMapPos(_tutorialSandMapId, player);
            Utils.Debug.Log.Info("TUTORIAL", $"[SendWalkToSandHint] sandMapId={_tutorialSandMapId}, pos={pos?.Length ?? -1}, playerMap={player.Map?.Config?.Id ?? 0}");
            var protocol = new Net.Protocol.Tutorial(
                (int)Phase.WalkToSand,
                (int)TargetType.Map,
                0,
                "",
                pos,
                ""
            );
            Net.Tcp.Instance.Send(player, protocol);
        }

        private void SendExploreHint(Player player, PlayerState state)
        {
            if (state.CurrentExploreTarget == ExploreTask.None)
            {
                SendExploreWaitHint(player);
                return;
            }

            // Don't send hint if player has an active Option panel open
            // This prevents interfering with player's manual interactions
            if (player.Option != null)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[SendExploreHint] Player has active Option ({player.Option.Type}), skipping hint to avoid interference");
                return;
            }

            int targetId;
            TargetType targetType;
            string path;
            string hintCid;

            if (state.CurrentExploreTarget == ExploreTask.GoldMine)
            {
                targetId = _goldMineItemId;
                targetType = TargetType.Item;
                path = state.CurrentAction == ExploreAction.SeeTarget ? "characters/goto" : "actions/interact";
                hintCid = state.CurrentAction == ExploreAction.SeeTarget ? "tutorial_goto" : "tutorial_interact";
            }
            else
            {
                targetId = _lizardLifeId;
                targetType = TargetType.Creature;
                path = state.CurrentAction == ExploreAction.SeeTarget ? "characters/goto" : "actions/attack";
                hintCid = state.CurrentAction == ExploreAction.SeeTarget ? "tutorial_goto" : "tutorial_attack";
            }

            string hintText = Domain.Text.Agent.Instance.GetByCid(hintCid, player);

            Utils.Debug.Log.Info("TUTORIAL", $"[SendExploreHint] target={state.CurrentExploreTarget}, action={state.CurrentAction}, path={path}, targetId={targetId}");

            var protocol = new Net.Protocol.Tutorial(
                (int)Phase.Explore,
                (int)targetType,
                targetId,
                path,
                null,
                hintText
            );
            Net.Tcp.Instance.Send(player, protocol);
        }

        private void SendExploreWaitHint(Player player)
        {
            // No visible targets - just clear any existing hint, don't send empty hint
            Utils.Debug.Log.Info("TUTORIAL", $"[SendExploreWaitHint] Clearing hint (no visible targets)");
            ClearHint(player);
        }

        private void SendCollectHint(Player player)
        {
            string hintText = Domain.Text.Agent.Instance.GetByCid("tutorial_pickup", player);

            var protocol = new Net.Protocol.Tutorial(
                (int)Phase.Collect,
                (int)TargetType.Item,
                0,
                "actions/pickup",
                null,
                hintText
            );
            Net.Tcp.Instance.Send(player, protocol);
        }

        private void SendOfferingHint(Player player, bool atStele)
        {
            string path = atStele ? "actions/give" : "characters/goto";
            string hintCid = atStele ? "tutorial_give" : "tutorial_goto";
            string hintText = Domain.Text.Agent.Instance.GetByCid(hintCid, player);

            var protocol = new Net.Protocol.Tutorial(
                (int)Phase.Offering,
                (int)TargetType.Item,
                _steleItemId,
                path,
                null,
                hintText
            );
            Net.Tcp.Instance.Send(player, protocol);
        }

        private void SendWalkToTowerHint(Player player)
        {
            var pos = GetMapPos(_tutorialTowerMapId, player);
            var protocol = new Net.Protocol.Tutorial(
                (int)Phase.Offering,
                (int)TargetType.Map,
                0,
                "",
                pos,
                ""
            );
            Net.Tcp.Instance.Send(player, protocol);
        }

        private void ClearHint(Player player)
        {
            Utils.Debug.Log.Info("TUTORIAL", $"[ClearHint] Sending clear protocol (step=0, targetType=0)");
            // Note: step=0 and targetType=0 signals client to clear/hide tutorial UI
            var protocol = new Net.Protocol.Tutorial(0, 0, 0, "", null, "");
            Net.Tcp.Instance.Send(player, protocol);
        }

        private int[] GetMapPos(int mapId, Player player)
        {
            var currentMap = player.Map;
            if (currentMap == null)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[GetMapPos] player.Map is null");
                return null;
            }

            if (currentMap.Copy != null)
            {
                var targetMap = currentMap.Copy.Content.Get<Logic.Map>(m => m.Config.Id == mapId);
                if (targetMap != null)
                {
                    Utils.Debug.Log.Info("TUTORIAL", $"[GetMapPos] Found in Copy, pos={string.Join(",", targetMap.Database.pos)}");
                    return targetMap.Database.pos;
                }
                Utils.Debug.Log.Info("TUTORIAL", $"[GetMapPos] Not found in Copy (mapId={mapId})");
            }
            else if (currentMap.Scene != null)
            {
                var targetMap = currentMap.Scene.Content.Get<Logic.Map>(m => m.Config.Id == mapId);
                if (targetMap != null)
                {
                    Utils.Debug.Log.Info("TUTORIAL", $"[GetMapPos] Found in Scene, pos={string.Join(",", targetMap.Database.pos)}");
                    return targetMap.Database.pos;
                }
                Utils.Debug.Log.Info("TUTORIAL", $"[GetMapPos] Not found in Scene (mapId={mapId})");
            }
            else
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[GetMapPos] No Copy or Scene");
            }
            return null;
        }

        private void PlayTutorialStory(Player player)
        {
            var textAgent = Domain.Text.Agent.Instance;
            var dialogues = new List<Net.Protocol.Story.Line>
            {
                new Net.Protocol.Story.Line { character = "", words = textAgent.GetByCid("tutorial_story_1", player) },
                new Net.Protocol.Story.Line { character = "", words = textAgent.GetByCid("tutorial_story_2", player) },
                new Net.Protocol.Story.Line { character = "", words = textAgent.GetByCid("tutorial_story_3", player) },
                new Net.Protocol.Story.Line { character = "", words = textAgent.GetByCid("tutorial_story_4", player) },
                new Net.Protocol.Story.Line { character = "", words = textAgent.GetByCid("tutorial_story_5", player) }
            };

            var story = new Net.Protocol.Story(dialogues);
            Net.Tcp.Instance.Send(player, story);
        }

        #endregion

        #region Legacy API Compatibility

        /// <summary>
        /// Legacy Step enum for backward compatibility
        /// Maps to new Phase + ExploreTask system
        /// </summary>
        public enum Step
        {
            None = 0,
            WalkToSand = 1,
            SeeGoldMine = 2,
            InteractGoldMine = 3,
            SeeLizard = 4,
            AttackLizard = 5,
            PickupItems = 6,
            SeeStele = 7,
            WalkToTower = 8,
            GiveToStele = 9,
            Completed = 100
        }

        /// <summary>
        /// Get current step (legacy API, maps from new system)
        /// </summary>
        public Step GetCurrentStep(Player player)
        {
            var state = GetState(player);
            if (state == null) return Step.None;

            return state.Phase switch
            {
                Phase.None => Step.None,
                Phase.WalkToSand => Step.WalkToSand,
                Phase.Explore => GetExploreStep(state),
                Phase.Collect => Step.PickupItems,
                Phase.Offering => GetOfferingStep(player, state),
                Phase.Completed => Step.Completed,
                _ => Step.None
            };
        }

        private Step GetExploreStep(PlayerState state)
        {
            if (state.CurrentExploreTarget == ExploreTask.GoldMine)
            {
                return state.CurrentAction == ExploreAction.AtTarget ? Step.InteractGoldMine : Step.SeeGoldMine;
            }
            else if (state.CurrentExploreTarget == ExploreTask.Lizard)
            {
                return state.CurrentAction == ExploreAction.AtTarget ? Step.AttackLizard : Step.SeeLizard;
            }
            return Step.SeeGoldMine; // Default
        }

        private Step GetOfferingStep(Player player, PlayerState state)
        {
            if (IsAtCharacterLocation(player, _steleItemId))
            {
                return Step.GiveToStele;
            }
            else if (CanSeeCharacter(player, _steleItemId))
            {
                return Step.SeeStele;
            }
            return Step.WalkToTower;
        }

        #endregion
    }
}
