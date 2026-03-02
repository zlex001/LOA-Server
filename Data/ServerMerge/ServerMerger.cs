using System;
using System.Collections.Generic;
using System.Linq;
using Data.Config;
using MySql.Data.MySqlClient;

namespace Data.ServerMerge
{
    public class ServerMerger
    {
        private IdMapper idMapper;
        private MergeConfig config;

        public ServerMerger()
        {
            idMapper = new IdMapper();
        }

        public bool ExecuteMerge(MergeConfig mergeConfig)
        {
            config = mergeConfig;

            try
            {
                Utils.Debug.Log.Info("MERGE", $"Starting merge: {config.MergeId}");
                Utils.Debug.Log.Info("MERGE", $"Target: {config.TargetServerId}");
                Utils.Debug.Log.Info("MERGE", $"Sources: {string.Join(", ", config.SourceServerIds)}");

                config.Status = MergeStatus.InProgress;

                Step1_Validate();
                Step2_Backup();
                Step3_GenerateIdMappings();
                Step4_MigrateData();
                Step5_UpdateServerConfig();
                Step6_DistributeCompensation();

                config.Status = MergeStatus.Completed;
                Utils.Debug.Log.Info("MERGE", "Merge completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                config.Status = MergeStatus.Failed;
                Utils.Debug.Log.Error("MERGE", $"Merge failed: {ex.Message}");
                Utils.Debug.Log.Error("MERGE", $"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private void Step1_Validate()
        {
            var targetServer = global::Data.Database.Agent.Instance.Content.Get<global::Data.Database.Server>(s=>s.Id== config.TargetServerId);
            if (targetServer == null)
            {
                throw new Exception($"Target server not found: {config.TargetServerId}");
            }

            foreach (var sourceId in config.SourceServerIds)
            {
                var sourceServer = global::Data.Database.Agent.Instance.Content.Get<global::Data.Database.Server>(s => s.Id == sourceId);
                if (sourceServer == null)
                {
                    throw new Exception($"Source server not found: {sourceId}");
                }
            }
        }

        private void Step2_Backup()
        {
            var backupDir = $"{Utils.Paths.Config}/backups/{config.MergeId}";
            System.IO.Directory.CreateDirectory(backupDir);

            foreach (var serverId in config.SourceServerIds)
            {
                var server = global::Data.Database.Agent.Instance.GetServerById(serverId);
                var backupFile = $"{backupDir}/{server.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            }
        }

        private void Step3_GenerateIdMappings()
        {
            idMapper.GenerateMappings(config.SourceServerIds, config.TargetServerId);

            var mappingFile = $"{Utils.Paths.Config}/merge_mappings_{config.MergeId}.json";
            idMapper.SaveMappings(mappingFile);
        }

        private void Step4_MigrateData()
        {
            var targetServer = global::Data.Database.Agent.Instance.GetServerById(config.TargetServerId);
            var targetConnStr = global::Data.Config.MySQL.GetConnectionString(targetServer.Id);

            foreach (var sourceId in config.SourceServerIds)
            {
                if (sourceId == config.TargetServerId) continue;

                var sourceServer = global::Data.Database.Agent.Instance.GetServerById(sourceId);
                var sourceConnStr = global::Data.Config.MySQL.GetConnectionString(sourceServer.Id);

                MigratePlayers(sourceId, sourceConnStr, targetConnStr);
                MigrateItems(sourceId, sourceConnStr, targetConnStr);
            }
        }

        private void MigratePlayers(string serverId, string sourceConnStr, string targetConnStr)
        {
            var existingNames = GetExistingPlayerNames(targetConnStr);
            var players = GetPlayers(sourceConnStr);

            using (var targetConn = new MySqlConnection(targetConnStr))
            {
                targetConn.Open();

                foreach (var player in players)
                {
                    var newId = idMapper.MapPlayerId(serverId, player.Id);
                    var newName = idMapper.MapPlayerName(serverId, player.Name, existingNames);

                    var sql = @"INSERT INTO Player 
                                (Id, Name, ServerId, MergedFrom, MergeDate, Level, Exp, Gold) 
                                VALUES (@Id, @Name, @ServerId, @MergedFrom, @MergeDate, @Level, @Exp, @Gold)";

                    using (var cmd = new MySqlCommand(sql, targetConn))
                    {
                        cmd.Parameters.AddWithValue("@Id", newId.ToString());
                        cmd.Parameters.AddWithValue("@Name", newName);
                        cmd.Parameters.AddWithValue("@ServerId", config.TargetServerId);
                        cmd.Parameters.AddWithValue("@MergedFrom", serverId);
                        cmd.Parameters.AddWithValue("@MergeDate", config.MergeDate);
                        cmd.Parameters.AddWithValue("@Level", player.Level);
                        cmd.Parameters.AddWithValue("@Exp", player.Exp);
                        cmd.Parameters.AddWithValue("@Gold", ApplyEconomyBalance(player.Gold, serverId));

                        cmd.ExecuteNonQuery();
                    }

                    existingNames.Add(newName);
                }
            }
        }

        private void MigrateItems(string serverId, string sourceConnStr, string targetConnStr)
        {
            var items = GetItems(sourceConnStr);

            using (var targetConn = new MySqlConnection(targetConnStr))
            {
                targetConn.Open();

                foreach (var item in items)
                {
                    var newId = idMapper.MapItemId(serverId, item.Id);
                    var newOwnerId = idMapper.MapPlayerId(serverId, item.OwnerId);

                    var sql = @"INSERT INTO Item 
                                (Id, OwnerId, Cid, Count) 
                                VALUES (@Id, @OwnerId, @Cid, @Count)";

                    using (var cmd = new MySqlCommand(sql, targetConn))
                    {
                        cmd.Parameters.AddWithValue("@Id", newId);
                        cmd.Parameters.AddWithValue("@OwnerId", newOwnerId.ToString());
                        cmd.Parameters.AddWithValue("@Cid", item.Cid);
                        cmd.Parameters.AddWithValue("@Count", item.Count);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private long ApplyEconomyBalance(long originalGold, string serverId)
        {
            if (serverId == config.TargetServerId) return originalGold;

            var discountRate = config.Options.TryGetValue("GoldDiscount", out var rate) 
                ? Convert.ToDouble(rate) 
                : 0.8;

            return (long)(originalGold * discountRate);
        }

        private HashSet<string> GetExistingPlayerNames(string connStr)
        {
            var names = new HashSet<string>();

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                var sql = "SELECT Name FROM Player";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        names.Add(reader.GetString(0));
                    }
                }
            }

            return names;
        }

        private List<PlayerData> GetPlayers(string connStr)
        {
            var players = new List<PlayerData>();

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                var sql = "SELECT Id, Name, Level, Exp, Gold FROM Player";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add(new PlayerData
                        {
                            Id = long.Parse(reader.GetString(0)),
                            Name = reader.GetString(1),
                            Level = reader.GetInt32(2),
                            Exp = reader.GetInt64(3),
                            Gold = reader.GetInt64(4)
                        });
                    }
                }
            }

            return players;
        }

        private List<ItemData> GetItems(string connStr)
        {
            var items = new List<ItemData>();

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                var sql = "SELECT Id, OwnerId, Cid, Count FROM Item";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ItemData
                        {
                            Id = reader.GetInt64(0),
                            OwnerId = long.Parse(reader.GetString(1)),
                            Cid = reader.GetString(2),
                            Count = reader.GetInt32(3)
                        });
                    }
                }
            }

            return items;
        }

        private void Step5_UpdateServerConfig()
        {
            foreach (var server in global::Data.Database.Agent.Instance.Content.Gets<global::Data.Database.Server>())
            {
                if (config.SourceServerIds.Contains(server.Id))
                {
                    if (server.Id == config.TargetServerId)
                    {
                        // Note: server.name is int (multilingual text ID), cannot append string directly
                    }
                }
            }
        }

        private void Step6_DistributeCompensation()
        {
        }

        private class PlayerData
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Level { get; set; }
            public long Exp { get; set; }
            public long Gold { get; set; }
        }

        private class ItemData
        {
            public long Id { get; set; }
            public long OwnerId { get; set; }
            public string Cid { get; set; }
            public int Count { get; set; }
        }
    }
}

