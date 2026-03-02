using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Data.Config
{
    public class Movement : Ability, ITag
    {
        public int description;
        public ConditionNode require;
        public Part.Types[] target;
        public string effects;
        public double cd;
        public List<string> Tags { get; private set; }

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            description = Get<int>(dict, "description");
            var requireJson = Get<string>(dict, "require");
            require = string.IsNullOrEmpty(requireJson) ? null : Utils.Json.Deserialize<ConditionNode>(requireJson);
            var targetJson = Get<string>(dict, "target");
            target = string.IsNullOrEmpty(targetJson) ? null : Utils.Json.Deserialize<Part.Types[]>(targetJson);
            effects = Get<string>(dict, "effects");
            cd = Get<double>(dict, "cd");
            Tags = Utils.Json.Deserialize<List<string>>(Get<string>(dict, "tags"));
            text = Utils.Json.Deserialize<Dictionary<string, List<string>>>(Get<string>(dict, "text"));
        }

        public IEnumerable<string> GetTags() => Tags ?? Enumerable.Empty<string>();
    }
}
