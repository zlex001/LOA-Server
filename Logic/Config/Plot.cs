using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Config
{
    public class Plot : Ability
    {
        public string trigger;
        public Copy copy;
        public int maze;
        public int[] dialogues;
        public ConditionNode condition;
        public bool repeatable;
        public List<(string type, int id, int amount)> rewards;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            trigger = Get<string>(dict, "trigger");
            var copyJson = Get<string>(dict, "copy");
            copy = string.IsNullOrEmpty(copyJson) || copyJson == "null" ? new Copy { scope = 0, characters = new Dictionary<int, List<Character>>() } : Utils.Json.Deserialize<Copy>(copyJson);
            maze = Get<int>(dict, "maze");
            dialogues = Utils.Json.Deserialize<int[]>(Get<string>(dict, "dialogues"));
            var conditionJson = Get<string>(dict, "condition");
            condition = string.IsNullOrEmpty(conditionJson) ? null : Utils.Json.Deserialize<ConditionNode>(conditionJson);
            repeatable = Get<bool>(dict, "repeatable");
            var rawRewards = Utils.Json.Deserialize<List<List<object>>>(Get<string>(dict, "reward"));
            rewards = rawRewards.Select(r => ((string)r[0], System.Convert.ToInt32(r[1]), System.Convert.ToInt32(r[2]))).ToList();

        }

        public class Loot
        {
            public int id { get; set; }
            public double probability { get; set; }
            public int minCount { get; set; }
            public int maxCount { get; set; }
        }

        public class Character
        {
            public int id { get; set; }
            public int count { get; set; }
            public int? min { get; set; }
            public int? max { get; set; }
            public List<Loot> loot { get; set; }
            public List<Character> nested { get; set; }
        }

        public class Copy
        {
            public int scope { get; set; }
            public Dictionary<int, List<Character>> characters { get; set; }
        }
    }
}