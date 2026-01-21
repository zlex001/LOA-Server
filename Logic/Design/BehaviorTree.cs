using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Basic;
using System.Text;


namespace Logic.Design
{
    public class BehaviorTree : Ability
    {
        public string name;
        public string type;
        public string nodes;
        public double interval_multiplier;
        
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            name = Get<string>(dict, "name");
            type = Get<string>(dict, "type");
            nodes = Get<string>(dict, "nodes");
            
            try
            {
                var multiplier = Get<double>(dict, "interval_multiplier");
                interval_multiplier = multiplier > 0 ? multiplier : 1.0;
            }
            catch
            {
                interval_multiplier = 1.0;
            }
        }


        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (BehaviorTree behaviorTree in Logic.Design.Agent.Instance.Content.Gets<BehaviorTree>())
            {
                int[] nodeIds = string.IsNullOrEmpty(behaviorTree.nodes)
                    ? Array.Empty<int>()
                    : behaviorTree.nodes.Split(',')
                        .Select(cid => Logic.Design.Agent.Instance.Content.Get<BehaviorTree>(bt => bt.cid == cid.Trim()))
                        .Where(bt => bt != null)
                        .Select(bt => bt.id)
                        .ToArray();


                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", behaviorTree.id },
                    {"name", Agent.Instance.Content.Get<Multilingual>(m=>m.cid==behaviorTree.name).id },
                    {"type", behaviorTree.type },
                    {"nodes", JsonConvert.SerializeObject(nodeIds) },
                    {"interval_multiplier", behaviorTree.interval_multiplier },
                };
                datas.Add(data);
            }
            
            string path = $"{Utils.Paths.Library}/Config/BehaviorTree.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }

    }
}


