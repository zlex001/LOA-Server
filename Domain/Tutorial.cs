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

            // Register event listeners
            Logic.Agent.Instance.monitor.Register(Logic.Player.Event.StoryComplete, OnStoryComplete);

            // Register for first-seen events from Perception
            Perception.Agent.Instance.FirstSeen += OnFirstSeen;
        }

        private void CacheConfigIds()
        {
            // Lookup Design layer configs for cid
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
        /// Start tutorial for a new player
        /// </summary>
        public void Start(Player player)
        {
            if (player == null) return;

            Utils.Debug.Log.Info("TUTORIAL", $"[Start] Starting tutorial for player");

            // Register map change listener
            RegisterPlayerMapListener(player);

            // Initialize state
            var state = GetOrCreateState(player);
            state.Phase = Phase.WalkToSand;

            // Save and send hint
            SaveProgress(player, state);
            SendPhaseHint(player, state);

            // Check if already at sand or can see targets
            CheckInitialVisibility(player, state);
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

        /// <summary>
        /// Called when player interacts with gold mine
        /// </summary>
        public void OnInteractGoldMine(Player player)
        {
            var state = GetState(player);
            if (state == null || state.Phase != Phase.Explore) return;
            if (state.CurrentExploreTarget != ExploreTask.GoldMine) return;
            if (state.CurrentAction != ExploreAction.AtTarget) return;

            Utils.Debug.Log.Info("TUTORIAL", $"[OnInteractGoldMine] Gold mine interaction completed");

            // Mark gold mine as completed
            state.ExploreCompleted |= ExploreTask.GoldMine;
            state.CurrentExploreTarget = ExploreTask.None;
            state.CurrentAction = ExploreAction.None;

            CheckExploreCompletion(player, state);
        }

        /// <summary>
        /// Called when player defeats lizard
        /// </summary>
        public void OnDefeatLizard(Player player)
        {
            var state = GetState(player);
            if (state == null || state.Phase != Phase.Explore) return;
            if (state.CurrentExploreTarget != ExploreTask.Lizard) return;
            if (state.CurrentAction != ExploreAction.AtTarget) return;

            Utils.Debug.Log.Info("TUTORIAL", $"[OnDefeatLizard] Lizard defeated");

            // Mark lizard as completed
            state.ExploreCompleted |= ExploreTask.Lizard;
            state.CurrentExploreTarget = ExploreTask.None;
            state.CurrentAction = ExploreAction.None;

            CheckExploreCompletion(player, state);
        }

        /// <summary>
        /// Called when player picks up items
        /// </summary>
        public void OnPickupItem(Player player, Item item)
        {
            var state = GetState(player);
            if (state == null || state.Phase != Phase.Collect) return;

            // Check if player has both gold ore and raw meat
            bool hasGoldOre = player.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
            bool hasRawMeat = player.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);

            if (hasGoldOre && hasRawMeat)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnPickupItem] All items collected, advancing to Offering");
                AdvancePhase(player, state, Phase.Offering);
            }
        }

        /// <summary>
        /// Called when player gives items to stele
        /// </summary>
        public void OnGiveToStele(Player player, Item stele, Item givenItem)
        {
            if (player == null || stele == null) return;

            var state = GetState(player);
            if (state == null || state.Phase != Phase.Offering) return;

            // Check if stele has required items
            bool hasGoldOre = stele.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
            bool hasRawMeat = stele.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);

            if (hasGoldOre && hasRawMeat)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnGiveToStele] Items given, playing story");
                PlayTutorialStory(player);
            }
        }

        /// <summary>
        /// Called when player clicks "GoTo" button on a character.
        /// </summary>
        public void OnPlayerGoTo(Player player, Logic.Character target)
        {
            var state = GetState(player);
            if (state == null) return;

            int targetConfigId = Perception.Agent.GetCharacterConfigId(target);

            // Check if this is the current explore target
            if (state.Phase == Phase.Explore && state.CurrentAction == ExploreAction.SeeTarget)
            {
                bool isCurrentTarget =
                    (state.CurrentExploreTarget == ExploreTask.GoldMine && targetConfigId == _goldMineItemId) ||
                    (state.CurrentExploreTarget == ExploreTask.Lizard && targetConfigId == _lizardLifeId);

                if (isCurrentTarget)
                {
                    Utils.Debug.Log.Info("TUTORIAL", $"[OnPlayerGoTo] Player going to explore target");
                    state.IsTraveling = true;
                    ClearHint(player);
                }
            }
            else if (state.Phase == Phase.Offering && targetConfigId == _steleItemId)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnPlayerGoTo] Player going to stele");
                state.IsTraveling = true;
                ClearHint(player);
            }
        }

        #endregion

        #region Event Handlers

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
            if (state.CurrentExploreTarget == ExploreTask.None)
            {
                // No target selected, try to find one
                SelectNearestExploreTarget(player, state);
                return;
            }

            // Check if player arrived at current target
            int targetConfigId = state.CurrentExploreTarget == ExploreTask.GoldMine ? _goldMineItemId : _lizardLifeId;

            if (state.CurrentAction == ExploreAction.SeeTarget)
            {
                // Check if arrived at target location
                if (IsAtCharacterLocation(player, targetConfigId))
                {
                    Utils.Debug.Log.Info("TUTORIAL", $"[HandleExploreMoved] Arrived at {state.CurrentExploreTarget}");
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

            if (canSeeGoldMine || canSeeLizard)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[CheckInitialVisibility] Can already see targets, advancing to Explore");
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
            state.CurrentAction = ExploreAction.SeeTarget;
            state.JustAdvanced = true;

            SendExploreHint(player, state);
        }

        private bool IsAtCharacterLocation(Player player, int configId)
        {
            if (player.Map == null) return false;

            // Check items in current map
            if (player.Map.Content.Has<Item>(i => i.Config?.Id == configId)) return true;
            // Check lives in current map
            if (player.Map.Content.Has<Life>(l => l.Config?.Id == configId)) return true;

            return false;
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
            // Generic hint to explore the area
            var protocol = new Net.Protocol.Tutorial(
                (int)Phase.Explore,
                (int)TargetType.UI,
                0,
                "",
                null,
                ""
            );
            Net.Tcp.Instance.Send(player, protocol);
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
            var protocol = new Net.Protocol.Tutorial(0, 0, 0, "", null, "");
            Net.Tcp.Instance.Send(player, protocol);
        }

        private int[] GetMapPos(int mapId, Player player)
        {
            var currentMap = player.Map;
            if (currentMap?.Copy != null)
            {
                var targetMap = currentMap.Copy.Content.Get<Logic.Map>(m => m.Config.Id == mapId);
                if (targetMap != null)
                {
                    return targetMap.Database.pos;
                }
            }
            else if (currentMap?.Scene != null)
            {
                var targetMap = currentMap.Scene.Content.Get<Logic.Map>(m => m.Config.Id == mapId);
                if (targetMap != null)
                {
                    return targetMap.Database.pos;
                }
            }
            return null;
        }

        private void PlayTutorialStory(Player player)
        {
            var dialogues = new List<Net.Protocol.Story.Line>
            {
                new Net.Protocol.Story.Line { character = "", words = "The ancient stele begins to glow..." },
                new Net.Protocol.Story.Line { character = "", words = "A voice echoes from the depths of time..." },
                new Net.Protocol.Story.Line { character = "", words = "\"Traveler, your journey has just begun.\"" },
                new Net.Protocol.Story.Line { character = "", words = "\"The world awaits your exploration.\"" },
                new Net.Protocol.Story.Line { character = "", words = "\"Go forth, and write your own legend.\"" }
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
