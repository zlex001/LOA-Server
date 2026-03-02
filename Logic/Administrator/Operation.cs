using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Logic.Administrator
{
    public class Operation
    {
        private static Operation instance;
        public static Operation Instance { get { if (instance == null) { instance = new Operation(); } return instance; } }

        public async void OnAdminLogin(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var request = context.Request;
            var response = context.Response;

            try
            {
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                string body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    await Net.Http.Instance.SendError(response, "请求体为空", 400);
                    return;
                }

                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);

                if (data == null)
                {
                    await Net.Http.Instance.SendError(response, "请求体格式错误", 400);
                    return;
                }

                if (data.TryGetValue("username", out var username) &&
                    data.TryGetValue("password", out var password))
                {
                    if (username == "admin" && password == "123456")
                    {
                        await Net.Http.Instance.SendJson(response, new { success = true, token = "admin-token" });
                    }
                    else
                    {
                        await Net.Http.Instance.SendJson(response, new { success = false, message = "账号或密码错误" });
                    }
                }
                else
                {
                    await Net.Http.Instance.SendError(response, "缺少字段", 400);
                }
            }
            catch
            {
                await Net.Http.Instance.SendError(response, "服务器内部错误", 500);
            }
        }

        public async void OnIps(params object[] args)
        {
            var context = (HttpListenerContext)args[0];

            var servers = global::Data.Database.Agent.Instance.GetAllServers();

            var serverList = servers.Select(s => new
            {
                id = s.Id,
                name = s.name,
                ip = s.ip,
                port = s.port
            }).ToList();

            await Net.Http.Instance.SendJson(context.Response, serverList);
        }

        public async void OnPortStatus(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var response = context.Response;

            try
            {
                var portStatus = new Dictionary<int, bool>
                {
                    { 8880, true },
                    { 9000, true },
                    { 8882, true }
                };

                await Net.Http.Instance.SendJson(response, portStatus);
            }
            catch
            {
                var empty = new Dictionary<int, bool>();
                await Net.Http.Instance.SendJson(response, empty);
            }
        }
    }
}

