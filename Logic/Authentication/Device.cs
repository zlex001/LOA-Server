using Data.Database;
using Net;
using System.Net;

namespace Logic.Authentication
{
    public static class Device
    {
        public static async Task ProcessRegister(HttpListenerContext context)
        {
            var request = context.Request;
            var language = Net.Http.Instance.GetLanguage(context);

            string jsonData;
            using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
            {
                jsonData = await reader.ReadToEndAsync();
            }

            try
            {
                dynamic deviceInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData);

                string platformStr = deviceInfo.platform;
                string deviceId = deviceInfo.deviceId;

                if (string.IsNullOrEmpty(deviceId))
                {
                    await Net.Http.Instance.SendError(context.Response, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.DeviceIdMissing, language), 400);
                    return;
                }

                if (!Enum.TryParse(platformStr, out global::Data.Database.Device.Platforms platform))
                {
                    await Net.Http.Instance.SendError(context.Response, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.PlatformInvalid, language), 400);
                    return;
                }

                CreateOrUpdateDevice(deviceId, platform, language);

                var servers = global::Data.Database.Agent.Instance.GetAllServers();
                var serverList = servers.Select(s => new
                {
                    id = s.Id,
                    name = Logic.Text.Agent.Instance.Get(s.name, language),
                    ip = s.ip,
                    port = s.port
                }).ToList();

                var response = new
                {
                    ip = global::Data.Agent.Instance.ExternalIp.ToString(),
                    port = 19881,
                    servers = serverList
                };

                await Net.Http.Instance.SendJson(context.Response, response);
            }
            catch (Exception ex)
            {
                await Net.Http.Instance.SendError(context.Response, Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.JsonParseError, language), 400);
                return;
            }
        }

        public static void CreateOrUpdateDevice(string deviceId, global::Data.Database.Device.Platforms platform, global::Data.Text.Languages language)
        {
            var content = global::Data.Database.Agent.Instance.Content;

            if (!content.Has<global::Data.Database.Device>(d => d.Id == deviceId))
            {
                var device = new global::Data.Database.Device(deviceId, platform);
                device.PreferredLanguage = language;
                global::Data.Database.Agent.Instance.AddAsParent(device);
            }
            else
            {
                var device = content.Get<global::Data.Database.Device>(d => d.Id == deviceId);
                if (device.Platform == default)
                {
                    device.Platform = platform;
                }
                device.PreferredLanguage = language;
            }
        }

        public static void Bind(string deviceId, string playerId)
        {
            var content = global::Data.Database.Agent.Instance.Content;

            if (content.Has<global::Data.Database.Device>(d => d.Id == deviceId))
            {
                content.Get<global::Data.Database.Device>(d => d.Id == deviceId).player = playerId;
            }
            else
            {
                var device = new global::Data.Database.Device(deviceId, default);
                device.player = playerId;
                global::Data.Database.Agent.Instance.AddAsParent(device);
            }
        }

        public static string GetBoundAccountId(string deviceId)
        {
            var content = global::Data.Database.Agent.Instance.Content;
            
            if (content.Has<global::Data.Database.Device>(d => d.Id == deviceId))
            {
                var device = content.Get<global::Data.Database.Device>(d => d.Id == deviceId);
                return device.player;
            }
            
            return null;
        }


    }
}
