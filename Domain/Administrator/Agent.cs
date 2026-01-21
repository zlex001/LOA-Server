using System.Net;

namespace Domain.Administrator
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        public void Init()
        {
            Net.Http.Instance.RegisterRoutes(9000,
                ("/api/administrator/login", Net.Http.Event.AdminLogin, Operation.Instance.OnAdminLogin),
                ("/api/administrator/port-status", Net.Http.Event.PortStatus, Operation.Instance.OnPortStatus),
                ("/api/administrator/analytics", Net.Http.Event.Analytics, Analytics.Instance.OnAnalytics),
                ("/api/administrator/debug", Net.Http.Event.Debug, Command.Instance.OnDebug),
                ("/api/administrator/commands", Net.Http.Event.Commands, Command.Instance.OnCommands),
                ("/api/administrator/connections", Net.Http.Event.Connections, Monitor.Instance.OnConnections),
                ("/api/administrator/connections/stats", Net.Http.Event.ConnectionsStats, Monitor.Instance.OnConnectionsStats),
                ("/api/administrator/connections/failures", Net.Http.Event.ConnectionsFailures, Monitor.Instance.OnConnectionsFailures),
                ("/api/authentication/ips", Net.Http.Event.AdminIps, Operation.Instance.OnIps),
                ("/api/admin/server/create", Net.Http.Event.ServerCreate, OnServerCreate),
                ("/api/admin/server/update", Net.Http.Event.ServerUpdate, OnServerUpdate),
                ("/api/admin/server/delete", Net.Http.Event.ServerDelete, OnServerDelete),
                ("/api/administrator/debug/config", Net.Http.Event.DebugConfigGet, Monitor.Instance.OnGetDebugConfig),
                ("/api/administrator/debug/logs", Net.Http.Event.DebugLogs, DebugControl.Instance.OnGetDebugLogs),
                ("/api/administrator/debug/log-files", Net.Http.Event.DebugLogFiles, DebugControl.Instance.OnGetLogFiles),
                ("/api/administrator/debug/file-log-content", Net.Http.Event.DebugFileLogContent, DebugControl.Instance.OnGetFileLogContent),
                ("/api/administrator/performance/realtime", Net.Http.Event.PerformanceRealtime, PerformanceMonitor.Instance.OnGetRealtimePerformance),
                ("/api/administrator/performance/history", Net.Http.Event.PerformanceHistory, PerformanceMonitor.Instance.OnGetPerformanceHistory),
                ("/api/administrator/performance/slow-operations", Net.Http.Event.PerformanceSlowOps, PerformanceMonitor.Instance.OnGetSlowOperations),
                ("/api/administrator/server/info", Net.Http.Event.ServerInfo, ServerInfo.Instance.OnGetServerInfo),
                ("/api/administrator/database/status", Net.Http.Event.DatabaseStatus, ServerInfo.Instance.OnGetDatabaseStatus),
                ("/api/administrator/systems/status", Net.Http.Event.SystemsStatus, ServerInfo.Instance.OnGetSystemsStatus),
                ("/api/administrator/players", Net.Http.Event.PlayerList, PlayerMonitor.Instance.OnGetPlayerList)
            );
            Net.Http.Instance.RegisterPatternRoutes(9000, Net.Http.Event.PlayerDetails, PlayerMonitor.Instance.OnGetPlayerDetails, "/api/administrator/players/");
            Net.Http.Instance.RegisterPatternRoutes(9000, Net.Http.Event.PlayerHistory, PlayerMonitor.Instance.OnGetPlayerHistory, "/api/administrator/players/");
            Net.Http.Instance.UriPrefixs.Add($"http://{Logic.Agent.Instance.InternalIp}:9000/api/admin/server/create/");
            Net.Http.Instance.UriPrefixs.Add($"http://{Logic.Agent.Instance.InternalIp}:9000/api/admin/server/update/");
            Net.Http.Instance.UriPrefixs.Add($"http://{Logic.Agent.Instance.InternalIp}:9000/api/admin/server/delete/");
            Net.Http.Instance.UriPrefixs.Add($"http://{Logic.Agent.Instance.InternalIp}:9000/api/administrator/debug/config/");
            Net.Http.Instance.monitor.Register(Net.Http.Event.DebugConfigUpdate, Monitor.Instance.OnUpdateDebugConfig);
            ConnectionMonitor.Instance.Init();
        }

        private async void OnServerCreate(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                string jsonData;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    jsonData = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    await Net.Http.Instance.SendError(context.Response, "������Ϊ��", 400);
                    return;
                }

                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData);
                string id = requestData.id;
                int name = requestData.name;
                string ip = requestData.ip;
                int port = requestData.port;

                if (string.IsNullOrWhiteSpace(id))
                {
                    await Net.Http.Instance.SendError(context.Response, "����ID����Ϊ��", 400);
                    return;
                }

                var existingServer = Logic.Database.Agent.Instance.GetServerById(id);
                if (existingServer != null)
                {
                    await Net.Http.Instance.SendError(context.Response, "����ID�Ѵ���", 400);
                    return;
                }

                var server = new Logic.Database.Server(id, name, ip, port);
                Logic.Database.Agent.Instance.Add(server);
                Logic.Database.Agent.Instance.Insert(Logic.Config.MySQL.ConnectionString, server);

                var result = new { code = 0, message = "�����ɹ�" };
                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[ServerCreate] Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        private async void OnServerUpdate(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                string jsonData;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    jsonData = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    await Net.Http.Instance.SendError(context.Response, "������Ϊ��", 400);
                    return;
                }

                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData);
                string id = requestData.id;
                int name = requestData.name;
                string ip = requestData.ip;
                int port = requestData.port;

                if (string.IsNullOrWhiteSpace(id))
                {
                    await Net.Http.Instance.SendError(context.Response, "����ID����Ϊ��", 400);
                    return;
                }

                var server = Logic.Database.Agent.Instance.GetServerById(id);
                if (server == null)
                {
                    await Net.Http.Instance.SendError(context.Response, "����������", 404);
                    return;
                }

                server.name = name;
                server.ip = ip;
                server.port = port;
                Logic.Database.Agent.Instance.Update(Logic.Config.MySQL.ConnectionString, server);

                var result = new { code = 0, message = "�޸ĳɹ�" };
                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[ServerUpdate] Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        private async void OnServerDelete(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                string jsonData;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    jsonData = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    await Net.Http.Instance.SendError(context.Response, "������Ϊ��", 400);
                    return;
                }

                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData);
                string id = requestData.id;

                if (string.IsNullOrWhiteSpace(id))
                {
                    await Net.Http.Instance.SendError(context.Response, "����ID����Ϊ��", 400);
                    return;
                }

                var server = Logic.Database.Agent.Instance.GetServerById(id);
                if (server == null)
                {
                    await Net.Http.Instance.SendError(context.Response, "����������", 404);
                    return;
                }

                Logic.Database.Agent.Instance.Remove(server);
                Logic.Database.Agent.Instance.Delete(Logic.Config.MySQL.ConnectionString, server);

                var result = new { code = 0, message = "ɾ���ɹ�" };
                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[ServerDelete] Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }
    }
}

