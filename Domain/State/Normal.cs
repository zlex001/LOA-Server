using Aop.Api.Domain;
using System.Linq;

namespace Domain.State
{
    public class Normal : Basic.StateBase<Logic.Life.States>
    {
        public override Logic.Life.States Key => Logic.Life.States.Normal;
        private Logic.Life Life { get; set; }

        public Normal(Logic.Life life) => Life = life;

        private bool HasSuitableFood(out Logic.Item food)
        {
            food = null;
            List<Logic.Item> items = Domain.Exchange.Agent.GetItems(Life, i => i.Type == Logic.Item.Types.Food && i.Config.value <= Life.data.GetLoss<double>(Logic.Life.Data.Lp));
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
            var head = Life.Content.Get<Logic.Part>(p => p.Type == Logic.Part.Types.Head);
            if (head != null && head.Hp <= 0)
            {
                Life.State.Change(Logic.Life.States.Unconscious);
                return;
            }

            Agent.DrainLp(Life, Agent.LpDrainPerSecond);

            if (HasSuitableFood(out Logic.Item food))
            {
                Domain.Use.Agent.Instance.Do(Life, food);
            }

            var handItem = Domain.Exchange.Agent.GetHandleItem(Life);
            if (handItem != null && handItem.EquipPart != null)
            {
                var targetPart = Life.Content.Get<Logic.Part>(p => p.Type == handItem.EquipPart.Value);
                if (targetPart != null && !targetPart.Content.Has<Logic.Item>())
                {
                    if (!Life.ManualUnequippedItems.Contains(handItem.Config.Id))
                    {
                        Domain.Exchange.Equip.Instance.Do(Life, handItem);
                    }
                }
            }

            if (Domain.Battle.Target.Get(Life).Count > 0)
            {
                Life.State.Change(Logic.Life.States.Battle);
            }
        }

    }
}
