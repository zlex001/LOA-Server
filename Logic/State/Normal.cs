using Aop.Api.Domain;
using System.Linq;

namespace Logic.State
{
    public class Normal : Basic.StateBase<global::Data.Life.States>
    {
        public override global::Data.Life.States Key => global::Data.Life.States.Normal;
        private global::Data.Life Life { get; set; }

        public Normal(global::Data.Life life) => Life = life;

        private bool HasSuitableFood(out global::Data.Item food)
        {
            food = null;
            List<global::Data.Item> items = Logic.Exchange.Agent.GetItems(Life, i => i.Type == global::Data.Item.Types.Food && i.Config.value <= Life.data.GetLoss<double>(global::Data.Life.Data.Lp));
            if (items.Count == 0) return false;
            food = items.First();
            return true;
        }

        protected override void OnEnter(object context)
        {
            if (Life.Action > 0 || Life.Round > 0)
            {
                Life.Action = 0;
                Life.Round = 0;
            }

            Agent.StartBehaviorTree(Life);
        }

        protected override void OnExit(object context)
        {
            Agent.StopBehaviorTree(Life);
        }

        public override void Update(object context)
        {
            var head = Life.Content.Get<global::Data.Part>(p => p.Type == global::Data.Part.Types.Head);
            if (head != null && head.Hp <= 0)
            {
                Life.State.Change(global::Data.Life.States.Unconscious);
                return;
            }

            Agent.DrainLp(Life, Agent.LpDrainPerSecond);

            if (HasSuitableFood(out global::Data.Item food))
            {
                Logic.Use.Agent.Instance.Do(Life, food);
            }

            var handItem = Logic.Exchange.Agent.GetHandleItem(Life);
            if (handItem != null && handItem.EquipPart != null)
            {
                var targetPart = Life.Content.Get<global::Data.Part>(p => p.Type == handItem.EquipPart.Value);
                if (targetPart != null && !targetPart.Content.Has<global::Data.Item>())
                {
                    if (!Life.ManualUnequippedItems.Contains(handItem.Config.Id))
                    {
                        Logic.Exchange.Equip.Instance.Do(Life, handItem);
                    }
                }
            }

            if (Logic.Battle.Target.Get(Life).Count > 0)
            {
                Life.State.Change(global::Data.Life.States.Battle);
            }
        }

    }
}
