using System.Collections.Generic;

namespace Data.Config
{
    public class BehaviorTree : Ability
    {
        public int Name { get; private set; }
        public string type;
        public int[] nodes;
        public double IntervalMultiplier { get; private set; } = 1.0;
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            Name = Get<int>(dict, "name");
            type = Get<string>(dict, "type");
            nodes = Utils.Json.Deserialize<int[]>(Get<string>(dict, "nodes"));
            var multiplier = Get<double>(dict, "interval_multiplier");
            IntervalMultiplier = multiplier > 0 ? multiplier : 1.0;
        }
    }
 
}