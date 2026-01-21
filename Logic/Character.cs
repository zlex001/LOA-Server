using Logic.Config;

namespace Logic
{
    public class Character : Ability
    {
        public enum Event
        {
            Broadcast,
            Given
        }
        public enum Data
        {
            Map
        }
        public Map Map { get => data.Get<Map>(Data.Map); set => data.Change(Data.Map, value, this); }

    }
    public class Character<TConfig> : Character where TConfig : ICharacter
    {
        public TConfig Config { get; set; }
    }

}