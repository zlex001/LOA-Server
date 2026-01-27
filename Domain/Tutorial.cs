using Logic;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    /// <summary>
    /// Tutorial system for new player guidance.
    /// Manages tutorial steps and triggers appropriate hints.
    /// </summary>
    public class Tutorial
    {
        #region Singleton

        private static Tutorial instance;
        public static Tutorial Instance { get { if (instance == null) { instance = new Tutorial(); } return instance; } }

        #endregion

        #region Constants

        /// <summary>
        /// Tutorial step identifiers
        /// </summary>
        public enum Step
        {
            None = 0,
            WalkToSand = 1,          // Guide player to walk to sand map
            InteractGoldMine = 2,    // Guide player to interact with gold mine
            AttackLizard = 3,        // Guide player to attack lizard
            PickupItems = 4,         // Guide player to pickup items
            WalkToTower = 5,         // Guide player to walk to tower
            GiveToStele = 6,         // Guide player to give items to stele
            Completed = 100          // Tutorial completed
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

        /// <summary>
        /// Cutscene IDs
        /// </summary>
        public const int TutorialCutsceneId = 1;

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

        // Player tutorial state storage (player hash -> current step)
        private Dictionary<int, Step> playerStates = new Dictionary<int, Step>();

        #endregion

        #region Initialization

        public void Init()
        {
            // Cache config IDs for runtime lookup
            CacheConfigIds();
            
            // Register event listeners - use Map.Event.Arrived for player movement detection
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnAddPlayer);
            Logic.Agent.Instance.monitor.Register(Logic.Player.Event.CutsceneComplete, OnCutsceneComplete);
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

        private void OnAddPlayer(params object[] args)
        {
            var player = args[1] as Player;
            if (player == null) return;
            
            // Register for map changes
            player.data.after.Register(Basic.Element.Data.Parent, OnPlayerParentChanged);
        }

        private void OnPlayerParentChanged(params object[] args)
        {
            // args[0] = old value, args[1] = new value, args[2] = player
            if (args.Length < 3) return;
            var player = args[2] as Player;
            if (player == null) return;
            
            OnPlayerMoved(player);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start tutorial for a new player
        /// </summary>
        public void Start(Player player)
        {
            if (player == null) return;

            playerStates[player.GetHashCode()] = Step.WalkToSand;
            SendTutorialHint(player, Step.WalkToSand);
        }

        /// <summary>
        /// Get current tutorial step for player
        /// </summary>
        public Step GetCurrentStep(Player player)
        {
            if (player == null) return Step.None;
            return playerStates.TryGetValue(player.GetHashCode(), out var step) ? step : Step.None;
        }

        /// <summary>
        /// Check if player is in tutorial
        /// </summary>
        public bool IsInTutorial(Player player)
        {
            var step = GetCurrentStep(player);
            return step != Step.None && step != Step.Completed;
        }

        /// <summary>
        /// Complete tutorial and teleport to random city
        /// </summary>
        public void Complete(Player player)
        {
            if (player == null) return;

            playerStates[player.GetHashCode()] = Step.Completed;

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

            // Clean up state
            playerStates.Remove(player.GetHashCode());
        }

        /// <summary>
        /// Called when player interacts with an item (for give action detection)
        /// </summary>
        public void OnGiveToStele(Player player, Item stele, Item givenItem)
        {
            if (player == null || stele == null) return;

            var step = GetCurrentStep(player);
            if (step != Step.GiveToStele) return;

            // Check if stele has required items (gold ore and raw meat)
            bool hasGoldOre = stele.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
            bool hasRawMeat = stele.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);

            if (hasGoldOre && hasRawMeat)
            {
                // Play cutscene
                PlayTutorialCutscene(player);
            }
        }

        #endregion

        #region Event Handlers

        private void OnPlayerMoved(Player player)
        {
            if (player == null) return;

            var step = GetCurrentStep(player);
            if (step == Step.None || step == Step.Completed) return;

            var currentMap = player.Map;
            if (currentMap == null) return;

            switch (step)
            {
                case Step.WalkToSand:
                    if (currentMap.Config.Id == _tutorialSandMapId)
                    {
                        AdvanceStep(player, Step.InteractGoldMine);
                    }
                    break;

                case Step.InteractGoldMine:
                case Step.AttackLizard:
                case Step.PickupItems:
                    // These are handled by interaction/combat/pickup events
                    break;

                case Step.WalkToTower:
                    if (currentMap.Config.Id == _tutorialTowerMapId)
                    {
                        AdvanceStep(player, Step.GiveToStele);
                    }
                    break;
            }
        }

        private void OnCutsceneComplete(params object[] args)
        {
            if (args.Length < 3) return;

            var player = args[0] as Player;
            var cutsceneId = (int)args[1];
            var skipped = (bool)args[2];

            if (player == null) return;
            if (cutsceneId != TutorialCutsceneId) return;

            // Complete tutorial
            Complete(player);
        }

        /// <summary>
        /// Called when player interacts with gold mine
        /// </summary>
        public void OnInteractGoldMine(Player player)
        {
            var step = GetCurrentStep(player);
            if (step == Step.InteractGoldMine)
            {
                AdvanceStep(player, Step.AttackLizard);
            }
        }

        /// <summary>
        /// Called when player defeats lizard
        /// </summary>
        public void OnDefeatLizard(Player player)
        {
            var step = GetCurrentStep(player);
            if (step == Step.AttackLizard)
            {
                AdvanceStep(player, Step.PickupItems);
            }
        }

        /// <summary>
        /// Called when player picks up items
        /// </summary>
        public void OnPickupItem(Player player, Item item)
        {
            var step = GetCurrentStep(player);
            if (step == Step.PickupItems)
            {
                // Check if player has both gold ore and raw meat
                bool hasGoldOre = player.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
                bool hasRawMeat = player.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);

                if (hasGoldOre && hasRawMeat)
                {
                    AdvanceStep(player, Step.WalkToTower);
                }
            }
        }

        #endregion

        #region Private Methods

        private void AdvanceStep(Player player, Step nextStep)
        {
            playerStates[player.GetHashCode()] = nextStep;
            SendTutorialHint(player, nextStep);
        }

        private void SendTutorialHint(Player player, Step step)
        {
            var (targetType, targetId, targetPath, hint) = GetStepHint(step);
            
            var protocol = new Net.Protocol.Tutorial(
                (int)step,
                (int)targetType,
                targetId,
                targetPath,
                hint
            );

            Net.Tcp.Instance.Send(player, protocol);
        }

        private (TargetType type, int id, string path, string hint) GetStepHint(Step step)
        {
            return step switch
            {
                Step.WalkToSand => (TargetType.Map, _tutorialSandMapId, "", ""),
                Step.InteractGoldMine => (TargetType.Item, _goldMineItemId, "", ""),
                Step.AttackLizard => (TargetType.Creature, _lizardLifeId, "", ""),
                Step.PickupItems => (TargetType.Item, 0, "", ""),  // Highlight dropped items
                Step.WalkToTower => (TargetType.Map, _tutorialTowerMapId, "", ""),
                Step.GiveToStele => (TargetType.Item, _steleItemId, "", ""),
                _ => (TargetType.UI, 0, "", "")
            };
        }

        private void PlayTutorialCutscene(Player player)
        {
            var texts = new string[]
            {
                "The ancient stele begins to glow...",
                "A voice echoes from the depths of time...",
                "\"Traveler, your journey has just begun.\"",
                "\"The world awaits your exploration.\"",
                "\"Go forth, and write your own legend.\""
            };

            var cutscene = new Net.Protocol.Cutscene(
                TutorialCutsceneId,
                texts,
                charInterval: 30,
                textInterval: 2000
            );

            Net.Tcp.Instance.Send(player, cutscene);
        }

        #endregion
    }
}
