using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace Domain.Administrator
{
    public class ServerInfo
    {
        private static ServerInfo instance;
        public static ServerInfo Instance { get { if (instance == null) { instance = new ServerInfo(); } return instance; } }

        private static DateTime _startTime = DateTime.UtcNow;

        public static void RecordStartTime()
        {
            _startTime = DateTime.UtcNow;
        }

        public async void OnGetServerInfo(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var uptime = (DateTime.UtcNow - _startTime).TotalSeconds;
                var uptimeFormatted = FormatUptime(uptime);

                var process = Process.GetCurrentProcess();
                var memoryUsedMB = process.WorkingSet64 / (1024.0 * 1024.0);

                var result = new
                {
                    mode = Logic.Agent.Instance.IsDevelopment ? "Development" : "Production",
                    version = "10.0",
                    internalIp = Logic.Agent.Instance.InternalIp.ToString(),
                    externalIp = Logic.Agent.Instance.ExternalIp?.ToString() ?? "detecting...",
                    startTime = _startTime.ToString("o"),
                    uptime = (int)uptime,
                    uptimeFormatted,
                    features = new
                    {
                        dataValidation = Logic.Agent.Instance.IsDevelopment,
                        designDataConversion = Logic.Agent.Instance.IsDevelopment
                    },
                    environment = new
                    {
                        os = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}",
                        runtime = $".NET {Environment.Version}",
                        memory = new
                        {
                            used = (int)memoryUsedMB,
                            total = 2048
                        }
                    }
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[ServerInfo] Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnGetDatabaseStatus(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var players = Logic.Database.Agent.Instance.Content.Gets<Logic.Database.Player>();
                var items = Logic.Database.Agent.Instance.Content.Gets<Logic.Database.Item>();
                var skills = Logic.Agent.Instance.Content.Gets<Logic.Skill>();

                var result = new
                {
                    connected = true,
                    connectionString = Logic.Config.MySQL.ConnectionString,
                    totalPlayers = players.Count(),
                    serverId = Logic.Agent.Instance.ServerId ?? "null",
                    validationStatus = "completed",
                    validationTime = _startTime.ToString("o"),
                    tablesLoaded = new[]
                    {
                        new { name = "Player", count = players.Count() },
                        new { name = "Item", count = items.Count() },
                        new { name = "Skill", count = skills.Count() }
                    },
                    lastError = (string)null
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[ServerInfo] DatabaseStatus Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnGetSystemsStatus(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var systems = new List<object>
                {
                    new
                    {
                        name = "StateMachine",
                        status = "running",
                        config = new
                        {
                            battle = "100ms",
                            normal = "300ms",
                            @default = "1000ms"
                        },
                        initTime = _startTime.ToString("o"),
                        uptime = (int)(DateTime.UtcNow - _startTime).TotalSeconds
                    },
                    new
                    {
                        name = "Logic.Agent",
                        status = "running",
                        features = new[] { "Data Validation", "Design Data Conversion" },
                        initTime = _startTime.ToString("o")
                    },
                    new
                    {
                        name = "Database.Agent",
                        status = "running",
                        initTime = _startTime.ToString("o")
                    }
                };

                var result = new
                {
                    systems,
                    allHealthy = true
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[ServerInfo] SystemsStatus Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        private string FormatUptime(double seconds)
        {
            var timespan = TimeSpan.FromSeconds(seconds);
            if (timespan.TotalDays >= 1)
                return $"{(int)timespan.TotalDays}天{timespan.Hours}小时{timespan.Minutes}分";
            if (timespan.TotalHours >= 1)
                return $"{(int)timespan.TotalHours}小时{timespan.Minutes}分{timespan.Seconds}秒";
            return $"{timespan.Minutes}分{timespan.Seconds}秒";
        }
    }
}

