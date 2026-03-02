using System.Collections.Concurrent;
using System.Net;

namespace Logic
{
    public class ConnectionMonitor
    {
        #region Singleton
        private static ConnectionMonitor instance;
        public static ConnectionMonitor Instance { get { if (instance == null) { instance = new ConnectionMonitor(); } return instance; } }
        #endregion

        #region Enums
        public enum Status
        {
            Connected,
            Failed,
            Disconnected
        }
        #endregion

        #region Connection Record
        public class Record
        {
            public string Id { get; set; }
            public string Ip { get; set; }
            public int Port { get; set; }
            public DateTime ConnectTime { get; set; }
            public Status Status { get; set; }
            public long Duration { get; set; }
            public string PlayerId { get; set; }
            public string PlayerName { get; set; }
            public string Protocol { get; set; } = "TCP";
            public string ClientVersion { get; set; }
            public string FailReason { get; set; }
            public int RetryCount { get; set; }
        }
        #endregion

        #region Fields
        private ConcurrentDictionary<string, Record> records = new ConcurrentDictionary<string, Record>();
        private ConcurrentDictionary<string, Record> activeConnections = new ConcurrentDictionary<string, Record>();
        private int connectionIdCounter = 0;
        private object lockObject = new object();
        #endregion

        #region Statistics
        private int todayTotal = 0;
        private int todaySuccess = 0;
        private int todayFailed = 0;
        private int peakOnline = 0;
        private DateTime peakTime = DateTime.UtcNow;
        private DateTime lastResetDate = DateTime.UtcNow.Date;
        #endregion

        #region Public Methods
        public void Init()
        {
            Basic.Time.Manager.Instance.Scheduler.Repeat(5000, (_) => UpdateStatistics());
            
            Net.Tcp.Instance.Content.Add.Register(typeof(Net.Client), OnNetAddClient);
            Net.Tcp.Instance.Content.Remove.Register(typeof(Net.Client), OnNetRemoveClient);
        }
        
        private void OnNetAddClient(params object[] args)
        {
            Net.Client client = (Net.Client)args[1];
            
            RecordConnection(client.IP, ((System.Net.IPEndPoint)client.Socket.RemoteEndPoint).Port);
            
            client.monitor.Register(Net.Client.Event.Disconnected, OnClientDisconnected);
            client.monitor.Register(Net.Client.Event.PlayerBound, OnClientPlayerBound);
        }
        
        private void OnNetRemoveClient(params object[] args)
        {
            Net.Client client = (Net.Client)args[1];
            client.monitor.Unregister(Net.Client.Event.Disconnected, OnClientDisconnected);
            client.monitor.Unregister(Net.Client.Event.PlayerBound, OnClientPlayerBound);
        }
        
        private void OnClientDisconnected(params object[] args)
        {
            Net.Client client = (Net.Client)args[0];
            if (!string.IsNullOrEmpty(client.ConnectionId))
            {
                RecordDisconnection(client.ConnectionId);
            }
        }
        
        private void OnClientPlayerBound(params object[] args)
        {
            Net.Client client = (Net.Client)args[0];
            global::Data.Player player = (global::Data.Player)args[1];
            if (!string.IsNullOrEmpty(client.ConnectionId))
            {
                UpdateConnection(client.ConnectionId, player);
            }
        }

        private void RecordConnection(IPAddress ip, int port)
        {
            CheckDailyReset();

            string connId = $"conn_{ip}:{port}";
            var record = new Record
            {
                Id = connId,
                Ip = ip.ToString(),
                Port = port,
                ConnectTime = DateTime.UtcNow,
                Status = Status.Connected,
                Duration = 0
            };

            records.TryAdd(connId, record);
            activeConnections.TryAdd(connId, record);

            todayTotal++;
            todaySuccess++;

            CleanOldRecords();
        }

        public void RecordFailure(IPAddress ip, int port, string reason, int retryCount = 0)
        {
            CheckDailyReset();

            string connId = GenerateConnectionId();
            var record = new Record
            {
                Id = connId,
                Ip = ip.ToString(),
                Port = port,
                ConnectTime = DateTime.UtcNow,
                Status = Status.Failed,
                Duration = 0,
                FailReason = reason,
                RetryCount = retryCount
            };

            records.TryAdd(connId, record);

            todayTotal++;
            todayFailed++;

            CleanOldRecords();
        }

        private void UpdateConnection(string connId, global::Data.Player player)
        {
            if (records.TryGetValue(connId, out var record))
            {
                record.PlayerId = player.Id.ToString();
                record.PlayerName = player.Name;
            }
        }

        private void RecordDisconnection(string connId)
        {
            if (activeConnections.TryRemove(connId, out var record))
            {
                record.Status = Status.Disconnected;
                record.Duration = (long)(DateTime.UtcNow - record.ConnectTime).TotalSeconds;
            }
        }

        public List<Record> GetConnections(string statusFilter, string ipFilter, DateTime? startTime, DateTime? endTime, int limit)
        {
            var query = records.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                Status status = statusFilter switch
                {
                    "connected" => Status.Connected,
                    "failed" => Status.Failed,
                    "disconnected" => Status.Disconnected,
                    _ => Status.Connected
                };
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(ipFilter))
            {
                query = query.Where(r => r.Ip.Contains(ipFilter));
            }

            if (startTime.HasValue)
            {
                query = query.Where(r => r.ConnectTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(r => r.ConnectTime <= endTime.Value);
            }

            return query
                .OrderByDescending(r => r.ConnectTime)
                .Take(limit)
                .Select(r => new Record
                {
                    Id = r.Id,
                    Ip = r.Ip,
                    Port = r.Port,
                    ConnectTime = r.ConnectTime,
                    Status = r.Status,
                    Duration = r.Status == Status.Connected
                        ? (long)(DateTime.UtcNow - r.ConnectTime).TotalSeconds
                        : r.Duration,
                    PlayerId = r.PlayerId,
                    PlayerName = r.PlayerName,
                    Protocol = r.Protocol,
                    ClientVersion = r.ClientVersion,
                    FailReason = r.FailReason,
                    RetryCount = r.RetryCount
                })
                .ToList();
        }

        public List<Record> GetFailures(DateTime? startTime, DateTime? endTime, int limit)
        {
            var query = records.Values.Where(r => r.Status == Status.Failed);

            if (startTime.HasValue)
            {
                query = query.Where(r => r.ConnectTime >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(r => r.ConnectTime <= endTime.Value);
            }

            return query
                .OrderByDescending(r => r.ConnectTime)
                .Take(limit)
                .ToList();
        }

        public object GetStatistics()
        {
            CheckDailyReset();

            int currentOnline = activeConnections.Count;
            double successRate = todayTotal > 0 ? (double)todaySuccess / todayTotal * 100 : 0;

            long totalDuration = 0;
            int count = 0;
            foreach (var record in activeConnections.Values)
            {
                totalDuration += (long)(DateTime.UtcNow - record.ConnectTime).TotalSeconds;
                count++;
            }
            long avgDuration = count > 0 ? totalDuration / count : 0;

            return new
            {
                currentOnline = currentOnline,
                todayTotal = todayTotal,
                todaySuccess = todaySuccess,
                todayFailed = todayFailed,
                successRate = Math.Round(successRate, 1),
                avgDuration = avgDuration,
                peakOnline = peakOnline,
                peakTime = peakTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public string GetConnectionIdByIp(IPAddress ip)
        {
            var record = activeConnections.Values.FirstOrDefault(r => r.Ip == ip.ToString());
            return record?.Id;
        }
        #endregion

        #region Private Methods
        private string GenerateConnectionId()
        {
            lock (lockObject)
            {
                connectionIdCounter++;
                return $"conn_{connectionIdCounter}";
            }
        }

        private void CleanOldRecords()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var oldRecords = records.Where(kvp => kvp.Value.ConnectTime < cutoffTime).ToList();

            foreach (var kvp in oldRecords)
            {
                records.TryRemove(kvp.Key, out _);
            }

            if (records.Count > 10000)
            {
                var excess = records.OrderBy(kvp => kvp.Value.ConnectTime)
                    .Take(records.Count - 5000)
                    .ToList();

                foreach (var kvp in excess)
                {
                    records.TryRemove(kvp.Key, out _);
                }
            }
        }

        private void UpdateStatistics()
        {
            int current = activeConnections.Count;
            if (current > peakOnline)
            {
                peakOnline = current;
                peakTime = DateTime.UtcNow;
            }
        }

        private void CheckDailyReset()
        {
            var currentDate = DateTime.UtcNow.Date;
            if (currentDate > lastResetDate)
            {
                todayTotal = 0;
                todaySuccess = 0;
                todayFailed = 0;
                peakOnline = activeConnections.Count;
                peakTime = DateTime.UtcNow;
                lastResetDate = currentDate;
            }
        }
        #endregion
    }
}

