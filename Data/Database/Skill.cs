using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;
using Utils;

namespace Data.Database
{
    [Serializable]
    public class Skill : Basic.Data
    {
        public int Id { get; set; }
        public int Exp { get; set; }
        public int Level { get; set; }

        public Skill() { }
        public Skill(int id)
        {
            Id = id;
        }
        public Skill(global::Data.Skill skill)
        {
            Id = skill.Config.Id;
            Exp = skill.Exp;
            Level = skill.Level;
        }

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "Id");
            Exp = Get<int>(dict, "Exp");
            Level = Get<int>(dict, "Level");
        }

        public override Dictionary<string, object> ToDictionary
        {
            get
            {
                var dict = new Dictionary<string, object>
                {
                    ["Id"] = Id,
                    ["Exp"] = Exp,
                    ["Level"] = Level,
                };
                return dict;
            }
        }
    }
}
