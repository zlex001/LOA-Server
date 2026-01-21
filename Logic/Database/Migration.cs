using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Logic.Database
{
    public static class Migration
    {
        private static readonly Dictionary<string, string> ItemCidMapping = new()
        {
            ["匕首10"] = "匕首",
            ["匕首8"] = "匕首",
            ["匕首1"] = "匕首",
            ["食材2"] = "稻谷",
            ["食材1"] = "稻谷",
            ["食材6"] = "稻谷",
            ["食材9"] = "稻谷",
            ["食材7"] = "稻谷",
            ["食材8"] = "稻谷",
            ["食材3"] = "稻谷",
            ["食材5"] = "稻谷",
            ["食材4"] = "稻谷",
            ["暗器10"] = "飞刀",
            ["食品10"] = "佛跳墙",
            ["铠甲10"] = "钢甲",
            ["铠甲9"] = "钢甲",
            ["铠甲1"] = "钢甲",
            ["棍10"] = "鬼头杖",
            ["食品9"] = "好逑汤",
            ["等价币·基金"] = "基金",
            ["鞭10"] = "九截鞭",
            ["重置·人物五维"] = "九转还魂丹",
            ["帽10"] = "纶巾",
            ["食品1"] = "馒头",
            ["帽1"] = "帽子",
            ["衣服1"] = "长衫",
            ["鞋1"] = "布鞋",
            ["剑1"] = "长剑",
            ["手镯1"] = "髮饰",
            ["武学经验"] = "《武穆遗书》",
            ["秀儿的信"] = "秀儿的信",
            ["床2"] = "木床",
            ["裙子10"] = "霓裳羽衣",
            ["项链9"] = "霓裳羽衣",
            ["项链10"] = "霓裳羽衣",
            ["手镯10"] = "霓裳羽衣",
            ["项链1"] = "霓裳羽衣",
            ["食材10"] = "青菜",
            ["布料1"] = "兽皮",
            ["布料3"] = "兽皮",
            ["布料4"] = "兽皮",
            ["布料5"] = "兽皮",
            ["布料8"] = "兽皮",
            ["布料6"] = "兽皮",
            ["布料9"] = "兽皮",
            ["布料10"] = "兽皮",
            ["布料7"] = "兽皮",
            ["布料2"] = "兽皮",
            ["矿石3"] = "铁矿石",
            ["矿石2"] = "铁矿石",
            ["矿石4"] = "铁矿石",
            ["矿石6"] = "铁矿石",
            ["矿石1"] = "铁矿石",
            ["矿石5"] = "铁矿石",
            ["游戏币·兑换"] = "统钞",
            ["头盔10"] = "头盔",
            ["头盔1"] = "头盔",
            ["戒指10"] = "无尘剑",
            ["戒指9"] = "无尘剑",
            ["剑10"] = "无尘剑",
            ["披风10"] = "武士披风",
            ["暗器1"] = "绣花针",
            ["刀10"] = "玄冥宝刀",
            ["腰带10"] = "腰带",
            ["矿石9"] = "玉石",
            ["矿石10"] = "玉石",
            ["矿石8"] = "玉石",
            ["矿石7"] = "玉石",
            ["发簪10"] = "簪子",
            ["裤子10"] = "长裤",
            ["衣服10"] = "长衫",
            ["鞋10"] = "长靴",
            ["靴10"] = "长靴",
            ["生活经验"] = "《齐民要术》",
            ["重置·人物名称"] = "名帖",
            ["武学经验·倍率次数"] = "玄冰烈火酒",
            ["重置·人物天赋"] = "洗髓丹",
            ["读书写字"] = "《读书写字》",
            ["生活经验·倍率次数"] = "《天工开物》",
            ["重置·人物外貌"] = "《易容术》",
            ["匕首7"] = "匕首",
            ["匕首9"] = "匕首",
            ["道家鞋"] = "布鞋",
            ["茶壶"] = "茶",
            ["带血的头巾"] = "带血的头巾",
            ["刀1"] = "钢刀",
            ["刀8"] = "钢刀",
            ["刀9"] = "钢刀",
            ["棍2"] = "钢棍",
            ["棍6"] = "钢棍",
            ["棍7"] = "钢棍",
            ["棍9"] = "钢棍",
            ["铠甲7"] = "钢甲",
            ["铠甲8"] = "钢甲",
            ["衣服-佛家"] = "袈裟",
            ["鞭8"] = "九截鞭",
            ["鞭9"] = "九截鞭",
            ["帽2"] = "纶巾",
            ["帽3"] = "纶巾",
            ["帽7"] = "纶巾",
            ["帽8"] = "纶巾",
            ["帽-佛家"] = "帽子",
            ["床0"] = "木床",
            ["床1"] = "木床",
            ["峨嵋派床1"] = "木床",
            ["华山派床1"] = "木床",
            ["少林寺床1"] = "木床",
            ["华山派木头人"] = "木头人",
            ["裙子1"] = "霓裳羽衣",
            ["裙子5"] = "霓裳羽衣",
            ["裙子7"] = "霓裳羽衣",
            ["裙子8"] = "霓裳羽衣",
            ["裙子9"] = "霓裳羽衣",
            ["手镯6"] = "霓裳羽衣",
            ["手镯8"] = "霓裳羽衣",
            ["项链8"] = "霓裳羽衣",
            ["披风1"] = "披风",
            ["披风6"] = "披风",
            ["披风7"] = "披风",
            ["披风8"] = "披风",
            ["披风9"] = "披风",
            ["蒲团"] = "披风",
            ["佛家鞋"] = "僧靴",
            ["布料5"] = "兽皮",
            ["佛偈0"] = "兽皮",
            ["头盔5"] = "头盔",
            ["头盔8"] = "头盔",
            ["头盔9"] = "头盔",
            ["门派·就职"] = "推荐信",
            ["戒指8"] = "无尘剑",
            ["暗器3"] = "绣花针",
            ["暗器9"] = "绣花针",
            ["暗器10"] = "绣花针",
            ["腰带1"] = "腰带",
            ["腰带6"] = "腰带",
            ["腰带8"] = "腰带",
            ["腰带9"] = "腰带",
            ["木柴"] = "玉石",
            ["发簪1"] = "簪子",
            ["发簪3"] = "簪子",
            ["发簪4"] = "簪子",
            ["发簪7"] = "簪子",
            ["发簪8"] = "簪子",
            ["发簪9"] = "簪子",
            ["鞭1"] = "长鞭",
            ["剑3"] = "长剑",
            ["剑4"] = "长剑",
            ["剑5"] = "长剑",
            ["剑7"] = "长剑",
            ["戒指1"] = "长剑",
            ["裤子6"] = "长裤",
            ["裤子8"] = "长裤",
            ["裤子9"] = "长裤",
            ["衣服2"] = "长衫",
            ["衣服3"] = "长衫",
            ["衣服4"] = "长衫",
            ["衣服5"] = "长衫",
            ["衣服8"] = "长衫",
            ["衣服9"] = "长衫",
            ["鞋2"] = "长靴",
            ["鞋3"] = "长靴",
            ["鞋7"] = "长靴",
            ["鞋8"] = "长靴",
            ["鞋9"] = "长靴",
            ["靴2"] = "长靴",
            ["靴6"] = "长靴",
            ["靴7"] = "长靴",
            ["靴8"] = "长靴",
            ["棍1"] = "钢棍",
            ["剑8"] = "长剑",
            ["靴1"] = "短靴"
        };

        private static readonly HashSet<string> EquipmentNames = new()
        {
            "短刀", "钢刀", "大刀", "双刀", "芙蓉双刀", "红缨刀", "戒刀", "柳月双刀", "苗刀",
            "鬼牙刀", "凤鸣刀", "玄冥宝刀", "巫月神刀", "短剑", "越女剑", "双剑", "长剑",
            "钢剑", "仙女剑", "玄铁剑", "青钢剑", "双龙剑", "龙泉剑", "金童剑", "玉女剑",
            "七星剑", "磐龙剑", "太极剑", "无尘剑", "匕首", "钢棍", "青蛇杖", "鬼头杖",
            "长鞭", "钢鞭", "九截鞭", "髮饰", "簪子", "头巾", "帽子", "纶巾", "道帽",
            "头盔", "披风", "斗篷", "武士披风", "钢甲", "宝甲", "长衫", "霓裳羽衣",
            "罗汉袍", "袈裟", "道袍", "战袍", "腰带", "短裤", "长裤", "钢靴", "僧靴",
            "长靴", "短靴", "绣花鞋", "布鞋"
        };


        public static void Init()
        {
            var players = Database.Agent.Instance.Query(Logic.Config.MySQL.ConnectionString, "Player");
            int migratedCount = 0;

            foreach (var player in players)
            {
                bool modified = false;

                // 先转换装备格式，再处理物品迁移，最后转换技能格式
                if (ConvertPlayerEquipments(player)) modified = true;
                if (ConvertPlayerItems(player)) modified = true;
                if (ConvertPlayerSkills(player)) modified = true;

                if (modified)
                {
                    SavePlayer(player);
                    migratedCount++;
                }
            }

            MigrateMerchandise();
            DropItemsColumn();
            Utils.Debug.Log.Info("MIGRATION", $"数据迁移完成，共处理 {migratedCount} 个玩家账号");
        }

        /// <summary>
        /// 转换Player.Items：CID字典 → 背包装备
        /// </summary>
        private static bool ConvertPlayerItems(Dictionary<string, object> player)
        {
            var itemsJson = player.TryGetValue("Items", out var itemsObj) ? itemsObj?.ToString() : null;
            if (string.IsNullOrEmpty(itemsJson)) return false;

            // 解析CID字典：{"道家鞋":1, "剑1":2}
            var itemDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(itemsJson);
            var itemList = new List<Dictionary<string, object>>();

            foreach (var kvp in itemDict)
            {
                var cid = kvp.Key;
                var count = Convert.ToInt32(kvp.Value);
                itemList.Add(CreateItem(cid, count));
            }

            // 创建背包装备
            var bagItem = new Dictionary<string, object>
            {
                ["Cid"] = "行囊",
                ["Count"] = 1,
                ["Properties"] = new Dictionary<string, object>
                {
                    ["Items"] = itemList  // 直接存储物品列表为对象格式
                }
            };

            // 添加到Equipments，删除Items
            var equipmentsJson = player.TryGetValue("Equipments", out var equipmentsObj) ? equipmentsObj?.ToString() : "[]";
            var equipmentsList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(equipmentsJson) ?? new();
            equipmentsList.Add(bagItem);

            player["Equipments"] = JsonConvert.SerializeObject(equipmentsList);
            player.Remove("Items");

            Utils.Debug.Log.Info("MIGRATION", $"玩家 {player["Id"]} Items迁移完成，{itemDict.Count}个物品 → 背包");
            return true;
        }

        /// <summary>
        /// 转换Player.Equipments：CID数组 → Item实例列表
        /// </summary>
        private static bool ConvertPlayerEquipments(Dictionary<string, object> player)
        {
            var equipmentsJson = player.TryGetValue("Equipments", out var equipmentsObj) ? equipmentsObj?.ToString() : null;
            if (string.IsNullOrEmpty(equipmentsJson) || equipmentsJson == "[]") return false;

            // 转换旧的CID数组：["帽1", "剑1", "道家鞋"]
            var equipmentCids = JsonConvert.DeserializeObject<string[]>(equipmentsJson);
            var equipmentsList = new List<Dictionary<string, object>>();

            foreach (var cid in equipmentCids)
            {
                if (!string.IsNullOrEmpty(cid))
                {
                    equipmentsList.Add(CreateItem(cid, 1));
                }
            }

            player["Equipments"] = JsonConvert.SerializeObject(equipmentsList);
            Utils.Debug.Log.Info("MIGRATION", $"玩家 {player["Id"]} Equipments迁移完成，{equipmentCids.Length}个装备 → Item实例");
            return true;

        }

        /// <summary>
        /// 迁移Merchandise表到Player.merchandises
        /// </summary>
        private static void MigrateMerchandise()
        {
            var merchandises = Database.Agent.Instance.Query(Logic.Config.MySQL.ConnectionString, "Merchandise");
            var players = Database.Agent.Instance.Query(Logic.Config.MySQL.ConnectionString, "Player");

            var merchandiseBySupplier = merchandises
                .Where(m => m.TryGetValue("supplier", out var supplier) && !string.IsNullOrEmpty(supplier?.ToString()))
                .GroupBy(m => m["supplier"].ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            int migratedPlayerCount = 0;
            int totalMerchandiseCount = 0;

            foreach (var player in players)
            {
                var playerId = player["Id"].ToString();

                if (merchandiseBySupplier.TryGetValue(playerId, out var playerMerchandises))
                {
                    var merchandiseItems = new List<Dictionary<string, object>>();

                    foreach (var merchandise in playerMerchandises)
                    {
                        var itemCid = merchandise.TryGetValue("item", out var itemObj) ? itemObj?.ToString() : null;
                        var count = merchandise.TryGetValue("count", out var countObj) ? Convert.ToInt32(countObj) : 1;
                        var priceJson = merchandise.TryGetValue("price", out var priceObj) ? priceObj?.ToString() : "[]";

                        if (!string.IsNullOrEmpty(itemCid))
                        {
                            var item = CreateItem(itemCid, count);
                            merchandiseItems.Add(new Dictionary<string, object>
                            {
                                ["Item"] = JsonConvert.SerializeObject(item),
                                ["Price"] = priceJson,
                                ["ListTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            });
                            totalMerchandiseCount++;
                        }
                    }

                    if (merchandiseItems.Count > 0)
                    {
                        var merchandisesJson = JsonConvert.SerializeObject(merchandiseItems);
                        Database.Agent.Instance.ExecuteNonQuery(Logic.Config.MySQL.ConnectionString,
                            "UPDATE Player SET Merchandises = @merchandises WHERE Id = @id",
                            new Dictionary<string, object>
                            {
                                ["@merchandises"] = merchandisesJson,
                                ["@id"] = playerId
                            });
                        migratedPlayerCount++;
                    }
                }
            }

            Utils.Debug.Log.Info("MIGRATION", $"Merchandise迁移完成，{migratedPlayerCount}个玩家，{totalMerchandiseCount}件商品");
        }

        /// <summary>
        /// 创建Item实例
        /// </summary>
        private static Dictionary<string, object> CreateItem(string cid, int count)
        {
            var properties = new Dictionary<string, string>();

            // 特殊道具处理
            if (cid.Contains("师傅信物"))
            {
                var masterName = cid.Replace("信物", "").Replace("天龙寺", "大理段氏");
                properties["Master"] = masterName;
                cid = "信物";
            }
            else if (IsSkillCid(cid))
            {
                var skillCid = cid.Replace("天龙寺", "大理段氏");
                properties["Skill"] = skillCid;
                cid = "秘籍";
            }
            else
            {
                // 普通道具CID映射
                cid = ItemCidMapping.GetValueOrDefault(cid, cid);

                // 装备添加耐久度
                if (EquipmentNames.Contains(cid))
                {
                    properties["Durability"] = "500/500";
                }
            }

            return new Dictionary<string, object>
            {
                ["Cid"] = cid,
                ["Count"] = count,
                ["Properties"] = properties  // 直接返回对象格式，避免过度序列化
            };
        }

        /// <summary>
        /// 判断是否为技能CID
        /// </summary>
        private static bool IsSkillCid(string cid)
        {
            try
            {
                var mappedCid = cid.Replace("天龙寺", "大理段氏");
                var skillConfigs = Logic.Design.Agent.Instance.Content.Gets<Logic.Design.Skill>();
                return skillConfigs.Any(s => s.cid == mappedCid);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 转换Player.Skills：将字段名从"Id"更改为"Cid"
        /// </summary>
        private static bool ConvertPlayerSkills(Dictionary<string, object> player)
        {
            var skillsJson = player.TryGetValue("Skills", out var skillsObj) ? skillsObj?.ToString() : null;
            if (string.IsNullOrEmpty(skillsJson)) return false;

            try
            {
                // 解析现有技能数据
                var skillsList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(skillsJson);
                if (skillsList == null || skillsList.Count == 0) return false;

                bool hasChanges = false;

                // 转换每个技能的字段名
                foreach (var skill in skillsList)
                {
                    // 检查是否有旧的"Id"字段
                    if (skill.ContainsKey("Id") && !skill.ContainsKey("Cid"))
                    {
                        // 将"Id"字段重命名为"Cid"
                        skill["Cid"] = skill["Id"];
                        skill.Remove("Id");
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    // 更新Skills字段
                    player["Skills"] = JsonConvert.SerializeObject(skillsList);
                    Utils.Debug.Log.Info("MIGRATION", $"Skills字段已更新：{skillsList.Count} 个技能的字段名已从 Id 转换为 Cid");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("MIGRATION", $"转换Skills失败：{ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 保存玩家数据
        /// </summary>
        private static void SavePlayer(Dictionary<string, object> player)
        {
            var playerId = player["Id"].ToString();
            var skillsJson = player.TryGetValue("Skills", out var skillsObj) ? skillsObj?.ToString() : "[]";
            var equipmentsJson = player["Equipments"].ToString();
            var warehousesJson = player.TryGetValue("Warehouses", out var warehousesObj) ? warehousesObj?.ToString() : "[]";

            var parameters = new Dictionary<string, object>
            {
                ["@skills"] = skillsJson,
                ["@equipments"] = equipmentsJson,
                ["@warehouses"] = warehousesJson,
                ["@id"] = playerId
            };

            string sql;
            if (player.ContainsKey("Items"))
            {
                sql = "UPDATE Player SET Skills = @skills, Items = @items, Equipments = @equipments, Warehouses = @warehouses WHERE Id = @id";
                parameters["@items"] = player["Items"].ToString();
            }
            else
            {
                sql = "UPDATE Player SET Skills = @skills, Equipments = @equipments, Warehouses = @warehouses WHERE Id = @id";
            }

            Database.Agent.Instance.ExecuteNonQuery(Logic.Config.MySQL.ConnectionString, sql, parameters);
        }

        /// <summary>
        /// 删除Player表的Items列
        /// </summary>
        private static void DropItemsColumn()
        {
            try
            {
                var dropColumnSql = "ALTER TABLE Player DROP COLUMN Items";
                Database.Agent.Instance.ExecuteNonQuery(Logic.Config.MySQL.ConnectionString, dropColumnSql, new Dictionary<string, object>());
                Utils.Debug.Log.Info("MIGRATION", "Player.Items列已删除");
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                if (ex.Number == 1091) // Column doesn't exist
                {
                    Utils.Debug.Log.Info("MIGRATION", "Player.Items列不存在，跳过删除");
                }
                else
                {
                    Utils.Debug.Log.Error("MIGRATION", $"删除Player.Items列失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("MIGRATION", $"删除Player.Items列失败: {ex.Message}");
            }
        }
    }
} 