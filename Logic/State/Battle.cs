namespace Logic.State
{
    public class Battle : Basic.StateBase<global::Data.Life.States>
    {
        public override global::Data.Life.States Key => global::Data.Life.States.Battle;
        private readonly global::Data.Life life;

        public Battle(global::Data.Life life) => this.life = life;

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
            var head = life.Content.Get<global::Data.Part>(p => p.Type == global::Data.Part.Types.Head);
            if (head != null && head.Hp <= 0)
            {
                life.State.Change(global::Data.Life.States.Unconscious);
                return;
            }

            Agent.DrainLp(life, Agent.LpDrainPerSecond);

            if (life.Agi > 0)
            {
                life.Action += Math.Log(life.Agi);
            }

            if (ShouldExitBattle())
            {
                life.State.Change(global::Data.Life.States.Normal);
            }
        }

        private bool ShouldExitBattle()
        {
            var targets = Logic.Battle.Target.Get(life);
            return targets.Count == 0;
        }
    }
}
