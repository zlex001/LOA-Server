using System.Net;
using Newtonsoft.Json;

namespace Logic.Administrator
{
    public class PerformanceMonitor
    {
        private static PerformanceMonitor instance;
        public static PerformanceMonitor Instance { get { if (instance == null) { instance = new PerformanceMonitor(); } return instance; } }

        public async void OnGetRealtimePerformance(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var snapshot = Utils.Debug.Performance.GetRealtime();
                if (snapshot == null)
                {
                    await Net.Http.Instance.SendError(context.Response, "性能数据尚未初始化", 503);
                    return;
                }

                var result = new
                {
                    timestamp = snapshot.Timestamp.ToString("o"),
                    loopFrequency = snapshot.Frequency,
                    maxDuration = snapshot.MaxDuration,
                    slowUpdates = snapshot.SlowUpdates,
                    avgDuration = snapshot.AvgDuration,
                    minDuration = snapshot.MinDuration,
                    tickRates = new
                    {
                        battle = 100,
                        normal = 300,
                        @default = 1000
                    },
                    health = Utils.Debug.Performance.GetHealthStatus()
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[PerformanceMonitor] GetRealtime Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnGetPerformanceHistory(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var query = context.Request.QueryString;
                string duration = query["duration"] ?? "1h";
                string interval = query["interval"] ?? "30s";

                int minutes = duration switch
                {
                    "15m" => 15,
                    "1h" => 60,
                    "6h" => 360,
                    "24h" => 1440,
                    _ => 60
                };

                var history = Utils.Debug.Performance.GetHistory(minutes);

                var data = history.Select(s => new
                {
                    time = s.Timestamp.ToString("o"),
                    frequency = s.Frequency,
                    maxDuration = s.MaxDuration,
                    slowUpdates = s.SlowUpdates
                }).ToList();

                var summary = new
                {
                    avgFrequency = history.Any() ? history.Average(s => s.Frequency) : 0,
                    peakFrequency = history.Any() ? history.Max(s => s.Frequency) : 0,
                    totalSlowUpdates = history.Sum(s => s.SlowUpdates)
                };

                var result = new
                {
                    data,
                    summary
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[PerformanceMonitor] GetHistory Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnGetSlowOperations(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var query = context.Request.QueryString;
                int limit = int.TryParse(query["limit"], out var l) ? l : 50;
                int threshold = int.TryParse(query["threshold"], out var t) ? t : 100;
                DateTime? startTime = DateTime.TryParse(query["startTime"], out var st) ? st : null;
                DateTime? endTime = DateTime.TryParse(query["endTime"], out var et) ? et : null;

                var operations = Utils.Debug.Performance.GetSlowOperations(limit, threshold, startTime, endTime);

                var result = new
                {
                    operations = operations.Select(op => new
                    {
                        time = op.Time.ToString("o"),
                        duration = op.Duration,
                        type = op.Type,
                        context = op.Context,
                        stackTrace = op.StackTrace
                    }),
                    total = operations.Count,
                    threshold
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[PerformanceMonitor] GetSlowOperations Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }
    }
}

