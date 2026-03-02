using Data;

namespace Logic.Battle
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }
        
        public bool CanHostile(Life sub, Life obj)
        {
            return sub != null && obj != null && sub != obj && 
                   !sub.State.Is(Life.States.Unconscious) && 
                   !obj.State.Is(Life.States.Unconscious);
        }

        public void Hostile(Life sub, Life obj)
        {
            Broadcast.Instance.Local(obj, [Text.Agent.Instance.Id(global::Data.Text.Labels.Hostile)], ("sub", sub), ("obj", obj));
            Logic.Relation.Interact(sub, obj, Relation.Reason.Hostile);
            
            SyncCompanionRelation(sub, obj);
            SyncCompanionRelation(obj, sub);
            
            if (Logic.Justice.Assault.Check(sub, obj))
            {
                Logic.Justice.Assault.Judge(sub, obj);
            }
        }

        private void SyncCompanionRelation(Life life, Life enemy)
        {
            if (life == null || enemy == null || life.Map == null) return;
            
            var companions = life.Map.Content.Gets<Life>(l => l.Leader == life);
            foreach (var companion in companions)
            {
                Logic.Relation.Do(companion, enemy, Relation.Reason.Hostile);
                Logic.Relation.Do(enemy, companion, Relation.Reason.Hostile);
            }
        }
        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(Life), OnAddLife);
        }

        private void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            life.data.after.Register(Life.Data.Action, OnLifeActionChanged);
        }

        private void OnLifeActionChanged(params object[] args)
        {
            double v = (double)args[0];
            Life life = (Life)args[1];
            
            if (ShouldStartRound(v, life))
            {
                StartBattleRound(life, v);
            }
        }

        private bool ShouldStartRound(double actionValue, Life life)
        {
            return actionValue >= 100 && life != null;
        }

        private void StartBattleRound(Life life, double actionValue)
        {
            Round.Do(life);
            life.data.raw[Life.Data.Action] = actionValue % 100;
        }





    }
}
