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
            SeeGoldMine = 2,         // Player sees gold mine - guide to click and go
            InteractGoldMine = 3,    // Player at gold mine - guide to interact
            SeeLizard = 4,           // Player sees lizard - guide to click and go
            AttackLizard = 5,        // Player at lizard - guide to attack
            PickupItems = 6,         // Guide player to pickup items
            SeeStele = 7,            // Player sees stele - guide to click and go
            WalkToTower = 8,         // Guide player to walk to tower
            GiveToStele = 9,         // Guide player to give items to stele
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
        
        private void OnFirstSeen(Player player, Character character)
        {
            if (player == null || character == null) return;
            
            var step = GetCurrentStep(player);
            if (step == Step.None || step == Step.Completed) return;
            
            int configId = Perception.Agent.GetCharacterConfigId(character);
            
            Utils.Debug.Log.Info("TUTORIAL", $"[OnFirstSeen] Player sees character configId={configId}, currentStep={step}, goldMineId={_goldMineItemId}, lizardId={_lizardLifeId}, steleId={_steleItemId}");
            
            // Check if player sees gold mine during WalkToSand step
            if (step == Step.WalkToSand && configId == _goldMineItemId)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnFirstSeen] Matched: gold mine at WalkToSand, advancing to SeeGoldMine");
                AdvanceStep(player, Step.SeeGoldMine);
                return;
            }
            
            // Check if player sees lizard during InteractGoldMine step
            if (step == Step.InteractGoldMine && configId == _lizardLifeId)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnFirstSeen] Matched: lizard at InteractGoldMine, advancing to SeeLizard");
                AdvanceStep(player, Step.SeeLizard);
                return;
            }
            
            // Check if player sees stele during PickupItems step
            if (step == Step.PickupItems && configId == _steleItemId)
            {
                Utils.Debug.Log.Info("TUTORIAL", $"[OnFirstSeen] Matched: stele at PickupItems, advancing to SeeStele");
                AdvanceStep(player, Step.SeeStele);
                return;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start tutorial for a new player
        /// </summary>
        public void Start(Player player)
        {
            if (player == null) return;

            Utils.Debug.Log.Info("TUTORIAL", $"[Start] Starting tutorial for player, goldMineId={_goldMineItemId}, lizardId={_lizardLifeId}, steleId={_steleItemId}");
            
            playerStates[player.GetHashCode()] = Step.WalkToSand;
            SendTutorialHint(player, Step.WalkToSand);
            
            // After starting, check if gold mine is already visible
            CheckVisibilityAfterAdvance(player, Step.WalkToSand);
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
                // Play story
                PlayTutorialStory(player);
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
                    // If player walks to sand without seeing gold mine first, skip to SeeGoldMine
                    if (currentMap.Config.Id == _tutorialSandMapId)
                    {
                        AdvanceStep(player, Step.SeeGoldMine);
                    }
                    break;
                    
                case Step.SeeGoldMine:
                    // Player arrived at gold mine location - guide to interact
                    if (IsAtCharacterLocation(player, _goldMineItemId))
                    {
                        AdvanceStep(player, Step.InteractGoldMine);
                    }
                    break;

                case Step.InteractGoldMine:
                    // Handled by OnInteractGoldMine
                    break;
                    
                case Step.SeeLizard:
                    // Player arrived at lizard location - guide to attack
                    if (IsAtCharacterLocation(player, _lizardLifeId))
                    {
                        AdvanceStep(player, Step.AttackLizard);
                    }
                    break;

                case Step.AttackLizard:
                case Step.PickupItems:
                    // These are handled by combat/pickup events
                    break;
                    
                case Step.SeeStele:
                    // Player arrived at stele location - guide to give
                    if (IsAtCharacterLocation(player, _steleItemId))
                    {
                        AdvanceStep(player, Step.GiveToStele);
                    }
                    break;

                case Step.WalkToTower:
                    if (currentMap.Config.Id == _tutorialTowerMapId)
                    {
                        AdvanceStep(player, Step.GiveToStele);
                    }
                    break;
            }
        }
        
        private bool IsAtCharacterLocation(Player player, int configId)
        {
            if (player.Map == null) return false;
            
            // Check if there's a character with this config ID in the player's current map
            var copy = player.Map.Copy;
            if (copy != null)
            {
                foreach (var map in copy.Content.Gets<Logic.Copy.Map>())
                {
                    if (map != player.Map) continue;
                    
                    // Check items
                    if (map.Content.Has<Item>(i => i.Config?.Id == configId)) return true;
                    // Check lives
                    if (map.Content.Has<Life>(l => l.Config?.Id == configId)) return true;
                }
            }
            return false;
        }

        private void OnStoryComplete(params object[] args)
        {
            if (args.Length < 1) return;

            var player = args[0] as Player;
            if (player == null) return;

            // Check if player is in GiveToStele step (story was triggered from tutorial)
            var step = GetCurrentStep(player);
            if (step != Step.GiveToStele) return;

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
                // After interacting with gold mine, check if lizard is already visible
                // If so, go to SeeLizard, otherwise wait for FirstSeen event
                if (CanSeeCharacter(player, _lizardLifeId))
                {
                    AdvanceStep(player, Step.SeeLizard);
                }
                else
                {
                    // Stay at InteractGoldMine until FirstSeen triggers SeeLizard
                    // This handles the case where lizard comes into view later
                }
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
                    // Check if stele is already visible
                    if (CanSeeCharacter(player, _steleItemId))
                    {
                        AdvanceStep(player, Step.SeeStele);
                    }
                    else
                    {
                        AdvanceStep(player, Step.WalkToTower);
                    }
                }
            }
        }
        
        private bool CanSeeCharacter(Player player, int configId)
        {
            var visibleCharacters = Perception.Agent.Instance.GetVisibleCharacters(player);
            return visibleCharacters.Any(c => Perception.Agent.GetCharacterConfigId(c) == configId);
        }

        #endregion

        #region Private Methods

        private void AdvanceStep(Player player, Step nextStep)
        {
            playerStates[player.GetHashCode()] = nextStep;
            SendTutorialHint(player, nextStep);
            
            // After advancing, check if the next target is already visible
            // This handles the case where FirstSeen already fired before we entered this step
            CheckVisibilityAfterAdvance(player, nextStep);
        }
        
        /// <summary>
        /// After advancing to a new step, check if the target for next step is already visible.
        /// This fixes timing issues where FirstSeen fired before we were ready for it.
        /// </summary>
        private void CheckVisibilityAfterAdvance(Player player, Step currentStep)
        {
            switch (currentStep)
            {
                case Step.WalkToSand:
                    // If gold mine is already visible, advance to SeeGoldMine
                    if (CanSeeCharacter(player, _goldMineItemId))
                    {
                        Utils.Debug.Log.Info("TUTORIAL", $"[CheckVisibility] Gold mine already visible at WalkToSand, advancing to SeeGoldMine");
                        AdvanceStep(player, Step.SeeGoldMine);
                    }
                    break;
                    
                case Step.InteractGoldMine:
                    // If lizard is already visible, advance to SeeLizard
                    if (CanSeeCharacter(player, _lizardLifeId))
                    {
                        Utils.Debug.Log.Info("TUTORIAL", $"[CheckVisibility] Lizard already visible at InteractGoldMine, advancing to SeeLizard");
                        AdvanceStep(player, Step.SeeLizard);
                    }
                    break;
                    
                case Step.PickupItems:
                    // Check if player already has required items
                    bool hasGoldOre = player.Content.Has<Item>(i => i.Config.Id == _goldOreItemId);
                    bool hasRawMeat = player.Content.Has<Item>(i => i.Config.Id == _rawMeatItemId);
                    
                    if (hasGoldOre && hasRawMeat)
                    {
                        // If stele is already visible, advance to SeeStele
                        if (CanSeeCharacter(player, _steleItemId))
                        {
                            Utils.Debug.Log.Info("TUTORIAL", $"[CheckVisibility] Stele already visible at PickupItems, advancing to SeeStele");
                            AdvanceStep(player, Step.SeeStele);
                        }
                        else
                        {
                            // Items collected but stele not visible, guide to tower
                            Utils.Debug.Log.Info("TUTORIAL", $"[CheckVisibility] Items collected but stele not visible, advancing to WalkToTower");
                            AdvanceStep(player, Step.WalkToTower);
                        }
                    }
                    break;
            }
        }

        private void SendTutorialHint(Player player, Step step)
        {
            var (targetType, targetId, targetPath, targetPos, hintCid) = GetStepHint(step, player);
            
            // Translate hint cid to localized text
            string hintText = string.IsNullOrEmpty(hintCid) 
                ? "" 
                : Domain.Text.Agent.Instance.GetByCid(hintCid, player);
            
            Utils.Debug.Log.Info("TUTORIAL", $"[SendTutorialHint] Step={step}, TargetType={targetType}, TargetId={targetId}, TargetPos={FormatPos(targetPos)}, Hint={hintText}");
            
            var protocol = new Net.Protocol.Tutorial(
                (int)step,
                (int)targetType,
                targetId,
                targetPath,
                targetPos,
                hintText
            );

            Net.Tcp.Instance.Send(player, protocol);
        }

        private static string FormatPos(int[] pos) => pos != null ? $"[{string.Join(",", pos)}]" : "null";

        private (TargetType type, int id, string path, int[] pos, string hint) GetStepHint(Step step, Player player)
        {
            return step switch
            {
                // For Map type: use targetPos with coordinates (client matches by pos)
                Step.WalkToSand => (TargetType.Map, 0, "", GetMapPos(_tutorialSandMapId, player), ""),
                Step.WalkToTower => (TargetType.Map, 0, "", GetMapPos(_tutorialTowerMapId, player), ""),
                
                // See steps: highlight the character in list, guide player to click and use "Go" button
                Step.SeeGoldMine => (TargetType.Item, _goldMineItemId, "characters/goto", null, "tutorial_goto"),
                Step.SeeLizard => (TargetType.Creature, _lizardLifeId, "characters/goto", null, "tutorial_goto"),
                Step.SeeStele => (TargetType.Item, _steleItemId, "characters/goto", null, "tutorial_goto"),
                
                // Interact steps: player is at location, guide specific interaction
                Step.InteractGoldMine => (TargetType.Item, _goldMineItemId, "actions/interact", null, "tutorial_interact"),
                Step.AttackLizard => (TargetType.Creature, _lizardLifeId, "actions/attack", null, "tutorial_attack"),
                Step.GiveToStele => (TargetType.Item, _steleItemId, "actions/give", null, "tutorial_give"),
                
                Step.PickupItems => (TargetType.Item, 0, "actions/pickup", null, "tutorial_pickup"),
                _ => (TargetType.UI, 0, "", null, "")
            };
        }

        private int[] GetMapPos(int mapId, Player player)
        {
            // Find map in player's current copy/scene
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
    }
}
