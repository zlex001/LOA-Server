using Logic.Database;
using Net;
using System.Net;

namespace Domain.Authentication
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
                    await Net.Http.Instance.SendError(context.Response, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.DeviceIdMissing, language), 400);
                    return;
                }

                if (!Enum.TryParse(platformStr, out Logic.Database.Device.Platforms platform))
                {
                    await Net.Http.Instance.SendError(context.Response, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.PlatformInvalid, language), 400);
                    return;
                }

                CreateOrUpdateDevice(deviceId, platform, language);

                var servers = Logic.Database.Agent.Instance.GetAllServers();
                var serverList = servers.Select(s => new
                {
                    id = s.Id,
                    name = Domain.Text.Agent.Instance.Get(s.name, language),
                    ip = s.ip,
                    port = s.port
                }).ToList();

                var response = new
                {
                    ip = Logic.Agent.Instance.ExternalIp.ToString(),
                    port = 19881,
                    servers = serverList
                };

                await Net.Http.Instance.SendJson(context.Response, response);
            }
            catch (Exception ex)
            {
                await Net.Http.Instance.SendError(context.Response, Domain.Text.Agent.Instance.Get(Logic.Text.Labels.JsonParseError, language), 400);
                return;
            }
        }

        public static void CreateOrUpdateDevice(string deviceId, Logic.Database.Device.Platforms platform, Logic.Text.Languages language)
        {
            var content = Logic.Database.Agent.Instance.Content;

            if (!content.Has<Logic.Database.Device>(d => d.Id == deviceId))
            {
                var device = new Logic.Database.Device(deviceId, platform);
                device.PreferredLanguage = language;
                Logic.Database.Agent.Instance.AddAsParent(device);
            }
            else
            {
                var device = content.Get<Logic.Database.Device>(d => d.Id == deviceId);
                if (device.Platform == default)
                {
                    device.Platform = platform;
                }
                device.PreferredLanguage = language;
            }
        }

        public static void Bind(string deviceId, string playerId)
        {
            var content = Logic.Database.Agent.Instance.Content;

            if (content.Has<Logic.Database.Device>(d => d.Id == deviceId))
            {
                content.Get<Logic.Database.Device>(d => d.Id == deviceId).player = playerId;
            }
            else
            {
                var device = new Logic.Database.Device(deviceId, default);
                device.player = playerId;
                Logic.Database.Agent.Instance.AddAsParent(device);
            }
        }


    }
}
