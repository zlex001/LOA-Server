using Data;

namespace Logic.Talk
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        public void Init()
        {
            Function.Instance.Init();
        }
        
        public bool Can(Life sub, Life obj)
        {
            return obj != null && obj is not Player && sub != obj && !obj.State.Is(Life.States.Unconscious);
        }

        public void Do(Life sub, Life obj)
        {
            Function.Instance.Do(obj.Config, sub);
            obj.monitor.Fire(global::Data.Life.Event.Talked, obj, sub);
        }
    }
}
