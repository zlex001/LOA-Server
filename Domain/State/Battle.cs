namespace Domain.State
{
    public class Battle : Basic.StateBase<Logic.Life.States>
    {
        public override Logic.Life.States Key => Logic.Life.States.Battle;
        private readonly Logic.Life life;

        public Battle(Logic.Life life) => this.life = life;

        protected override void OnEnter(object context)
        {
        }

        protected override void OnExit(object context)
        {
            if (life.Action > 0) life.Action = 0;
            if (life.Round > 0) life.Round = 0;
        }

        public override void Update(object context)
        {
            var head = life.Content.Get<Logic.Part>(p => p.Type == Logic.Part.Types.Head);
            if (head != null && head.Hp <= 0)
            {
                life.State.Change(Logic.Life.States.Unconscious);
                return;
            }

            Agent.DrainLp(life, Agent.LpDrainPerSecond);

            if (life.Agi > 0)
            {
                life.Action += Math.Log(life.Agi);
            }

            if (ShouldExitBattle())
            {
                life.State.Change(Logic.Life.States.Normal);
            }
        }

        private bool ShouldExitBattle()
        {
            var targets = Domain.Battle.Target.Get(life);
            return targets.Count == 0;
        }
    }
}
