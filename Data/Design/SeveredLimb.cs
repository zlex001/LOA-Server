using System.Collections.Generic;
using Basic;

namespace Data.Design
{
    /// <summary>
    /// 断肢道具映射配置 - 从策划表加载
    /// </summary>
    public class SeveredLimb : Ability
    {
        public string part_type;    // 对应Part.Types的字符串名称，如"Head"、"Chest"等
        public string item_cid;     // 对应断肢道具的CID，如"断肢_头部"、"断肢_胸部"等
        public string description;  // 配置说明
        
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            part_type = Get<string>(dict, "part_type");
            item_cid = Get<string>(dict, "item_cid");
            description = Get<string>(dict, "description");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
            foreach (SeveredLimb config in Agent.Instance.Content.Gets<SeveredLimb>())
            {
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", config.id },
                    {"part_type", config.part_type },
                    {"item_cid", config.item_cid },
                    {"description", config.description },
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/SeveredLimb.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
} 