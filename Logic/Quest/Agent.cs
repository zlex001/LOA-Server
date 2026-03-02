using Data;
using System.Linq;

namespace Logic.Quest
{
    public class Agent : Logic.Agent<Agent>
    {
        private static Agent instance;
        public static Agent Instance => instance ??= new Agent();

        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(Life), OnAddLife);
            global::Data.Agent.Instance.Content.Add.Register(typeof(Map), OnAddMap);
            global::Data.Agent.Instance.Content.Add.Register(typeof(Item), OnAddItem);
            global::Data.Agent.Instance.Content.Add.Register(typeof(Player), OnAddPlayer);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(Life), OnRemoveLife);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(Map), OnRemoveMap);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(Item), OnRemoveItem);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(Player), OnRemovePlayer);
        }

        private void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            Accese(life, life.Config?.quests);
        }

        private void OnAddMap(params object[] args)
        {
            Map map = (Map)args[1];
            if (map is not global::Data.Copy.Map)
            {
                Accese(map, map.Config?.quests);
            }
        }

        private void OnRemoveLife(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(global::Data.Quest), OnAbilityRemoveQuest);
        }

        private void OnRemoveMap(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(global::Data.Quest), OnAbilityRemoveQuest);
        }

        private void OnAddItem(params object[] args)
        {
            Item item = (Item)args[1];
            Accese(item, item.Config?.quests);
        }

        private void OnRemoveItem(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(global::Data.Quest), OnAbilityRemoveQuest);
        }

        private void OnTrigger(global::Data.Quest quest, object[] args)
        {
            var ability = (Ability)args[0];
            object[] eventArgs = args != null && args.Length > 1 ? args.Skip(1).ToArray() : new object[] { args[0] };
            
            Ability conditionTarget = eventArgs.Length > 0 && eventArgs[0] is Player player ? player : ability;
            
            bool conditionResult = quest.Config.condition?.Evaluate(conditionTarget, eventArgs) ?? true;
            if (conditionResult)
            {
                bool hasQuest = ability.Content.Has<global::Data.Quest>(s => s.Config.Id == quest.Config.Id);
                if (hasQuest)
                {
                    global::Data.Player targetPlayer = ability as global::Data.Player ?? (eventArgs.Length > 0 ? eventArgs[0] as global::Data.Player : null);
                    if (quest.Config.repeatable && targetPlayer != null)
                    {
                        Do(targetPlayer, quest, ability);
                    }
                }
                else
                {
                    ability.Add(quest);
                }
            }
        }

        private void OnAbilityRemoveQuest(params object[] args)
        {
            Ability element = (Ability)args[0];
            global::Data.Quest quest = (global::Data.Quest)args[1];
            Unregister(element.monitor, quest.Trigger);
        }

        private void OnAddPlayer(params object[] args)
        {
            Player player = (Player)args[1];
            player.Content.Add.Register(typeof(global::Data.Quest), OnPlayerAddQuest);
        }

        private void OnRemovePlayer(params object[] args)
        {
            Player player = (Player)args[1];
            player.Content.Add.Unregister(typeof(global::Data.Quest), OnPlayerAddQuest);
        }



        private void OnPlayerAddQuest(params object[] args)
        {
            var player = (Player)args[0];
            var quest = (global::Data.Quest)args[1];

            // Try to find appropriate trigger source
            Ability source = null;

            // 1. Prefer the object player is currently interacting with
            if (player.Option?.Relates?.FirstOrDefault() is Ability interactingObject)
            {
                source = interactingObject;
            }
            // 2. If no interacting object, try to find first Life object in same map (possibly NPC)
            else if (player.Map != null)
            {
                source = player.Map.Content.Gets<Life>().FirstOrDefault(l => l != player);
            }

            Do(player, quest, source);
        }

        private void Do(Player player, global::Data.Quest quest, Ability source = null)
        {
            if (DialogueSender.Can(quest))
            {
                DialogueSender.Do(player, quest);
            }

            if (Copy.Can(quest, player))
            {
                Copy.Do(quest, player);
            }
            if (Maze.Can(quest, player))
            {
                Maze.Do(quest, player);
            }

            if (Reward.Can(quest))
            {
                Reward.Do(quest, player, source);
            }
        }

        private void Accese(Ability ability, int[] quests)
        {
            if (quests != null && quests.Length > 0)
            {
                foreach (var q in quests)
                {
                    if (Exist(q, out global::Data.Quest quest))
                    {
                        Integrate(ability, quest);
                    }
                    else
                    {
                        CreateAndIntegrate(ability, q);
                    }
                }
            }
            ability.Content.Remove.Register(typeof(global::Data.Quest), OnAbilityRemoveQuest);
        }

        private void Integrate(Ability ability, global::Data.Quest quest)
        {
            Register(ability.monitor, quest.Trigger, quest, OnTrigger);
            ability.Add(quest);
        }

        private void CreateAndIntegrate(Ability ability, int id)
        {
            global::Data.Quest quest = global::Data.Agent.Instance.Load<global::Data.Config.Quest, global::Data.Quest>(id);
            Register(ability.monitor, quest.Trigger, quest, OnTrigger);
            ability.Add(quest);
        }

        private bool Exist(int id, out global::Data.Quest quest)
        {
            if (global::Data.Agent.Instance.Content.Has<global::Data.Quest>(q => q.Config.Id == id))
            {
                quest = global::Data.Agent.Instance.Content.Get<global::Data.Quest>(q => q.Config.Id == id);
                return true;
            }
            else
            {
                quest = null;
                return false;
            }
        }
    }
}
