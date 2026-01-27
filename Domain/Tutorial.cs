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

        #endregion

        #region State Storage

        // Player tutorial state storage (player hash -> current step)
        private Dictionary<int, Step> playerStates = new Dictionary<int, Step>();

        #endregion

        #region Initialization

        public void Init()
        {
            // Register event listeners
            Logic.Agent.Instance.monitor.Register(Logic.Life.Event.AfterMoved, OnPlayerMoved);
            Logic.Agent.Instance.monitor.Register(Logic.Player.Event.CutsceneComplete, OnCutsceneComplete);
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
                Move.Walk.Teleport(player, destination);
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
            bool hasGoldOre = stele.Content.Has<Item>(i => i.Config.cid == "金矿石");
            bool hasRawMeat = stele.Content.Has<Item>(i => i.Config.cid == "生肉");

            if (hasGoldOre && hasRawMeat)
            {
                // Play cutscene
                PlayTutorialCutscene(player);
            }
        }

        #endregion

        #region Event Handlers

        private void OnPlayerMoved(params object[] args)
        {
            if (args.Length < 1) return;

            var life = args[0] as Life;
            if (life is not Player player) return;

            var step = GetCurrentStep(player);
            if (step == Step.None || step == Step.Completed) return;

            var currentMap = player.Map;
            if (currentMap == null) return;

            switch (step)
            {
                case Step.WalkToSand:
                    if (currentMap.Config.cid == "遗迹-沙地")
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
                    if (currentMap.Config.cid == "遗迹-通天塔")
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
                bool hasGoldOre = player.Content.Has<Item>(i => i.Config.cid == "金矿石");
                bool hasRawMeat = player.Content.Has<Item>(i => i.Config.cid == "生肉");

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
                Step.WalkToSand => (TargetType.Map, Logic.Constant.TutorialSand, "", ""),
                Step.InteractGoldMine => (TargetType.Item, GetGoldMineId(), "", ""),
                Step.AttackLizard => (TargetType.Creature, GetLizardId(), "", ""),
                Step.PickupItems => (TargetType.Item, 0, "", ""),  // Highlight dropped items
                Step.WalkToTower => (TargetType.Map, Logic.Constant.TutorialTower, "", ""),
                Step.GiveToStele => (TargetType.Item, Logic.Constant.Stele, "", ""),
                _ => (TargetType.UI, 0, "", "")
            };
        }

        private int GetGoldMineId()
        {
            var goldMine = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Item>(i => i.cid == "金矿");
            return goldMine?.Id ?? 0;
        }

        private int GetLizardId()
        {
            var lizard = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Life>(l => l.cid == "蜥蜴");
            return lizard?.Id ?? 0;
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
