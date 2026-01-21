using Newtonsoft.Json;
using System.Net;

namespace Domain.Administrator
{
    public class Monitor
    {
        private static Monitor instance;
        public static Monitor Instance { get { if (instance == null) { instance = new Monitor(); } return instance; } }
        private string GetCategoryDescription(string category)
        {
            return category switch
            {
                "DEBUG" => "通用调试",
                "DISPLAY" => "显示系统",
                "ADMIN" => "管理员操作",
                "AUTH" => "认证系统",
                "PERFORMANCE" => "性能监控",
                "HOT" => "热更新",
                "STATE" => "状态机",
                _ => category
            };
        }
        public async void OnGetDebugConfig(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var categories = Utils.Debug.Log.GetCategories().Select(kv => new
                {
                    name = kv.Key,
                    enabled = kv.Value,
                    description = GetCategoryDescription(kv.Key)
                }).ToList();

                var config = new
                {
                    categories,
                    bufferSize = Utils.Debug.Log.GetTotalCount()
                };

                await Net.Http.Instance.SendJson(context.Response, config);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[DebugControl] GetConfig Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnConnections(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var request = context.Request;
            var response = context.Response;

            try
            {
                var queryString = request.Url.Query;
                var queryParameters = System.Web.HttpUtility.ParseQueryString(queryString);

                string statusFilter = queryParameters["status"] ?? "all";
                string ipFilter = queryParameters["ip"];
                string startTimeStr = queryParameters["startTime"];
                string endTimeStr = queryParameters["endTime"];
                string limitStr = queryParameters["limit"] ?? "100";

                DateTime? startTime = null;
                DateTime? endTime = null;

                if (!string.IsNullOrEmpty(startTimeStr) && DateTime.TryParse(startTimeStr, out var st))
                {
                    startTime = st;
                }

                if (!string.IsNullOrEmpty(endTimeStr) && DateTime.TryParse(endTimeStr, out var et))
                {
                    endTime = et;
                }

                if (!int.TryParse(limitStr, out int limit))
                {
                    limit = 100;
                }

                var connections = ConnectionMonitor.Instance.GetConnections(
                    statusFilter, ipFilter, startTime, endTime, limit);

                var result = new
                {
                    connections = connections.Select(c => new
                    {
                        id = c.Id,
                        ip = c.Ip,
                        port = c.Port,
                        connectTime = c.ConnectTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        status = c.Status.ToString().ToLower(),
                        duration = c.Duration,
                        playerId = c.PlayerId,
                        playerName = c.PlayerName,
                        protocol = c.Protocol,
                        clientVersion = c.ClientVersion,
                        failReason = c.FailReason,
                        retryCount = c.RetryCount
                    }),
                    total = connections.Count
                };

                await Net.Http.Instance.SendJson(response, result);
            }
            catch (Exception ex)
            {
                await Net.Http.Instance.SendError(response, ex.Message, 500);
            }
        }

        public async void OnConnectionsStats(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var request = context.Request;
            var response = context.Response;

            try
            {
                var stats = ConnectionMonitor.Instance.GetStatistics();
                await Net.Http.Instance.SendJson(response, stats);
            }
            catch (Exception ex)
            {
                await Net.Http.Instance.SendError(response, ex.Message, 500);
            }
        }

        public async void OnConnectionsFailures(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var request = context.Request;
            var response = context.Response;

            try
            {
                var queryString = request.Url.Query;
                var queryParameters = System.Web.HttpUtility.ParseQueryString(queryString);

                string startTimeStr = queryParameters["startTime"];
                string endTimeStr = queryParameters["endTime"];
                string limitStr = queryParameters["limit"] ?? "50";

                DateTime? startTime = null;
                DateTime? endTime = null;

                if (!string.IsNullOrEmpty(startTimeStr) && DateTime.TryParse(startTimeStr, out var st))
                {
                    startTime = st;
                }

                if (!string.IsNullOrEmpty(endTimeStr) && DateTime.TryParse(endTimeStr, out var et))
                {
                    endTime = et;
                }

                if (!int.TryParse(limitStr, out int limit))
                {
                    limit = 50;
                }

                var failures = ConnectionMonitor.Instance.GetFailures(startTime, endTime, limit);

                var result = new
                {
                    failures = failures.Select(f => new
                    {
                        ip = f.Ip,
                        time = f.ConnectTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        reason = f.FailReason ?? "Unknown",
                        port = f.Port,
                        retryCount = f.RetryCount,
                        detail = f.FailReason
                    }),
                    total = failures.Count
                };

                await Net.Http.Instance.SendJson(response, result);
            }
            catch (Exception ex)
            {
                await Net.Http.Instance.SendError(response, ex.Message, 500);
            }
        }
        public async void OnUpdateDebugConfig(params object[] args)
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
                    await Net.Http.Instance.SendError(context.Response, "请求体为空", 400);
                    return;
                }

                dynamic requestData = JsonConvert.DeserializeObject(jsonData);
                var affected = new List<string>();

                if (requestData.categories != null)
                {
                    var categories = JsonConvert.DeserializeObject<Dictionary<string, bool>>(requestData.categories.ToString());
                    foreach (var kv in categories)
                    {
                        Utils.Debug.Log.SetCategoryEnabled(kv.Key, kv.Value);
                        affected.Add(kv.Key);
                    }
                }

                var currentCategories = Utils.Debug.Log.GetCategories().Select(kv => new
                {
                    name = kv.Key,
                    enabled = kv.Value,
                    description = GetCategoryDescription(kv.Key)
                }).ToList();

                var currentConfig = new
                {
                    categories = currentCategories,
                    bufferSize = Utils.Debug.Log.GetTotalCount()
                };

                var result = new
                {
                    success = true,
                    message = "调试配置已更新",
                    affected,
                    currentConfig
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[DebugControl] UpdateConfig Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

    }
}