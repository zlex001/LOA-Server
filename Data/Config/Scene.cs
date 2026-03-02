using System.Collections.Generic;
using System.Linq;
using System;

namespace Data.Config
{
    public class Scene : Ability, IName
    {
        public int Name { get; private set; }
        public string Type { get; private set; }
        public Dictionary<string, string[]> exit;
        public List<int[]> content;
        public List<(int id, int count, int? minCount, int? maxCount, int? minLevel, int? maxLevel, double probability)> Characters { get; private set; }

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            Name = Get<int>(dict, "name");
            Type = Get<string>(dict, "type") ?? "";
            var characterList = Utils.Json.Deserialize<List<Dictionary<string, object>>>(Get<string>(dict, "characters"));
            Characters = new List<(int, int, int?, int?, int?, int?, double)>();

            foreach (var d in characterList)
            {
                int id = (int)(long)d["id"];
                int count = d.ContainsKey("count") ? (int)(long)d["count"] : 1;
                int? minCount = d.ContainsKey("minCount") ? (int?)(long)d["minCount"] : null;
                int? maxCount = d.ContainsKey("maxCount") ? (int?)(long)d["maxCount"] : null;
                int? minLevel = d.ContainsKey("minLevel") ? (int?)(long)d["minLevel"] : null;
                int? maxLevel = d.ContainsKey("maxLevel") ? (int?)(long)d["maxLevel"] : null;
                double probability = d.ContainsKey("probability") ? (double)d["probability"] : 1.0;

                Characters.Add((id, count, minCount, maxCount, minLevel, maxLevel, probability));
            }
        }

    }
}


