using Basic;
using Logic;
using Logic.Database;
using Net;
using Net.Protocol;
using System;
using System.Net;
using Utils;

namespace Domain.Authentication
{
    public class Agent
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }

        private static readonly DateTime serverStartTime = DateTime.Now;
        private static System.Diagnostics.PerformanceCounter cpuCounter;
        private static readonly object cpuCounterLock = new object();
        private static DateTime lastCpuSampleTime = DateTime.MinValue;
        private static float lastCpuUsage = 0f;
        
        public static void CleanupResources()
        {
            lock (cpuCounterLock)
            {
                cpuCounter?.Dispose();
                cpuCounter = null;
                lastCpuSampleTime = DateTime.MinValue;
                lastCpuUsage = 0f;
            }
        }

        public void Init()
        {
            Utils.Debug.Log.Info("AUTH", "[Agent.Init] Starting authentication initialization...");
            
            Net.Http.Instance.RegisterRoutes(8880,
                ("/api/authentication/device", Net.Http.Event.Device, OnDevice),
                ("/api/authentication/language-detect", Net.Http.Event.LanguageDetect, OnLanguageDetect),
                ("/api/authentication/ips", Net.Http.Event.Ips, OnIps),
                ("/api/authentication/connect", Net.Http.Event.Connect, OnConnect)
            );
            Utils.Debug.Log.Info("AUTH", "[Agent.Init] HTTP routes registered");
            
            Net.Tcp.Instance.Content.Add.Register(typeof(Client), OnNetAddClient);
            Utils.Debug.Log.Info("AUTH", "[Agent.Init] OnNetAddClient registered to Tcp.Content.Add");
            
            Net.Tcp.Instance.Content.Remove.Register(typeof(Client), OnNetRemoveClient);
            Utils.Debug.Log.Info("AUTH", "[Agent.Init] OnNetRemoveClient registered to Tcp.Content.Remove");
            
            Logic.Agent.Instance.Content.Add.Register(typeof(Logic.Player), OnBundlePlayer);
            Utils.Debug.Log.Info("AUTH", "[Agent.Init] OnBundlePlayer registered");
            
            Register.Init();
            Utils.Debug.Log.Info("AUTH", "[Agent.Init] Register.Init() completed");
            Utils.Debug.Log.Info("AUTH", "[Agent.Init] Authentication initialization complete");
        }
        
        private async void OnLanguageDetect(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            string clientIp = context.Request.RemoteEndPoint.Address.ToString();
            
            Logic.Text.Languages detectedLanguage = DetectLanguageByIp(clientIp);
            
            var response = new
            {
                language = detectedLanguage.ToString()
            };

            await Net.Http.Instance.SendJson(context.Response, response);
        }

        private Logic.Text.Languages DetectLanguageByIp(string ip)
        {
            return Logic.Text.Languages.ChineseSimplified;
        }

        private async void OnIps(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var language = Net.Http.Instance.GetLanguage(context);

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
                servers = serverList,
                ui = new
                {
                    name = "Start",
                    data = new
                    {
                        title = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.StartTitle, language),
                        tip = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.StartTip, language),
                        loginButton = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.StartLogin, language),
                        footer = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.StartFooter, language),
                        accountIdPlaceholder = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.AccountIdPlaceholder, language),
                        accountPasswordPlaceholder = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.AccountPasswordPlaceholder, language),
                        accountNotePlaceholder = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.AccountNotePlaceholder, language),
                        errorAccountEmpty = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.ErrorAccountEmpty, language),
                        errorAccountFormat = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.ErrorAccountFormat, language),
                        errorPasswordEmpty = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.ErrorPasswordEmpty, language),
                        errorPasswordFormat = Domain.Text.Agent.Instance.Get(Logic.Text.Labels.ErrorPasswordFormat, language)
                    }
                }
            };

            await Net.Http.Instance.SendJson(context.Response, response);
        }

        
        private async void OnDevice(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            await Device.ProcessRegister(context);
        }
        
        private async void OnConnect(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var request = context.Request;

            var serverInfo = new
            {
                status = DetermineServerStatus(),
                version = string.Join(".", Logic.Config.Agent.Instance.ClientVersion),
                port = 19881
            };

            await Net.Http.Instance.SendJson(context.Response, serverInfo);
        }
        
        private string DetermineServerStatus()
        {
            try
            {
                bool isStartupPhase = (DateTime.Now - serverStartTime).TotalSeconds < 60;
                if (isStartupPhase)
                {
                    return "good";
                }

                float cpuUsage = 0;
                float memoryUsageMB = 0;

                try
                {
                    lock (cpuCounterLock)
                    {
                        if ((DateTime.Now - lastCpuSampleTime).TotalSeconds < 5)
                        {
                            cpuUsage = lastCpuUsage;
                        }
                        else
                        {
                            if (cpuCounter == null)
                            {
                                cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                                cpuCounter.NextValue();
                                System.Threading.Thread.Sleep(1000);
                                lastCpuUsage = cpuCounter.NextValue();
                            }
                            else
                            {
                                cpuCounter.NextValue();
                                System.Threading.Thread.Sleep(1000);
                                lastCpuUsage = cpuCounter.NextValue();
                            }
                            
                            lastCpuSampleTime = DateTime.Now;
                            cpuUsage = lastCpuUsage;
                        }
                    }
                }
                catch
                {
                    cpuUsage = 0;
                }

                try
                {
                    memoryUsageMB = Utils.Debug.Performance.GetMemoryUsageMB();
                }
                catch
                {
                    memoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024.0f);
                }

                int connectedClients = Net.Tcp.Instance.Content.Count<Client>();

                double mainLoopFreq = Domain.Manager.GetMainLoopFrequency();
                long maxUpdateTime = Domain.Manager.GetMaxUpdateDuration();
                long slowUpdates = Domain.Manager.GetSlowUpdateCount();

                string status;
                bool hasPerformanceIssues = slowUpdates > 0 || maxUpdateTime > 200 || mainLoopFreq > 5000;
                
                if (cpuUsage > 85 || memoryUsageMB > 3500 || connectedClients > 500 || hasPerformanceIssues)
                    status = "busy";
                else if (cpuUsage > 60 || memoryUsageMB > 2500 || connectedClients > 300 || maxUpdateTime > 100)
                    status = "normal";
                else
                    status = "good";

                return status;
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("AUTH", $"[ServerStatus] 异常: {ex.Message}", ex);
                return "good";
            }
        }
        
        #region Network Event Handlers
        private void OnBundlePlayer(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[1];
            player.monitor.Register(Logic.Life.Event.Die, OnPlayerDie);
        }

        private void OnPlayerDie(params object[] args)
        {
            Logic.Player player = (Logic.Player)args[0];
            Logic.Life dyingLife = (Logic.Life)args[1];
            
            if (player == dyingLife)
            {
                Death.Do(player);
            }
        }

        private void OnNetAddClient(params object[] args)
        {
            Client client = (Client)args[1];
            Utils.Debug.Log.Info("AUTH", $"[OnNetAddClient] New client connected: {client.Name}");
            client.monitor.Register(Client.Event.Login, OnLogin);
            client.monitor.Register(Client.Event.InitializeRandom, OnInitializeRandom);
            Utils.Debug.Log.Info("AUTH", $"[OnNetAddClient] Login and InitializeRandom handlers registered for {client.Name}");
        }

        private void OnNetRemoveClient(params object[] args)
        {
            Client client = (Client)args[1];
            Utils.Debug.Log.Info("AUTH", $"[OnNetRemoveClient] Client disconnecting: {client.Name}");
            if (client.Player != null)
            {
                Utils.Debug.Log.Info("AUTH", $"[OnNetRemoveClient] Logging out player: {client.Player.Id}");
                Logout.Do(client.Player);
            }
            
            client.Socket.Close();
            client.monitor.Unregister(Client.Event.Login, OnLogin);
            client.monitor.Unregister(Client.Event.InitializeRandom, OnInitializeRandom);
            Utils.Debug.Log.Info("AUTH", $"[OnNetRemoveClient] Client cleanup complete");
        }

        private void OnLogin(params object[] args)
        {
            Utils.Debug.Log.Info("AUTH", $"[OnLogin] Event received, args count: {args.Length}");
            var loginStartTime = DateTime.Now;
            Client client = (Client)args[0];
            string id = (string)args[1];
            string pw = (string)args[2];
            string device = (string)args[3];
            string version = (string)args[4];
            string platform = (string)args[5];
            string language = (string)args[6];
            Utils.Debug.Log.Info("AUTH", $"[OnLogin] Processing login for id={id}, version={version}, platform={platform}, language={language}");
            Login.Do(client, device, id, pw, version, platform, language, loginStartTime);
            Utils.Debug.Log.Info("AUTH", $"[OnLogin] Login.Do completed for id={id}");
        }

        private void OnInitializeRandom(params object[] args)
        {
            Client client = (Client)args[0];
            Register.Reset(client);
        }


        #endregion
    }
}
