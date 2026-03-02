using System;
using System.Collections.Generic;
using Utils;

namespace Data.Database
{
    public partial class Agent : Basic.MySql
    {
        public delegate void FixDelegate();
        private Dictionary<string, FixDelegate> fix = new Dictionary<string, FixDelegate>();
        private Dictionary<string, List<string>> FixPlayer { get; set; } = new Dictionary<string, List<string>>();
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }
        
        public override void Init(params object[] args)
        {
            EnsureDailyAnalyticsTable();
            EnsurePlayerHistoryTable();
            
            Load<Server>(Config.MySQL.ConnectionString);
            Load<Card>(Config.MySQL.ConnectionString);
            Load<Device>(Config.MySQL.ConnectionString);
            Load<Player>(Config.MySQL.ConnectionString);
            
            // 区服隔离：如果指定了区服，只保留本区服的玩家
            if (!string.IsNullOrEmpty(global::Data.Agent.Instance.ServerId))
            {
                var serverId = global::Data.Agent.Instance.ServerId;
                var allPlayers = Content.Gets<Player>().ToList();
                var invalidPlayers = allPlayers.Where(p => p.GetText("ServerId") != serverId).ToList();
                
                // 移除其他区服的玩家
                foreach (var player in invalidPlayers)
                {
                    Remove(player);
                }
                
                var validCount = allPlayers.Count - invalidPlayers.Count;
            }
            
            if (global::Data.Agent.Instance.IsDevelopment)
            {
                ValidateDatabaseData();
            }
        }
        
        private void EnsureDailyAnalyticsTable()
        {
            try
            {
                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS DailyAnalytics (
                        Date DATE PRIMARY KEY,
                        Weekday VARCHAR(20),
                        ActivePlayers INT DEFAULT 0,
                        NewDevices INT DEFAULT 0,
                        NewDevicePlayers INT DEFAULT 0,
                        NewValidDevicePlayers INT DEFAULT 0,
                        NewPlayers INT DEFAULT 0,
                        NewValidPlayers INT DEFAULT 0,
                        RetentionRate DOUBLE DEFAULT 0,
                        WinBackRate DOUBLE DEFAULT 0,
                        ConversionRate DOUBLE DEFAULT 0,
                        ARPU DOUBLE DEFAULT 0,
                        ARPPU DOUBLE DEFAULT 0,
                        AverageUserLifetime DOUBLE DEFAULT 0,
                        LTV DOUBLE DEFAULT 0,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        INDEX idx_date (Date)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                ";
                
                ExecuteNonQuery(Config.MySQL.ConnectionString, createTableSql, new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DATABASE", $"Failed to ensure DailyAnalytics table: {ex.Message}");
            }
        }
        
        private void EnsurePlayerHistoryTable()
        {
            try
            {
                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS PlayerHistory (
                        Id BIGINT AUTO_INCREMENT PRIMARY KEY,
                        PlayerId VARCHAR(50) NOT NULL,
                        RecordTime DATETIME NOT NULL,
                        Level INT DEFAULT 0,
                        Exp BIGINT DEFAULT 0,
                        Hp INT DEFAULT 0,
                        MaxHp INT DEFAULT 0,
                        Mp INT DEFAULT 0,
                        MaxMp INT DEFAULT 0,
                        Gem INT DEFAULT 0,
                        OpvpScore INT DEFAULT 0,
                        MapId INT DEFAULT 0,
                        INDEX idx_player_time (PlayerId, RecordTime)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                ";
                
                ExecuteNonQuery(Config.MySQL.ConnectionString, createTableSql, new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DATABASE", $"Failed to ensure PlayerHistory table: {ex.Message}");
            }
        }
        private void OnTurnOff(params object[] args)
        {
            Save<Device>(global::Data.Config.MySQL.ConnectionString);
            Save<Player>(global::Data.Config.MySQL.ConnectionString);
        }
        public void Delete(Basic.Data data)
        {
            Delete(global::Data.Config.MySQL.ConnectionString, data);
        }

        public void ExecuteNonQuery(string connectionString, string query, Dictionary<string, object> parameters)
        {
            using var connection = Connection(connectionString);
            connection.Open();
            using var command = new MySql.Data.MySqlClient.MySqlCommand(query, connection);
            
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }
            
            command.ExecuteNonQuery();
        }
        
        public void SaveDailyAnalytics(DailyAnalytics analytics)
        {
            try
            {
                var upsertSql = @"
                    INSERT INTO DailyAnalytics 
                    (Date, Weekday, ActivePlayers, NewDevices, NewDevicePlayers, NewValidDevicePlayers, 
                     NewPlayers, NewValidPlayers, RetentionRate, WinBackRate, ConversionRate, 
                     ARPU, ARPPU, AverageUserLifetime, LTV)
                    VALUES 
                    (@Date, @Weekday, @ActivePlayers, @NewDevices, @NewDevicePlayers, @NewValidDevicePlayers,
                     @NewPlayers, @NewValidPlayers, @RetentionRate, @WinBackRate, @ConversionRate,
                     @ARPU, @ARPPU, @AverageUserLifetime, @LTV)
                    ON DUPLICATE KEY UPDATE
                    Weekday = @Weekday,
                    ActivePlayers = @ActivePlayers,
                    NewDevices = @NewDevices,
                    NewDevicePlayers = @NewDevicePlayers,
                    NewValidDevicePlayers = @NewValidDevicePlayers,
                    NewPlayers = @NewPlayers,
                    NewValidPlayers = @NewValidPlayers,
                    RetentionRate = @RetentionRate,
                    WinBackRate = @WinBackRate,
                    ConversionRate = @ConversionRate,
                    ARPU = @ARPU,
                    ARPPU = @ARPPU,
                    AverageUserLifetime = @AverageUserLifetime,
                    LTV = @LTV,
                    CreatedAt = CURRENT_TIMESTAMP
                ";
                
                var parameters = new Dictionary<string, object>
                {
                    ["@Date"] = analytics.Date.ToString("yyyy-MM-dd"),
                    ["@Weekday"] = analytics.Weekday,
                    ["@ActivePlayers"] = analytics.ActivePlayers,
                    ["@NewDevices"] = analytics.NewDevices,
                    ["@NewDevicePlayers"] = analytics.NewDevicePlayers,
                    ["@NewValidDevicePlayers"] = analytics.NewValidDevicePlayers,
                    ["@NewPlayers"] = analytics.NewPlayers,
                    ["@NewValidPlayers"] = analytics.NewValidPlayers,
                    ["@RetentionRate"] = analytics.RetentionRate,
                    ["@WinBackRate"] = analytics.WinBackRate,
                    ["@ConversionRate"] = analytics.ConversionRate,
                    ["@ARPU"] = analytics.ARPU,
                    ["@ARPPU"] = analytics.ARPPU,
                    ["@AverageUserLifetime"] = analytics.AverageUserLifetime,
                    ["@LTV"] = analytics.LTV
                };
                
                ExecuteNonQuery(Config.MySQL.ConnectionString, upsertSql, parameters);
                Utils.Debug.Log.Info("DATABASE", $"Daily analytics saved: {analytics.Date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DATABASE", $"Failed to save daily analytics for {analytics.Date:yyyy-MM-dd}: {ex.Message}");
                throw;
            }
        }
        
        public List<DailyAnalytics> QueryDailyAnalytics(DateTime startDate, DateTime endDate)
        {
            var result = new List<DailyAnalytics>();
            
            try
            {
                using (var connection = Connection(Config.MySQL.ConnectionString))
                {
                    connection.Open();
                    
                    var querySql = @"
                        SELECT * FROM DailyAnalytics 
                        WHERE Date BETWEEN @StartDate AND @EndDate 
                        ORDER BY Date ASC
                    ";
                    
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(querySql, connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var dict = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    dict.Add(reader.GetName(i), reader.GetValue(i));
                                }
                                
                                var analytics = new DailyAnalytics();
                                analytics.Init(dict);
                                result.Add(analytics);
                            }
                        }
                    }
                }
                
                Utils.Debug.Log.Info("DATABASE", $"Queried {result.Count} daily analytics records from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DATABASE", $"Failed to query daily analytics: {ex.Message}");
            }
            
            return result;
        }

        public Server GetServerById(string serverId)
        {
            return Content.Get<Server>(s => s.Id == serverId);
        }

        public List<Server> GetAllServers()
        {
            return Content.Gets<Server>();
        }
        
        public void SavePlayerHistory(string playerId, int level, long exp, int hp, int maxHp, int mp, int maxMp, int gem, int opvpScore, int mapId)
        {
            try
            {
                var insertSql = @"
                    INSERT INTO PlayerHistory 
                    (PlayerId, RecordTime, Level, Exp, Hp, MaxHp, Mp, MaxMp, Gem, OpvpScore, MapId)
                    VALUES 
                    (@PlayerId, @RecordTime, @Level, @Exp, @Hp, @MaxHp, @Mp, @MaxMp, @Gem, @OpvpScore, @MapId)
                ";
                
                var parameters = new Dictionary<string, object>
                {
                    ["@PlayerId"] = playerId,
                    ["@RecordTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["@Level"] = level,
                    ["@Exp"] = exp,
                    ["@Hp"] = hp,
                    ["@MaxHp"] = maxHp,
                    ["@Mp"] = mp,
                    ["@MaxMp"] = maxMp,
                    ["@Gem"] = gem,
                    ["@OpvpScore"] = opvpScore,
                    ["@MapId"] = mapId
                };
                
                ExecuteNonQuery(Config.MySQL.ConnectionString, insertSql, parameters);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DATABASE", $"Failed to save player history for {playerId}: {ex.Message}");
            }
        }
        
        public List<Dictionary<string, object>> QueryPlayerHistory(string playerId, DateTime startDate, DateTime endDate)
        {
            var result = new List<Dictionary<string, object>>();
            
            try
            {
                using (var connection = Connection(Config.MySQL.ConnectionString))
                {
                    connection.Open();
                    
                    var querySql = @"
                        SELECT PlayerId, RecordTime, Level, Exp, Hp, MaxHp, Mp, MaxMp, Gem, OpvpScore, MapId
                        FROM PlayerHistory 
                        WHERE PlayerId = @PlayerId 
                          AND RecordTime >= @StartDate 
                          AND RecordTime <= @EndDate 
                        ORDER BY RecordTime ASC
                    ";
                    
                    using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(querySql, connection))
                    {
                        cmd.Parameters.AddWithValue("@PlayerId", playerId);
                        cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new Dictionary<string, object>
                                {
                                    ["playerId"] = reader.GetString("PlayerId"),
                                    ["recordTime"] = reader.GetDateTime("RecordTime").ToString("yyyy-MM-dd HH:mm:ss"),
                                    ["level"] = reader.GetInt32("Level"),
                                    ["exp"] = reader.GetInt64("Exp"),
                                    ["hp"] = reader.GetInt32("Hp"),
                                    ["maxHp"] = reader.GetInt32("MaxHp"),
                                    ["mp"] = reader.GetInt32("Mp"),
                                    ["maxMp"] = reader.GetInt32("MaxMp"),
                                    ["gem"] = reader.GetInt32("Gem"),
                                    ["opvpScore"] = reader.GetInt32("OpvpScore"),
                                    ["mapId"] = reader.GetInt32("MapId")
                                };
                                result.Add(record);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DATABASE", $"Failed to query player history for {playerId}: {ex.Message}");
            }
            
            return result;
        }

        #region 验证方法

        /// <summary>
        /// 验证Database数据
        /// </summary>
        private void ValidateDatabaseData()
        {
            var validation = global::Data.Validation.Agent.Instance;

            // 执行具体验证
            ValidatePlayers();

            // 通知验证框架：Database验证完成
            validation.monitor.Fire(global::Data.Validation.Event.Completed, "Database");
        }

        /// <summary>
        /// 验证数据库玩家数据
        /// </summary>
        private void ValidatePlayers()
        {
            var validation = global::Data.Validation.Agent.Instance;
            var players = Content.Gets<global::Data.Database.Player>();
            var validSkillCids = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Skill>()
                .Where(config => !string.IsNullOrEmpty(config.cid))
                .Select(config => config.cid)
                .ToHashSet();
            var validItemCids = global::Data.Design.Agent.Instance.Content.Gets<global::Data.Design.Item>()
                .Where(config => !string.IsNullOrEmpty(config.cid))
                .Select(config => config.cid)
                .ToHashSet();

            foreach (var player in players)
            {
                if (string.IsNullOrEmpty(player.Id))
                {
                    validation.Create<global::Data.Validation.Error>("ID为空", "Database", "Player", player.Id?.ToString() ?? "null");
                }
            }

            // 验证技能CID和道具CID
            var playerRawData = Query(global::Data.Config.MySQL.ConnectionString, "Player");
            var usedSkillCids = new List<(string cid, string playerId, string field)>();
            var usedItemCids = new List<(string cid, string playerId, string field)>();

            foreach (var playerDict in playerRawData)
            {
                var playerId = playerDict.Get<string>("Id");
                
                // 验证技能CID
                var skillsJson = playerDict.Get<string>("Skills");
                if (!string.IsNullOrEmpty(skillsJson))
                {
                    var skillDicts = Utils.Json.Deserialize<List<Dictionary<string, object>>>(skillsJson);
                    if (skillDicts != null)
                    {
                        var cids = skillDicts.Where(s => s.ContainsKey("Id"))
                            .Select(s => s["Id"]?.ToString())
                            .Where(id => !string.IsNullOrEmpty(id));

                        foreach (var cid in cids)
                        {
                            usedSkillCids.Add((cid, playerId, "Skills"));
                        }
                    }
                }

                // 验证道具CID
                CollectItemCidsFromPlayerWithDetails(playerDict, usedItemCids, playerId);
            }

            // 验证商品CID
            try
            {
                var merchandiseRawData = Query(global::Data.Config.MySQL.ConnectionString, "Merchandise");
                foreach (var merchandiseDict in merchandiseRawData)
                {
                    var cid = merchandiseDict.Get<string>("Cid");
                    if (!string.IsNullOrEmpty(cid))
                    {
                        var merchantId = merchandiseDict.Get<string>("Id");
                        usedItemCids.Add((cid, $"Merchandise[{merchantId}]", "Cid"));
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DATABASE", $"[Merchandise CID Validation Failed]");
                Utils.Debug.Log.Error("DATABASE", $"Exception: {ex.Message}");
                Utils.Debug.Log.Error("DATABASE", $"StackTrace: {ex.StackTrace}");
            }

            // 检查未找到的技能CID
            foreach (var (cid, playerId, field) in usedSkillCids)
            {
                if (!validSkillCids.Contains(cid) && !IsAutoHandleCid(cid))
                {
                    var details = $"玩家[{playerId}]的{field}字段";
                    validation.Create<global::Data.Validation.Error>("技能CID不存在", "Database", "Skill", cid, details);
                }
            }

            // 检查未找到的道具CID
            foreach (var (cid, playerId, field) in usedItemCids)
            {
                if (!validItemCids.Contains(cid) && !IsAutoHandleCid(cid))
                {
                    var details = $"玩家[{playerId}]的{field}字段";
                    validation.Create<global::Data.Validation.Error>("道具CID不存在", "Database", "Item", cid, details);
                }
            }
        }

        /// <summary>
        /// 收集玩家道具CID（带详细信息）
        /// </summary>
        private void CollectItemCidsFromPlayerWithDetails(Dictionary<string, object> playerDict, List<(string cid, string playerId, string field)> usedCids, string playerId)
        {
            // 背包道具
            var itemsJson = playerDict.Get<string>("Items");
            CollectCidsFromJsonWithDetails(itemsJson, usedCids, playerId, "Items");

            // 装备道具
            var equipmentsJson = playerDict.Get<string>("Equipments");
            CollectCidsFromJsonWithDetails(equipmentsJson, usedCids, playerId, "Equipments");

            // 仓库道具
            var warehousesJson = playerDict.Get<string>("Warehouses");
            CollectCidsFromJsonWithDetails(warehousesJson, usedCids, playerId, "Warehouses");
        }

        /// <summary>
        /// 从JSON中收集CID
        /// </summary>
        private void CollectCidsFromJson(string json, HashSet<string> usedCids)
        {
            if (string.IsNullOrEmpty(json)) return;

            if (json.Trim().StartsWith("{"))
            {
                var itemDict = Utils.Json.Deserialize<Dictionary<string, object>>(json);
                if (itemDict != null)
                {
                    var cids = itemDict.Keys.Where(k => !int.TryParse(k, out _));
                    foreach (var cid in cids) usedCids.Add(cid);
                }
            }
            else if (json.Trim().StartsWith("["))
            {
                var itemArray = Utils.Json.Deserialize<List<Dictionary<string, object>>>(json);
                if (itemArray != null)
                {
                    foreach (var item in itemArray)
                    {
                        var cid = item.Get<string>("Cid");
                        if (!string.IsNullOrEmpty(cid)) usedCids.Add(cid);
                    }
                }
            }
        }

        /// <summary>
        /// 从JSON中收集CID（带详细信息）
        /// </summary>
        private void CollectCidsFromJsonWithDetails(string json, List<(string cid, string playerId, string field)> usedCids, string playerId, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return;

            if (json.Trim().StartsWith("{"))
            {
                var itemDict = Utils.Json.Deserialize<Dictionary<string, object>>(json);
                if (itemDict != null)
                {
                    var cids = itemDict.Keys.Where(k => !int.TryParse(k, out _));
                    foreach (var cid in cids) 
                    {
                        usedCids.Add((cid, playerId, fieldName));
                    }
                }
            }
            else if (json.Trim().StartsWith("["))
            {
                var itemArray = Utils.Json.Deserialize<List<Dictionary<string, object>>>(json);
                if (itemArray != null)
                {
                    foreach (var item in itemArray)
                    {
                        var cid = item.Get<string>("Cid");
                        if (!string.IsNullOrEmpty(cid)) 
                        {
                            usedCids.Add((cid, playerId, fieldName));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否为自动处理的CID
        /// </summary>
        private bool IsAutoHandleCid(string cid)
        {
            return IsTokenCid(cid) || IsSkillBook(cid);
        }

        /// <summary>
        /// 判断是否为代币CID
        /// </summary>
        private bool IsTokenCid(string cid)
        {
            return cid.Contains("游戏币") || cid.Contains("等价币") || cid.Contains("代币");
        }

        /// <summary>
        /// 判断是否为技能书CID
        /// </summary>
        private bool IsSkillBook(string cid)
        {
            return cid.Contains("武学经验") || cid.Contains("生活经验") || cid.Contains("心得");
        }

        #endregion
    }
}
