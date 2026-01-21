using System.Net;
using System.Text;
using Newtonsoft.Json;
using Utils;

namespace Net
{
    public class Http : Basic.Manager
    {
    #region Enumerations
    public enum Event
    {
        Device,
        Alipay,
        Hot,
        HotVersion,
        HotCheck,
        LanguageDetect,
        AdminLogin,
        PortStatus,
        Analytics,
        Debug,
        Commands,
        Connect,
        Ips,
        AdminIps,
        Connections,
        ConnectionsStats,
        ConnectionsFailures,
        ServerCreate,
        ServerUpdate,
        ServerDelete,
        DebugConfigGet,
        DebugConfigUpdate,
        DebugLogs,
        DebugLogFiles,
        DebugFileLogContent,
        PerformanceRealtime,
        PerformanceHistory,
        PerformanceSlowOps,
        ServerInfo,
        DatabaseStatus,
        SystemsStatus,
        PlayerList,
        PlayerDetails,
        PlayerHistory,
    }
    #endregion

        #region Singleton
        private static Http instance;
        public static Http Instance { get { if (instance == null) { instance = new Http(); } return instance; } }
        #endregion

        #region Fields and Properties
        private bool Open { get; set; }
        private HttpClient client = new HttpClient();
        private HttpListener listener;
        public Dictionary<(int port, string path), Event> ExactRoutes { get; set; } = new();
        public List<(string route, Event evt)> PatternRoutes { get; set; } = new();
        public List<string> UriPrefixs { get; set; } = new();
        
        private readonly string[] allowedOrigins = new[]
        {
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost:5174",
            "http://127.0.0.1:5174",
            "http://localhost:3000",
            "http://127.0.0.1:3000",
            "http://localhost:8080",
            "http://127.0.0.1:8080"
        };
        #endregion

        #region CORS Helpers
        private void SetCorsHeaders(HttpListenerRequest request, HttpListenerResponse response)
        {
            string origin = request.Headers["Origin"];
            if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin))
            {
                response.AddHeader("Access-Control-Allow-Origin", origin);
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
                response.AddHeader("Access-Control-Max-Age", "86400");
            }
        }
        #endregion

        #region Initialization and Core Logic
        public override void Init(params object[] args)
        {
            Logic.Agent.Instance.data.after.Register(Logic.Agent.Data.Open, OnLogicManagerOpenChanged);
        }

        private void OnLogicManagerOpenChanged(params object[] args)
        {
            bool open = (bool)args[0];
            if (open)
            {
                Task.Run(async () =>
                {
                    listener = new HttpListener();
                    foreach (string prefix in UriPrefixs)
                        listener.Prefixes.Add(prefix);

                    listener.Start();

                    while (true)
                    {
                        var context = await listener.GetContextAsync();
                        await Process(context);
                    }
                });
            }
        }

        private async Task Process(HttpListenerContext context)
        {
            SetCorsHeaders(context.Request, context.Response);
            
            if (context.Request.HttpMethod == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                context.Response.Close();
                return;
            }

            int port = context.Request.LocalEndPoint.Port;
            string path = context.Request.Url.LocalPath.TrimEnd('/');
            string method = context.Request.HttpMethod;

            if (ExactRoutes.TryGetValue((port, path), out var exactEvt))
            {
                if (path == "/api/administrator/debug/config" && method == "POST")
                {
                    monitor.Fire(Event.DebugConfigUpdate, context);
                }
                else
                {
                    monitor.Fire(exactEvt, context);
                }
            }
            else
            {
                var matched = PatternRoutes.FirstOrDefault(p => path.StartsWith(p.route.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(matched.route))
                {
                    monitor.Fire(matched.evt, context);
                }
                else
                {
                    await SendError(context.Response, "接口不存在", 404);
                }
            }
        }

        #endregion

        #region HTTP Client Methods
        public async Task<string> PostJson(string url, object data)
        {
            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }


        public async Task<T> PostJson<T>(string url, object data)
        {
            string result = await PostJson(url, data);
            return string.IsNullOrEmpty(result) ? default : JsonConvert.DeserializeObject<T>(result);
        }

        public async Task<string> Get(string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostForm(string url, Dictionary<string, string> form)
        {
            var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }


        public async Task<string> PostWithHeaders(string url, object data, Dictionary<string, string> headers)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            foreach (var h in headers)
                request.Headers.Add(h.Key, h.Value);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> DownloadFile(string url, string savePath)
        {
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(savePath, data);
            return true;
        }

        #endregion

        #region Language Helper
        public Logic.Text.Languages GetLanguage(HttpListenerContext context)
        {
            string acceptLanguage = context.Request.Headers["Accept-Language"];
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                string firstLang = acceptLanguage.Split(',')[0].Trim().Split(';')[0].ToLower();
                
                if (firstLang.StartsWith("zh-cn") || firstLang == "zh")
                    return Logic.Text.Languages.ChineseSimplified;
                if (firstLang.StartsWith("zh-tw") || firstLang.StartsWith("zh-hk"))
                    return Logic.Text.Languages.ChineseTraditional;
                if (firstLang.StartsWith("en"))
                    return Logic.Text.Languages.English;
                if (firstLang.StartsWith("ja"))
                    return Logic.Text.Languages.Japanese;
                if (firstLang.StartsWith("ko"))
                    return Logic.Text.Languages.Korean;
                if (firstLang.StartsWith("fr"))
                    return Logic.Text.Languages.French;
                if (firstLang.StartsWith("de"))
                    return Logic.Text.Languages.German;
                if (firstLang.StartsWith("es"))
                    return Logic.Text.Languages.Spanish;
                if (firstLang.StartsWith("pt"))
                    return Logic.Text.Languages.Portuguese;
                if (firstLang.StartsWith("ru"))
                    return Logic.Text.Languages.Russian;
                if (firstLang.StartsWith("tr"))
                    return Logic.Text.Languages.Turkish;
                if (firstLang.StartsWith("th"))
                    return Logic.Text.Languages.Thai;
                if (firstLang.StartsWith("id"))
                    return Logic.Text.Languages.Indonesian;
                if (firstLang.StartsWith("vi"))
                    return Logic.Text.Languages.Vietnamese;
                if (firstLang.StartsWith("it"))
                    return Logic.Text.Languages.Italian;
                if (firstLang.StartsWith("pl"))
                    return Logic.Text.Languages.Polish;
                if (firstLang.StartsWith("nl"))
                    return Logic.Text.Languages.Dutch;
                if (firstLang.StartsWith("sv"))
                    return Logic.Text.Languages.Swedish;
                if (firstLang.StartsWith("no") || firstLang.StartsWith("nb"))
                    return Logic.Text.Languages.Norwegian;
                if (firstLang.StartsWith("da"))
                    return Logic.Text.Languages.Danish;
                if (firstLang.StartsWith("fi"))
                    return Logic.Text.Languages.Finnish;
                if (firstLang.StartsWith("uk"))
                    return Logic.Text.Languages.Ukrainian;
            }
            
            return Logic.Text.Languages.ChineseSimplified;
        }
        #endregion

        #region Route Registration
        public void RegisterRoute(int port, string path, Event evt, Basic.Monitor.Function handler)
        {
            UriPrefixs.Add($"http://{Logic.Agent.Instance.InternalIp}:{port}{path}/");
            ExactRoutes[(port, path)] = evt;
            monitor.Register(evt, handler);
        }

        public void RegisterRoutes(int port, params (string path, Event evt, Basic.Monitor.Function handler)[] routes)
        {
            foreach (var (path, evt, handler) in routes)
                RegisterRoute(port, path, evt, handler);
        }

        public void RegisterPatternRoutes(int port, Event evt, Basic.Monitor.Function handler, params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                UriPrefixs.Add($"http://{Logic.Agent.Instance.InternalIp}:{port}{pattern}");
                PatternRoutes.Add((pattern, evt));
            }
            monitor.Register(evt, handler);
        }
        #endregion

        #region HTTP Server Response Methods
        public async Task SendJson(HttpListenerResponse response, object data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data);
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch
            {
                if (!response.OutputStream.CanWrite)
                {
                    return;
                }
                throw;
            }
        }

        public async Task SendError(HttpListenerResponse response, string message, int statusCode = 500)
        {
            try
            {
                if (!response.OutputStream.CanWrite)
                {
                    Utils.Debug.Log.Warning("HTTP", "[SendError] Response already sent, cannot send error");
                    return;
                }
                
                var error = new { Success = false, Message = message };
                string json = JsonConvert.SerializeObject(error);
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = statusCode;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Warning("HTTP", $"[SendError] Failed to send error response: {ex.Message}");
            }
        }

        public async Task SendFile(HttpListenerRequest request, HttpListenerResponse response, string filePath)
        {
            if (!File.Exists(filePath))
            {
                await SendError(response, "File Not Found", 404);
            }
            else
            {
                byte[] buffer = await File.ReadAllBytesAsync(filePath);
                response.ContentType = "application/octet-stream";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;

                if (request.HttpMethod == "HEAD")
                {
                    response.Close();
                }
                else
                {
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.Close();
                }
            }
        }

        #endregion
    }
}
