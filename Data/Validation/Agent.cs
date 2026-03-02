using System;
using System.Collections.Generic;
using System.Linq;
using Utils;
using Basic;

namespace Data.Validation
{
    public enum Event
    {
        Completed,
    }

    public class Agent : Basic.Ability
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }
        public override void Init(params object[] args)
        {
            monitor.Register(Validation.Event.Completed, OnCompleted);
        }
        private void OnCompleted(params object[] args)
        {
        }
        
        public void RunDesignValidation()
        {
            ValidateDesignData();
            
            var errors = Content.Gets<Error>();
            if (errors.Any())
            {
                Utils.Debug.Log.Error("VALIDATION", "========================================");
                Utils.Debug.Log.Error("VALIDATION", $"FOUND {errors.Count} VALIDATION ERRORS:");
                Utils.Debug.Log.Error("VALIDATION", "========================================");
                
                int index = 1;
                foreach (var error in errors)
                {
                    Utils.Debug.Log.Error("VALIDATION", $"");
                    Utils.Debug.Log.Error("VALIDATION", $"ERROR #{index}:");
                    Utils.Debug.Log.Error("VALIDATION", $"  Error Type: {error.Category}");
                    Utils.Debug.Log.Error("VALIDATION", $"  Module: {error.Module}");
                    Utils.Debug.Log.Error("VALIDATION", $"  Config Type: {error.ConfigType}");
                    Utils.Debug.Log.Error("VALIDATION", $"  Problem Value: [{error.Value}]");
                    if (!string.IsNullOrEmpty(error.Details))
                    {
                        Utils.Debug.Log.Error("VALIDATION", $"  Details: {error.Details}");
                    }
                    index++;
                }
                
                Utils.Debug.Log.Error("VALIDATION", "");
                Utils.Debug.Log.Error("VALIDATION", "========================================");
                
                var missingMultilingualKeys = errors
                    .Where(e => e.Category == "Multilingual key not found" && !string.IsNullOrEmpty(e.Details))
                    .Select(e =>
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(e.Details, @"multilingual key \[(.+?)\] which");
                        return match.Success ? match.Groups[1].Value : null;
                    })
                    .Where(key => key != null)
                    .Distinct()
                    .ToList();

                if (missingMultilingualKeys.Any())
                {
                    Utils.Debug.Log.Error("VALIDATION", "");
                    Utils.Debug.Log.Error("VALIDATION", "Missing Multilingual Keys (copy to CSV):");
                    Utils.Debug.Log.Error("VALIDATION", "========================================");
                    
                    var groupedKeys = missingMultilingualKeys
                        .GroupBy(key =>
                        {
                            if (key.Contains("[名称]")) return 1;
                            if (key.Contains("[描述]")) return 2;
                            return 3;
                        })
                        .OrderBy(g => g.Key);
                    
                    foreach (var group in groupedKeys)
                    {
                        foreach (var key in group.OrderBy(k => k))
                        {
                            System.Console.WriteLine(key);
                        }
                        System.Console.WriteLine();
                    }
                    
                    Utils.Debug.Log.Error("VALIDATION", "========================================");
                }
                
                throw new Exception($"Design data validation failed with {errors.Count} errors. Check logs above.");
            }
        }
        private void ValidateBinaryData()
        {
            var validation = global::Data.Validation.Agent.Instance;

            ValidateSceneData();

            validation.monitor.Fire(global::Data.Validation.Event.Completed, "Binary");
        }
        private void ValidateSceneData()
        {
            var validation = global::Data.Validation.Agent.Instance;
            string binPath = $"{Utils.Paths.Config}/Shortest.bin";
            if (!System.IO.File.Exists(binPath))
            {
                validation.Create<global::Data.Validation.Error>("�ļ�������", "Binary", "Scene", "Shortest.bin");
                return;
            }

            var fileInfo = new System.IO.FileInfo(binPath);
            if (fileInfo.Length == 0)
            {
                validation.Create<global::Data.Validation.Error>("�ļ�Ϊ��", "Binary", "Scene", "Shortest.bin");
                return;
            }

            try
            {
                var scenes = Utils.Binary.Deserialize<List<global::Data.Database.Scene>>(binPath, Utils.SerializeFormat.Binary);

                if (scenes == null)
                {
                    validation.Create<global::Data.Validation.Error>("�����л�ʧ��", "Binary", "Scene", "Shortest.bin");
                    return;
                }

                if (scenes.Count == 0)
                {
                    validation.Create<global::Data.Validation.Error>("����Ϊ��", "Binary", "Scene", "Shortest.bin");
                    return;
                }


                // ��֤ÿ��Scene������������
                foreach (var scene in scenes)
                {
                    if (scene == null)
                    {
                        validation.Create<global::Data.Validation.Error>("����Ϊnull", "Binary", "Scene", "δ֪Scene");
                        continue;
                    }

           

                    if (scene.maps == null)
                    {
                        validation.Create<global::Data.Validation.Error>("maps����Ϊnull", "Binary", "Scene", scene.id.ToString());
                    }
                    else if (scene.maps.Count == 0)
                    {
                        validation.Create<global::Data.Validation.Error>("maps����Ϊ��", "Binary", "Scene", scene.id.ToString());
                    }

                    if (scene.shortest == null)
                    {
                        validation.Create<global::Data.Validation.Error>("shortest����Ϊnull", "Binary", "Scene", scene.id.ToString());
                    }
                }
            }
            catch (System.Exception ex)
            {
                validation.Create<global::Data.Validation.Error>("�����л��쳣", "Binary", "Scene", ex.Message);
            }
        }
        /// <summary>
        /// ��֤Design����
        /// </summary>
        private void ValidateDesignData()
        {
            var validation = global::Data.Validation.Agent.Instance;

            // ִ�о�����֤
            ValidateItems();
            ValidateLifes();  // 添加Life验证
            ValidateScenes();
            ValidateMaps();
            ValidateIntegrity();

            // ֪ͨ��֤��ܣ�Design��֤���
            validation.monitor.Fire(global::Data.Validation.Event.Completed, "Design");
        }

        /// <summary>
        /// 验证道具数据
        /// </summary>
        private void ValidateItems()
        {
            var validation = global::Data.Validation.Agent.Instance;
            var items = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Item>();

            foreach (var item in items)
            {
                // 验证枚举值
                if (!string.IsNullOrEmpty(item.type))
                {
                    var validValues = System.Enum.GetNames(typeof(global::Data.Item.Types));
                    if (!validValues.Contains(item.type))
                    {
                        validation.Create<global::Data.Validation.Error>("Invalid enum value", "Design", "Item", item.cid);
                    }
                }
            }
        }

        /// <summary>
        /// 验证生物数据
        /// </summary>
        private void ValidateLifes()
        {
            var validation = global::Data.Validation.Agent.Instance;
            var lifes = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Life>();
            
            // 有效的Part名称（中文）
            var validPartNames = new HashSet<string> { "头", "胸", "手", "背", "腰", "腿", "脚", "爪", "尾", "翼" };

            foreach (var life in lifes)
            {
                // 验证parts字段必须存在
                if (string.IsNullOrEmpty(life.parts))
                {
                    validation.Create<global::Data.Validation.Error>("Parts field is required", "Design", "Life", life.cid, 
                        $"Life [{life.cid}] has empty or null parts field. Please configure body parts in 生物.md");
                    continue;
                }

                // 验证parts字段的格式和内容
                var parts = life.parts.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                
                if (!parts.Any())
                {
                    validation.Create<global::Data.Validation.Error>("Parts field is empty", "Design", "Life", life.cid,
                        $"Life [{life.cid}] has empty parts configuration. Please add at least one body part.");
                    continue;
                }

                // 验证每个part名称是否有效
                foreach (var part in parts)
                {
                    if (!validPartNames.Contains(part))
                    {
                        validation.Create<global::Data.Validation.Error>("Invalid part name", "Design", "Life", life.cid,
                            $"Life [{life.cid}] has invalid part name [{part}]. Valid names: {string.Join(", ", validPartNames)}");
                    }
                }

                // 验证必须有头和胸（核心部位）
                var partList = parts.ToList();
                if (!partList.Contains("头"))
                {
                    validation.Create<global::Data.Validation.Error>("Missing required part", "Design", "Life", life.cid,
                        $"Life [{life.cid}] is missing required part [头]. All lives must have a head.");
                }
                if (!partList.Contains("胸"))
                {
                    validation.Create<global::Data.Validation.Error>("Missing required part", "Design", "Life", life.cid,
                        $"Life [{life.cid}] is missing required part [胸]. All lives must have a chest.");
                }
            }
        }



        /// <summary>
        /// 验证场景数据
        /// </summary>
        private void ValidateScenes()
        {
            var validation = global::Data.Validation.Agent.Instance;
            var scenes = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Scene>();
            var multilingualCids = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Multilingual>().Select(ml => ml.cid).ToHashSet();

            foreach (var scene in scenes)
            {
                if (!string.IsNullOrEmpty(scene.name) && !multilingualCids.Contains(scene.name))
                {
                    validation.Create<global::Data.Validation.Error>("Multilingual key not found", "Design", "Scene", scene.cid, 
                        $"Scene [{scene.cid}] uses multilingual key [{scene.name}] which is not defined in Multilingual config");
                }
            }
        }

        /// <summary>
        /// ��֤��ͼ����
        /// </summary>
        private void ValidateMaps()
        {
            var validation = global::Data.Validation.Agent.Instance;

            // ��֤�����ͼ�ļ�������
            string worldPath = $"{Utils.Paths.DesignData}/地图坐标.csv";
            if (!System.IO.File.Exists(worldPath))
            {
                validation.Create<global::Data.Validation.Error>("File not found", "Design", "Map", "地图坐标.csv");
                return;
            }

            var cells = Utils.Csv.LoadAsCells(worldPath);
            var maps = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Map>();
            var scenes = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Scene>();
            var lives = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Life>();
            var items = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Item>();

            var mapCids = maps.Select(m => m.cid).ToHashSet();
            var sceneCids = scenes.Select(s => s.cid).ToHashSet();
            var lifeCids = lives.Select(l => l.cid).ToHashSet();
            var itemCids = items.Select(i => i.cid).ToHashSet();
            var multilingualCids = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Multilingual>().Select(ml => ml.cid).ToHashSet();
            var questCids = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Quest>().Select(p => p.cid).ToHashSet();
            var usedMapCids = new HashSet<string>();

            // ��֤�����ͼ����
            for (int y = 0; y < cells.Count; y++)
            {
                for (int x = 0; x < cells[y].Count; x++)
                {
                    var cellValue = System.Convert.ToString(cells[y][x]);
                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        usedMapCids.Add(cellValue);

                        // ��֤��ͼCID������
                        if (!mapCids.Contains(cellValue))
                        {
                            validation.Create<global::Data.Validation.Error>("Map CID not found in config", "Design", "Map", cellValue, 
                                $"地图坐标.csv references map [{cellValue}] at position (row={y+1}, col={x+1}), but this map is not defined in 地图.csv");
                        }
                        else
                        {
                            // ��֤��������
                            var sceneCid = cellValue.Split('-')[0];
                            if (!sceneCids.Contains(sceneCid))
                            {
                                validation.Create<global::Data.Validation.Error>("Scene CID not found in config", "Design", "Scene", sceneCid,
                                    $"Map [{cellValue}] requires scene [{sceneCid}], but this scene is not defined in 场景.csv");
                            }
                        }
                    }
                }
            }

            // ��֤δʹ�õĵ�ͼ
            foreach (var map in maps)
            {
                if (!usedMapCids.Contains(map.cid))
                {
                    validation.Create<global::Data.Validation.Error>("Unused map config", "Design", "Map", map.cid);
                }

                if (string.IsNullOrEmpty(map.name))
                {
                    validation.Create<global::Data.Validation.Error>("Map name is empty", "Design", "Map", map.cid,
                        $"Map [{map.cid}] has empty or null name field. Please set a valid name in 地图.csv");
                }

                if (string.IsNullOrEmpty(map.type))
                {
                    validation.Create<global::Data.Validation.Error>("Map type is empty", "Design", "Map", map.cid,
                        $"Map [{map.cid}] has empty or null type field. Please set a valid type in 地图.csv");
                }

                // ��֤��ͼ���ƶ����Կ���
                if (!string.IsNullOrEmpty(map.name) && !multilingualCids.Contains(map.name))
                {
                    validation.Create<global::Data.Validation.Error>("Multilingual key not found", "Design", "Map", map.cid, 
                        $"Map [{map.cid}] uses multilingual key [{map.name}] which is not defined in Multilingual config");
                }

                // ��֤��ͼ�¼�����
                if (map.quests != null && map.quests.Length > 0)
                {
                    foreach (var questCid in map.quests)
                    {
                        if (!string.IsNullOrEmpty(questCid) && !questCids.Contains(questCid))
                        {
                            validation.Create<global::Data.Validation.Error>("Quest CID not found", "Design", "Map", map.cid, 
                                $"Map [{map.cid}] references quest [{questCid}] which is not defined in Quest config");
                        }
                    }
                }

                // ��֤��ͼ��ɫ����
                if (map.characters != null)
                {
                    foreach (string characterEntry in map.characters)
                    {
                        var processedCids = ProcessCharacterEntry(characterEntry);
                        foreach (var (cid, hasLevel) in processedCids)
                        {
                            if (itemCids.Contains(cid)) continue;

                            if (lifeCids.Contains(cid) && !hasLevel)
                            {
                                validation.Create<global::Data.Validation.Error>("Missing level range", "Design", "Life", cid);
                            }
                            else if (!lifeCids.Contains(cid))
                            {
                                validation.Create<global::Data.Validation.Error>("Character CID not found", "Design", "Character", cid);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ��֤����������
        /// </summary>
        private void ValidateIntegrity()
        {
            var validation = global::Data.Validation.Agent.Instance;
            var items = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Item>();
            var maps = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Map>();

            if (!items.Any())
            {
                validation.Create<global::Data.Validation.Error>("Data is empty or load failed", "Design", "Item", "All Item Data");
            }

            if (!maps.Any())
            {
                validation.Create<global::Data.Validation.Error>("Data is empty or load failed", "Design", "Map", "All Map Data");
            }
        }

        /// <summary>
        /// ������ͼ��ɫ��Ŀ
        /// </summary>
        private List<(string cid, bool hasLevel)> ProcessCharacterEntry(string characterEntry)
        {
            var results = new List<(string, bool)>();
            string[] entries = characterEntry.Contains(',') && !characterEntry.Contains('(')
                ? characterEntry.Split(',')
                : new[] { characterEntry };

            foreach (string entry in entries)
            {
                string entryTrimmed = entry.Trim();
                if (string.IsNullOrEmpty(entryTrimmed)) continue;

                string entryWithoutQuantity = entryTrimmed;
                
                var quantityMatch = System.Text.RegularExpressions.Regex.Match(
                    entryTrimmed,
                    @"^(.+?)[×x\*·](\d+)$"
                );
                
                if (quantityMatch.Success)
                {
                    entryWithoutQuantity = quantityMatch.Groups[1].Value;
                }

                string actualCid;
                bool hasLevel = false;

                if (entryWithoutQuantity.Contains(':'))
                {
                    var parts = entryWithoutQuantity.Split(':');
                    actualCid = parts[0];
                    hasLevel = parts.Length > 1;
                }
                else
                {
                    actualCid = entryWithoutQuantity;
                }

                results.Add((actualCid, hasLevel));
            }

            return results;
        }
    }
}