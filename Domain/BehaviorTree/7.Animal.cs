using Logic;
using System.Linq;
using Utils;

namespace Domain.BehaviorTree
{
    /// <summary>
    /// Animal business category - corresponds to 007xxxx series nodes
    /// Including forage, devour meat, hunt and other animal survival behavior tree nodes
    /// </summary>
    public class Animal
    {

        /// <summary>
        /// Condition:71001 - Is Moderately Hungry
        /// Check if Lp is between 40%-59% (moderate hunger level)
        /// </summary>
        [BehaviorCondition(71001)]
        public static bool IsModeratelyHungry(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            
            float lpRatio = (float)(life.Lp / life.MaxLp);
            return lpRatio >= 0.4f && lpRatio <= 0.59f;
        }

        /// <summary>
        /// Condition:71002 - Is Forage Target Adjacent
        /// Check if forage target (food matching favorite tag) is adjacent
        /// </summary>
        [BehaviorCondition(71002)]
        public static bool IsForageTargetAdjacent(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            
            var items = life.Map.Content.Gets<Item>();
            return items.Any(item => IsFavoriteItem(life, item));
        }

        /// <summary>
        /// Condition:71003 - Is Carnivorous
        /// Check if the animal has carnivorous tag
        /// </summary>
        [BehaviorCondition(71003)]
        public static bool IsCarnivorous(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            
            return life.Config.Tags.Has("Carnivorous");
        }

        /// <summary>
        /// Condition:71004 - Is Raw Meat Target Adjacent
        /// Check if raw meat item is on current map
        /// </summary>
        [BehaviorCondition(71004)]
        public static bool IsRawMeatTargetAdjacent(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            
            var items = life.Map.Content.Gets<Item>();
            return items.Any(item => item.Config.Id == Logic.Constant.RawMeat);
        }

        /// <summary>
        /// Condition:71005 - Is Hostile Target Adjacent
        /// Check if unconscious hostile enemy target is on current map
        /// </summary>
        [BehaviorCondition(71005)]
        public static bool IsHostileTargetAdjacent(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            
            // Find unconscious hostile targets on current map
            return life.Map.Content.Has<Life>(l => 
                l != life && 
                l.State.Is(Logic.Life.States.Unconscious) &&
                life.Relation.TryGetValue(l, out var relationValue) && relationValue < 0);
        }

        /// <summary>
        /// Condition:71006 - Does Prey Target Exist
        /// Check if there exists a weaker animal to hunt in birth scene and adjacent scenes
        /// </summary>
        [BehaviorCondition(71006)]
        public static bool HasPreyTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            
            return FindPreyTarget(life) != null;
        }

        /// <summary>
        /// Action:72001 - Forage
        /// Directly consume food matching favorite tag
        /// </summary>
        [BehaviorAction(72001)]
        public static bool Forage(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            
            var items = life.Map.Content.Gets<Item>();
            var target = items.FirstOrDefault(item => IsFavoriteItem(life, item));
            if (target == null) return false;
            
            Domain.Use.Agent.Instance.Do(life, target);
            return true;
        }

        /// <summary>
        /// Action:72002 - Auto Pathfind to Forage Target
        /// Find forage target and walk to it
        /// </summary>
        [BehaviorAction(72002)]
        public static bool AutoPathToForageTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            foreach (var scene in life.WorkingScenes)
            {
                var targetMap = scene.Content.Get<Map>(m => 
                    m.Content.Gets<Item>().Any(item => IsFavoriteItem(life, item)));
                
                if (targetMap != null)
                {
                    Domain.Move.Walk.FollowShortest(life, targetMap);
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Action:72003 - Devour Meat
        /// Pick up and consume raw meat from ground
        /// </summary>
        [BehaviorAction(72003)]
        public static bool DevourMeat(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            
            var items = life.Map.Content.Gets<Item>();
            var meat = items.FirstOrDefault(item => item.Config.Id == Logic.Constant.RawMeat);
            if (meat == null) return false;
            
            Domain.Use.Agent.Instance.Do(life, meat);
            return true;
        }

        /// <summary>
        /// Action:72004 - Auto Pathfind to Raw Meat Target
        /// Find raw meat and walk to it
        /// </summary>
        [BehaviorAction(72004)]
        public static bool AutoPathToRawMeatTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.WorkingScenes == null || life.WorkingScenes.Count == 0) return false;
            
            foreach (var scene in life.WorkingScenes)
            {
                var targetMap = scene.Content.Get<Map>(m => 
                    m.Content.Gets<Item>().Any(item => item.Config.Id == Logic.Constant.RawMeat));
                
                if (targetMap != null)
                {
                    Domain.Move.Walk.FollowShortest(life, targetMap);
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Action:72005 - Dismember
        /// Attack unconscious hostile enemy to obtain meat
        /// </summary>
        [BehaviorAction(72005)]
        public static bool Dismember(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            if (life.Map == null) return false;
            
            var target = life.Map.Content.Get<Life>(l => 
                l != life && 
                l.State.Is(Logic.Life.States.Unconscious) &&
                life.Relation.TryGetValue(l, out var relationValue) && relationValue < 0);
            
            if (target == null) return false;
            
            Domain.Battle.Round.DoAttack(life, target);
            return true;
        }

        /// <summary>
        /// Action:72006 - Hunt Prey
        /// Establish hostility with selected prey target
        /// </summary>
        [BehaviorAction(72006)]
        public static bool HuntPrey(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            
            var target = FindPreyTarget(life);
            if (target == null) return false;
            
            Domain.Battle.Agent.Instance.Hostile(life, target);
            return true;
        }

        /// <summary>
        /// Action:72007 - Auto Pathfind to Prey Target
        /// Find prey target and walk to it
        /// </summary>
        [BehaviorAction(72007)]
        public static bool AutoPathToPreyTarget(Character character)
        {
            var life = character as Logic.Life;
            if (life == null) return false;
            
            var target = FindPreyTarget(life);
            if (target == null) return false;
            if (target.Map == null) return false;
            
            Domain.Move.Walk.FollowShortest(life, target.Map);
            return true;
        }

        /// <summary>
        /// Check if item matches animal's favorite food tags
        /// Uses exact match instead of substring for better performance
        /// </summary>
        private static bool IsFavoriteItem(Life life, Item item)
        {
            if (life?.Config?.Tags == null) return false;
            if (item?.Config?.Tags == null) return false;
            
            var favorites = life.Config.Tags.GetValues("Favorite");
            if (favorites.Count == 0) return false;
            
            foreach (var favorite in favorites)
            {
                // Exact match in item tags
                if (item.Config.Tags.Contains(favorite))
                    return true;
                
                // Check cooking values
                var cookingValues = item.Config.Tags.GetValues("Cook");
                if (cookingValues != null && cookingValues.Contains(favorite))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Find prey target: weaker animal in birth scene and adjacent scenes
        /// Target must be: Animal category, not unconscious, lower level than hunter, in working area
        /// </summary>
        private static Life FindPreyTarget(Life hunter)
        {
            if (hunter == null) return null;
            if (hunter.WorkingScenes == null || hunter.WorkingScenes.Count == 0) return null;
            
            foreach (var scene in hunter.WorkingScenes)
            {
                var prey = scene.Content.Get<Life>(l => 
                    l != hunter &&
                    l.Category == Logic.Life.Categories.Animal &&
                    !l.State.Is(Logic.Life.States.Unconscious) &&
                    l.Level < hunter.Level);
                
                if (prey != null) return prey;
            }
            
            return null;
        }
    }
}
