using Logic;
using System.Linq;

namespace Domain.Quest
{
    public class Agent : Domain.Agent<Agent>
    {
        private static Agent instance;
        public static Agent Instance => instance ??= new Agent();

        public void Init()
        {
            Logic.Agent.Instance.Content.Add.Register(typeof(Life), OnAddLife);
            Logic.Agent.Instance.Content.Add.Register(typeof(Map), OnAddMap);
            Logic.Agent.Instance.Content.Add.Register(typeof(Item), OnAddItem);
            Logic.Agent.Instance.Content.Add.Register(typeof(Player), OnAddPlayer);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Life), OnRemoveLife);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Map), OnRemoveMap);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Item), OnRemoveItem);
            Logic.Agent.Instance.Content.Remove.Register(typeof(Player), OnRemovePlayer);
        }

        private void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            Accese(life, life.Config?.quests);
        }

        private void OnAddMap(params object[] args)
        {
            Map map = (Map)args[1];
            if (map is not Logic.Copy.Map)
            {
                Accese(map, map.Config?.quests);
            }
        }

        private void OnRemoveLife(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(Logic.Quest), OnAbilityRemoveQuest);
        }

        private void OnRemoveMap(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(Logic.Quest), OnAbilityRemoveQuest);
        }

        private void OnAddItem(params object[] args)
        {
            Item item = (Item)args[1];
            Accese(item, item.Config?.quests);
        }

        private void OnRemoveItem(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(Logic.Quest), OnAbilityRemoveQuest);
        }

        private void OnTrigger(Logic.Quest quest, object[] args)
        {
            var ability = (Ability)args[0];
            object[] eventArgs = args != null && args.Length > 1 ? args.Skip(1).ToArray() : new object[] { args[0] };
            
            Ability conditionTarget = eventArgs.Length > 0 && eventArgs[0] is Player player ? player : ability;
            
            bool conditionResult = quest.Config.condition?.Evaluate(conditionTarget, eventArgs) ?? true;
            if (conditionResult)
            {
                bool hasQuest = ability.Content.Has<Logic.Quest>(s => s.Config.Id == quest.Config.Id);
                if (hasQuest)
                {
                    Logic.Player targetPlayer = ability as Logic.Player ?? (eventArgs.Length > 0 ? eventArgs[0] as Logic.Player : null);
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
            Logic.Quest quest = (Logic.Quest)args[1];
            Unregister(element.monitor, quest.Trigger);
        }

        private void OnAddPlayer(params object[] args)
        {
            Player player = (Player)args[1];
            player.Content.Add.Register(typeof(Logic.Quest), OnPlayerAddQuest);
        }

        private void OnRemovePlayer(params object[] args)
        {
            Player player = (Player)args[1];
            player.Content.Add.Unregister(typeof(Logic.Quest), OnPlayerAddQuest);
        }



        private void OnPlayerAddQuest(params object[] args)
        {
            var player = (Player)args[0];
            var quest = (Logic.Quest)args[1];

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

        private void Do(Player player, Logic.Quest quest, Ability source = null)
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
                    if (Exist(q, out Logic.Quest quest))
                    {
                        Integrate(ability, quest);
                    }
                    else
                    {
                        CreateAndIntegrate(ability, q);
                    }
                }
            }
            ability.Content.Remove.Register(typeof(Logic.Quest), OnAbilityRemoveQuest);
        }

        private void Integrate(Ability ability, Logic.Quest quest)
        {
            Register(ability.monitor, quest.Trigger, quest, OnTrigger);
            ability.Add(quest);
        }

        private void CreateAndIntegrate(Ability ability, int id)
        {
            Logic.Quest quest = Logic.Agent.Instance.Load<Logic.Config.Quest, Logic.Quest>(id);
            Register(ability.monitor, quest.Trigger, quest, OnTrigger);
            ability.Add(quest);
        }

        private bool Exist(int id, out Logic.Quest quest)
        {
            if (Logic.Agent.Instance.Content.Has<Logic.Quest>(q => q.Config.Id == id))
            {
                quest = Logic.Agent.Instance.Content.Get<Logic.Quest>(q => q.Config.Id == id);
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
