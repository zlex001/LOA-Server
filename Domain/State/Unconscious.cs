using System;
using System.Linq;

namespace Domain.State
{
    public class Unconscious : Basic.StateBase<Logic.Life.States>
    {
        public override Logic.Life.States Key => Logic.Life.States.Unconscious;
        private Logic.Life Parent { get; set; }

        public Unconscious(Logic.Life life) => Parent = life;

        const int MaxInjury = 10;

        const double SecondsPerGameHour = 3600.0 / Time.Agent.Rate;

        protected override void OnEnter(object context)
        {
            Parent.Injury = Math.Min(Parent.Injury + 1, MaxInjury);
            int gameHours = Parent.Injury * Parent.Injury;
            double realSeconds = gameHours * SecondsPerGameHour;
            Parent.WakeUpTime = DateTime.Now.AddSeconds(realSeconds);
            Parent.FaintDateTime = DateTime.Now;
            
            Broadcast.Instance.Local(Parent, [Text.Agent.Instance.Id(Logic.Text.Labels.Faint)], ("sub", Parent));
            
            if (Parent.Map != null)
            {
                var hostiles = Parent.Map.Content.Gets<Logic.Life>().Where(life => 
                    life != Parent && 
                    life.Relation.TryGetValue(Parent, out var relationValue) && 
                    relationValue < 0);
                
                foreach (var hostile in hostiles)
                {
                    Develop.Experience.GiveBattleExp(hostile, Parent);
                    
                    // Check for tutorial lizard defeat
                    // Fire global event for life defeated (tutorial, achievements, quests, etc.)
                    if (hostile is Logic.Player player)
                    {
                        Logic.Agent.Instance.monitor.Fire(Logic.Life.Event.Die, player, Parent);
                    }
                }
            }
        }

        protected override void OnExit(object context)
        {
            var head = Parent.Content.Get<Logic.Part>(p => p.Type == Logic.Part.Types.Head);
            if (head != null && head.Hp <= 0)
            {
                head.Hp = 1;
            }
            
            Broadcast.Instance.Local(Parent, [Text.Agent.Instance.Id(Logic.Text.Labels.WakeUp)], ("sub", Parent));
        }

        public override void Update(object context)
        {
            Agent.DrainLp(Parent, Agent.LpDrainPerSecond);

            if (DateTime.Now >= Parent.WakeUpTime)
            {
                Parent.State.Change(Logic.Life.States.Normal);
            }
            else
            {
                RefreshViewers();
            }
        }

        private void RefreshViewers()
        {
            foreach (var player in Logic.Agent.Instance.Content.Gets<Logic.Player>())
            {
                var options = player.Content.Gets<Logic.Option>();
                if (options.Any(opt => opt.Relates.Contains(Parent)))
                {
                    Display.Agent.Instance.Refresh(player);
                }
            }
        }
    }
}
