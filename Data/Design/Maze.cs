using System;
using System.Collections.Generic;
using Basic;
using Newtonsoft.Json;
using System.Linq;

namespace Data.Design
{
    public class Maze :Ability
    {
        public int width;
        public int height;
        public float fillRate;
        public int iterations;
        public string fixedRooms;  // 策划填写格式如下，每行一条，使用 Excel 中的换行
                                   // boss_room:1
                                   // trap_room:3
        public string roomPool;    // 策划填写格式如下，每行一条，使用 Excel 中的换行
                                   // normal_1*0.6
                                   // normal_2*0.4

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            width = Get<int>(dict, "width");
            height = Get<int>(dict, "height");
            fillRate = Get<float>(dict, "fillRate");
            iterations = Get<int>(dict, "iterations");
            fixedRooms = Get<string>(dict, "fixedRooms");
            roomPool = Get<string>(dict, "roomPool");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
        foreach (Maze config in Agent.Instance.Content.Gets<Maze>())
        {
            string fixedRoomsJson = ConvertFixedRooms(config.fixedRooms);
            
            string roomPoolJson = ConvertRoomPool(config.roomPool);

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                {"id", config.id },
                {"width", config.width },
                {"height", config.height },
                {"fillRate", config.fillRate },
                {"iterations", config.iterations },
                {"fixedRooms", fixedRoomsJson },
                {"roomPool", roomPoolJson },
            };
            datas.Add(data);
        }

            string path = $"{Utils.Paths.Library}/Config/Maze.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }

        private static Dictionary<string, int> ParseFixedRooms(string source)
        {
            var dict = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(source)) return dict;

            var lines = source.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length == 2 &&
                    !string.IsNullOrEmpty(parts[0]) &&
                    int.TryParse(parts[1], out int count) && 
                    count > 0)
                {
                    dict[parts[0].Trim()] = count;
                }
            }
            return dict;
        }

        private static Dictionary<string, float> ParseRoomPool(string source)
        {
            var dict = new Dictionary<string, float>();
            if (string.IsNullOrEmpty(source)) return dict;

            var lines = source.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('*');
                if (parts.Length == 2 &&
                    !string.IsNullOrEmpty(parts[0]) &&
                    float.TryParse(parts[1], out float weight))
                {
                    dict[parts[0].Trim()] = weight;
                }
            }
            return dict;
        }

        private static string ConvertFixedRooms(string source)
        {
            var dict = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(source))
            {
                var pairs = source.Split(',');
                foreach (var pair in pairs)
                {
                    var parts = pair.Split('×');
                    if (parts.Length == 2)
                    {
                        string cid = parts[0].Trim();
                        if (int.TryParse(parts[1], out int count))
                        {
                            var mapConfig = Agent.Instance.Content.Get<Map>(m => m.cid == cid);
                            if (mapConfig != null)
                            {
                                dict[mapConfig.id.ToString()] = count;
                            }
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(dict);
        }

        private static string ConvertRoomPool(string source)
        {
            var dict = new Dictionary<string, float>();
            if (!string.IsNullOrEmpty(source))
            {
                var pairs = source.Split(',');
                foreach (var pair in pairs)
                {
                    var parts = pair.Split('*');
                    if (parts.Length == 2)
                    {
                        string cid = parts[0].Trim();
                        if (float.TryParse(parts[1], out float weight))
                        {
                            var mapConfig = Agent.Instance.Content.Get<Map>(m => m.cid == cid);
                            if (mapConfig != null)
                            {
                                dict[mapConfig.id.ToString()] = weight;
                            }
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(dict);
        }
    }
} 