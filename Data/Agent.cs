using System.Net;
using System.Net.Sockets;
using Utils;

namespace Data
{
    public class Agent : Core
    {
        public enum Data
        {
            Open,
        }
        public bool IsDevelopment { get; set; }
        public string ServerId { get; set; }
        public Database.Server CurrentServer => !string.IsNullOrEmpty(ServerId) ? Database.Agent.Instance.GetServerById(ServerId) : null;
        public IPAddress InternalIp => GetInternalIp();
        private IPAddress externalIp;
        public IPAddress ExternalIp => externalIp ?? InternalIp;
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }
        public Agent()
        {
            data.after.Register(Data.Open, OnOpen);

        }
        public bool Open { get => data.Get<bool>(Data.Open); set => data.Change(Data.Open, value); }
        public override void Init(params object[] args)
        {
            if (!IsDevelopment)
            {
                FetchExternalIpAsync();
            }

            if (IsDevelopment)
            {
                Validation.Agent.Instance.Init();
                Design.Agent.Instance.Init();
            }

            Config.Agent.Instance.Init();
            Database.Agent.Instance.Init();
            Text.Instance.Init();
        }

        private void FetchExternalIpAsync()
        {
            Task.Run(async () =>
            {
                externalIp = await GetExternalIp();
            });
        }

        private IPAddress GetInternalIp()
        {
            if (IsDevelopment)
            {
                return IPAddress.Loopback;
            }

            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork) ?? IPAddress.Loopback;
            }
            catch
            {
                Utils.Debug.Log.Warning("LOGIC", "Failed to get internal IP, using loopback");
                return IPAddress.Loopback;
            }
        }

        private async Task<IPAddress> GetExternalIp()
        {
            string[] services = new[]
            {
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://ifconfig.me/ip"
            };

            foreach (string service in services)
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(3);
                        string response = await client.GetStringAsync(service);
                        response = response.Trim();
                        if (IPAddress.TryParse(response, out IPAddress ip))
                        {
                            return ip;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }
        public void OnOpen(params object[] args)
        {
            bool v = (bool)args[0];
            if (v)
            {
                var sceneDataList = Utils.Binary.Deserialize<List<Database.Scene>>($"{Utils.Paths.Config}/Shortest.bin", Utils.SerializeFormat.Binary);
                
                foreach (var data in sceneDataList)
                {
                    Create<Scene>(data);
                }
            }

        }
    }
}