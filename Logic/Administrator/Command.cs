using Newtonsoft.Json;
using System.Net;
using System.Linq;

namespace Logic.Administrator
{
    public class Command
    {
        private static Command instance;
        public static Command Instance { get { if (instance == null) { instance = new Command(); } return instance; } }

        public async void OnDebug(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var response = context.Response;

            try
            {
                var query = context.Request.QueryString;
                string keyword = query["keyword"];
                int limit = int.TryParse(query["limit"], out var l) ? l : 1000;

                var logs = Utils.Debug.Log.GetLogs(null, keyword, limit);
                var currentPerformance = Utils.Debug.Performance.GetRealtime();
                var process = System.Diagnostics.Process.GetCurrentProcess();

                var result = logs
                    .OrderByDescending(log => log.Time)
                    .Select(log =>
                    {
                        var stackTrace = "";
                        if (log.Details != null)
                        {
                            try
                            {
                                if (log.Details is Exception ex)
                                    stackTrace = ex.StackTrace ?? "";
                                else if (log.Details is string str && str.Contains("at "))
                                    stackTrace = str;
                            }
                            catch { }
                        }

                        var cpuUsage = 0.0;
                        var memoryUsageMB = process.WorkingSet64 / (1024.0 * 1024.0);
                        var networkUsageKBps = 0.0;

                        var tag = MapLogLevelToTag(log.Category, log.Message, log.Level);

                        return new
                        {
                            Timestamp = log.Time.ToString("o"),
                            Message = log.Message,
                            StackTrace = stackTrace,
                            Source = log.Category,
                            CpuUsage = cpuUsage,
                            MemoryUsageMB = Math.Round(memoryUsageMB, 2),
                            NetworkUsageKBps = networkUsageKBps,
                            Tag = tag
                        };
                    }).ToList();

                await Net.Http.Instance.SendJson(response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[Debug] GetLogs Error: {ex.Message}", ex);
                await Net.Http.Instance.SendJson(response, new List<object>());
            }
        }

        private static string MapLogLevelToTag(string category, string message, Utils.Debug.Log.Level level)
        {
            var categoryUpper = category.ToUpper();
            var messageUpper = message.ToUpper();

            if (categoryUpper.Contains("EXCEPTION") || messageUpper.Contains("EXCEPTION"))
            {
                return "Exception";
            }

            if (level == Utils.Debug.Log.Level.Fatal)
            {
                return "Fatal";
            }

            if (categoryUpper.Contains("ERROR") || messageUpper.Contains("ERROR") || 
                categoryUpper.Contains("FAIL") || level == Utils.Debug.Log.Level.Error)
            {
                return "Error";
            }

            if (categoryUpper.Contains("WARN") || messageUpper.Contains("WARN") || 
                level == Utils.Debug.Log.Level.Warning)
            {
                return "Warning";
            }

            if (categoryUpper.Contains("PERFORMANCE") || messageUpper.Contains("SLOW") || 
                messageUpper.Contains("耗时") || categoryUpper.Contains("PERF"))
            {
                return "Performance";
            }


            return "Info";
        }

        public async void OnCommands(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var request = context.Request;
            var response = context.Response;

            try
            {
                string body;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                var command = JsonConvert.DeserializeObject<GMCommand>(body);
                if (command == null || string.IsNullOrEmpty(command.type))
                {
                    response.StatusCode = 400;
                    response.Close();
                    return;
                }

                if (command.type == "debug_snapshot")
                {
                    Utils.Debug.Snapshot.Capture("GM命令触发");
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                if (command.type == "behavior_tree")
                {
                    var player = global::Data.Agent.Instance.Content.Get<global::Data.Player>();
                    if (player == null)
                    {
                        var errorResult = new { success = false, message = "未找到玩家" };
                        await Net.Http.Instance.SendJson(response, errorResult);
                        return;
                    }

                    if (player.Leader == null)
                    {
                        var errorResult = new { success = false, message = "玩家未跟随任何NPC" };
                        await Net.Http.Instance.SendJson(response, errorResult);
                        return;
                    }

                    var npc = player.Leader;
                    if (npc.BtRoot == null)
                    {
                        var errorResult = new { success = false, message = "该NPC没有行为树" };
                        await Net.Http.Instance.SendJson(response, errorResult);
                        return;
                    }

                    npc.BtRoot.ExecuteWithDebug(npc);

                    var result = new { success = true, message = "行为树执行结果已输出到日志" };
                    await Net.Http.Instance.SendJson(response, result);
                    return;
                }

                if (command.type == "play_plot")
                {
                    var player = global::Data.Agent.Instance.Content.Get<global::Data.Player>();
                    if (player == null)
                    {
                        var errorResult = new { success = false, message = "未找到玩家" };
                        await Net.Http.Instance.SendJson(response, errorResult);
                        return;
                    }

                    if (command.plotId <= 0)
                    {
                        var quests = global::Data.Config.Agent.Instance.Content.Gets<global::Data.Config.Quest>()
                            .Select(p => p.Id)
                            .ToList();
                        var listResult = new { success = true, message = "Available quest ID list", quests = quests };
                        await Net.Http.Instance.SendJson(response, listResult);
                        return;
                    }

                    var questConfig = global::Data.Config.Agent.Instance.Content.Get<global::Data.Config.Quest>(p => p.Id == command.plotId);
                    if (questConfig == null)
                    {
                        var errorResult = new { success = false, message = $"Quest not found: {command.plotId}" };
                        await Net.Http.Instance.SendJson(response, errorResult);
                        return;
                    }

                    var quest = new global::Data.Quest { Config = questConfig };
                    Quest.DialogueSender.Do(player, quest);

                    var result = new { success = true, message = $"Playing quest ID: {command.plotId}" };
                    await Net.Http.Instance.SendJson(response, result);
                    return;
                }

                if (command.type == "add_gem")
                {
                    var gmResult = GameMaster.AddGem(command.playerId, command.amount > 0 ? command.amount : 100);
                    var result = new { success = gmResult.Success, message = gmResult.Message };
                    await Net.Http.Instance.SendJson(response, result);
                    return;
                }

                var defaultResult = new { success = true, message = $"已处理命令类型：{command.type}" };
                await Net.Http.Instance.SendJson(response, defaultResult);
            }
            catch (Exception ex)
            {
                var result = new { success = false, message = ex.Message };
                await Net.Http.Instance.SendJson(response, result);
            }
        }

        private class GMCommand
        {
            public string type { get; set; }
            public string snapshotTime { get; set; }
            public int plotId { get; set; }
            public string playerId { get; set; }
            public int amount { get; set; }
        }
    }
}

