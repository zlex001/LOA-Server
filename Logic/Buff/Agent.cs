using Data;

namespace Logic.Buff
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(Life), OnAddLife);
            Time.Agent.Instance.Scheduler.Repeat(1000, (_) => OnTick());
        }

        private void OnAddLife(params object[] args)
        {
            Life life = (Life)args[1];
            life.Content.Add.Register(typeof(global::Data.Buff), OnAddBuff);
            life.Content.Remove.Register(typeof(global::Data.Buff), OnRemoveBuff);
        }

        private void OnAddBuff(params object[] args)
        {
            global::Data.Buff buff = (global::Data.Buff)args[1];
            Life life = buff.Parent as Life;
            
            if (life == null) return;
            
            if (buff.Config.Broadcasts.TryGetValue("Add", out int langKey))
            {
                Broadcast.Instance.Local(life, [langKey], ("sub", life), ("buff", buff));
            }
        }

        private void OnRemoveBuff(params object[] args)
        {
            global::Data.Buff buff = (global::Data.Buff)args[1];
            Life life = args.Length > 2 ? args[2] as Life : null;
            
            if (life == null) return;
            
            if (buff.Config.Broadcasts.TryGetValue("Remove", out int langKey))
            {
                Broadcast.Instance.Local(life, [langKey], ("sub", life), ("buff", buff));
            }
        }

        private void OnTick()
        {
            var allLives = global::Data.Agent.Instance.Content.Gets<Life>();
            foreach (Life life in allLives)
            {
                var buffs = life.Content.Gets<global::Data.Buff>().ToList();
                foreach (global::Data.Buff buff in buffs)
                {
                    buff.RemainingTime -= 1;
                    
                    if (buff.RemainingTime <= 0)
                    {
                        life.Remove(buff);
                    }
                    else if (buff.Config.Interval > 0 && buff.Duration - buff.RemainingTime > 0)
                    {
                        double elapsed = buff.Duration - buff.RemainingTime;
                        if (elapsed % buff.Config.Interval == 0)
                        {
                            if (buff.Config.Broadcasts.TryGetValue("Tick", out int langKey))
                            {
                                Broadcast.Instance.Local(life, [langKey], ("sub", life), ("buff", buff));
                            }
                        }
                    }
                }
            }
        }

        public static double GetConcealmentValue(Life life)
        {
            if (life == null) return 0;
            
            return life.Content.Gets<global::Data.Buff>(b => b.Config.Id == global::Data.Constant.ConcealmentBuff)
                .Sum(b => b.EffectValue);
        }
    }
}

