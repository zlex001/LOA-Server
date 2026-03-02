using Basic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Design
{
    public class Step : global::Data.Design.Ability
    {
        public string Cid { get; set; }
        public string name;
        public string type;
        public int order;
        public int next;
        public string map;
        public string target;
        public string trigger;
        public string goal;
        public string description;
        public string reward;
        public int weight;
        public string save;
        public string copy;

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            copy = Get<string>(dict, "copy");
            description = Get<string>(dict, "description");
            goal = Get<string>(dict, "goal");
            map = Get<string>(dict, "map");
            next = Get<int>(dict, "next");
            order = Get<int>(dict, "order");
            reward = Get<string>(dict, "reward");
            save = Get<string>(dict, "save");
            target = Get<string>(dict, "target");
            trigger = Get<string>(dict, "trigger");
            type = Get<string>(dict, "type");
            weight = Get<int>(dict, "weight");
            Cid = Get<string>(dict, "id");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (Step config in Agent.Instance.Content.Gets<Step>())
            {
                Dictionary<string, int> reward = config.reward?.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToDictionary(r => r.Split(':')[0], r => System.Convert.ToInt32(r.Split(':')[1])) ?? new Dictionary<string, int>();

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.Cid },
                    {"order", config.order },
                    {"next", config.next },
                    {"trigger", config.trigger },
                    {"target", config.target },
                    {"goal", config.goal },
                    {"map", config.map },
                    {"type", config.type },
                    {"weight", config.weight },
                    {"save", config.save },
                    {"reward", JsonConvert.SerializeObject(reward) },
                    {"copy", config.copy },
                    {"text", JsonConvert.SerializeObject(new Dictionary<string, List<string>>()) },
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Step.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
}

