using Logic;
using System.Linq;

namespace Domain.Story
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
            Accese(life, life.Config?.plotors);
        }

        private void OnAddMap(params object[] args)
        {
            Map map = (Map)args[1];
            if (map is not Logic.Copy.Map)
            {
                Accese(map, map.Config?.plotors);
            }
        }

        private void OnRemoveLife(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(Plot), OnAbilityRemovePlotor);
        }

        private void OnRemoveMap(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(Plot), OnAbilityRemovePlotor);
        }

        private void OnAddItem(params object[] args)
        {
            Item item = (Item)args[1];
            Accese(item, item.Config?.plotors);
        }

        private void OnRemoveItem(params object[] args)
        {
            Ability element = (Ability)args[1];
            element.Content.Remove.Unregister(typeof(Plot), OnAbilityRemovePlotor);
        }

        private void OnTrigger(Plot plot, object[] args)
        {
            var ability = (Ability)args[0];
            object[] eventArgs = args != null && args.Length > 1 ? args.Skip(1).ToArray() : new object[] { args[0] };
            
            Ability conditionTarget = eventArgs.Length > 0 && eventArgs[0] is Player player ? player : ability;
            
            bool conditionResult = plot.Config.condition?.Evaluate(conditionTarget, eventArgs) ?? true;
            if (conditionResult)
            {
                bool hasPlot = ability.Content.Has<Plot>(s => s.Config.Id == plot.Config.Id);
                if (hasPlot)
                {
                    Logic.Player targetPlayer = ability as Logic.Player ?? (eventArgs.Length > 0 ? eventArgs[0] as Logic.Player : null);
                    if (plot.Config.repeatable && targetPlayer != null)
                    {
                        Do(targetPlayer, plot, ability);
                    }
                }
                else
                {
                    ability.Add(plot);
                }
            }
        }

        private void OnAbilityRemovePlotor(params object[] args)
        {
            Ability element = (Ability)args[0];
            Plot plotor = (Plot)args[1];
            Unregister(element.monitor, plotor.Trigger);
        }

        private void OnAddPlayer(params object[] args)
        {
            Player player = (Player)args[1];
            player.Content.Add.Register(typeof(Plot), OnPlayerAddPlot);
        }

        private void OnRemovePlayer(params object[] args)
        {
            Player player = (Player)args[1];
            player.Content.Add.Unregister(typeof(Plot), OnPlayerAddPlot);
        }



        private void OnPlayerAddPlot(params object[] args)
        {
            var player = (Player)args[0];
            var sign = (Plot)args[1];

            // 尝试找到合适的触发源
            Ability source = null;

            // 1. 优先使用玩家当前正在交互的对象
            if (player.Option?.Relates?.FirstOrDefault() is Ability interactingObject)
            {
                source = interactingObject;
            }
            // 2. 如果没有交互对象，尝试找到同地图的第一个Life对象（可能是NPC）
            else if (player.Map != null)
            {
                source = player.Map.Content.Gets<Life>().FirstOrDefault(l => l != player);
            }

            Do(player, sign, source);
        }

        private void Do(Player player, Plot sign, Ability source = null)
        {
            if (DialogueSender.Can(sign))
            {
                DialogueSender.Do(player, sign);
            }

            if (Copy.Can(sign, player))
            {
                Copy.Do(sign, player);
            }
            if (Maze.Can(sign, player))
            {
                Maze.Do(sign, player);
            }

            if (Reward.Can(sign))
            {
                Reward.Do(sign, player, source);
            }
        }

        private void Accese(Ability ability, int[] plots)
        {
            if (plots != null && plots.Length > 0)
            {
                foreach (var p in plots)
                {
                    if (Exist(p, out Plot plot))
                    {
                        Integrate(ability, plot);
                    }
                    else
                    {
                        CreateAndIntegrate(ability, p);
                    }
                }
            }
            ability.Content.Remove.Register(typeof(Plot), OnAbilityRemovePlotor);
        }

        private void Integrate(Ability ability, Plot plot)
        {
            Register(ability.monitor, plot.Trigger, plot, OnTrigger);
            ability.Add(plot);
        }

        private void CreateAndIntegrate(Ability ability, int id)
        {
            Plot plot = Logic.Agent.Instance.Load<Logic.Config.Plot, Plot>(id);
            Register(ability.monitor, plot.Trigger, plot, OnTrigger);
            ability.Add(plot);
        }

        private bool Exist(int id, out Plot plot)
        {
            if (Logic.Agent.Instance.Content.Has<Plot>(p => p.Config.Id == id))
            {
                plot = Logic.Agent.Instance.Content.Get<Plot>(p => p.Config.Id == id);
                return true;
            }
            else
            {
                plot = null;
                return false;
            }
        }
    }
}
