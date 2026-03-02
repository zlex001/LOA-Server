using Basic;
using Data.Config;


namespace Data
{
    public class Ability : Basic.Ability
    {

        public enum Data
        {
            DeathTimestamp,
            Behavior,
            Function,
            State,
            ProbabilityInc,
        }

        public Ability() : base()
        {
            Content.Add.Register(typeof(Ability), OnContentAddObj);
            Content.Remove.Register(typeof(Ability), OnContentRemoveObj);
        }
        public override void Release()
        {
            // 只清理事件监听器，不递归销毁子对象
            // 设计说明：
            // - 子对象的内存由GC自动回收（当没有外部引用时）
            // - 不需要手动递归Destroy，避免触发不必要的副作用
            // - 如需主动清理子对象，应在具体业务逻辑中显式处理
            base.Release();
        }
        private void OnContentAddObj(params object[] args)
        {
            object obj = args[1];
            if (obj is not Option.Settings)
            {
                var allOptions = Agent.Instance.Content.Gets<Option>().Where(opt => opt.Relates.Contains(this));
                if (allOptions.Any())
                {
                    monitor.Fire(Option.Event.Update);
                }
            }
        }
        private void OnContentRemoveObj(params object[] args)
        {
            Ability obj = (Ability)args[1];
            var allOptions = Agent.Instance.Content.Gets<Option>().Where(opt => opt.Relates.Contains(obj));
            if (allOptions.Any())
            {
                obj.monitor.Fire(Option.Event.Delete);
            }
        }
        public bool Has(object o) => Content.Has<object>(i => i == o);
        public virtual bool Touchable(Character sub)
        {
            return false;
        }
        public object[] GenerateOptionItems(Option.Item.Type itemType, params string[] options)
        {
            return options.Select(option => new Option.Item(itemType, option)).Cast<object>().ToArray();
        }
    }
    public class Ability<TConfig> : Ability where TConfig : Config.Ability, IName
    {
        public TConfig Config { get; set; }


    }
}
