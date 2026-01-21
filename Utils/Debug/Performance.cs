using System.Collections.Concurrent;
using System.Diagnostics;

namespace Utils.Debug
{
    public static class Performance
    {
        public class Snapshot
        {
            public DateTime Timestamp { get; set; }
            public double Frequency { get; set; }
            public long MaxDuration { get; set; }
            public long SlowUpdates { get; set; }
            public double AvgDuration { get; set; }
            public long MinDuration { get; set; }
        }

        public class SlowOperation
        {
            public DateTime Time { get; set; }
            public long Duration { get; set; }
            public string Type { get; set; }
            public string Context { get; set; }
            public string StackTrace { get; set; }
        }

        private static readonly ConcurrentQueue<Snapshot> _history = new();
        private static readonly ConcurrentQueue<SlowOperation> _slowOperations = new();
        private static readonly int _maxHistorySize = 120;
        private static readonly int _maxSlowOperationsSize = 100;

        private static Snapshot _latestSnapshot;
        private static readonly object _snapshotLock = new();

        public static int SlowOperationThreshold { get; set; } = 100;

        public static void RecordSnapshot(double frequency, long maxDuration, long slowUpdates, double avgDuration = 0, long minDuration = 0)
        {
            var snapshot = new Snapshot
            {
                Timestamp = DateTime.UtcNow,
                Frequency = frequency,
                MaxDuration = maxDuration,
                SlowUpdates = slowUpdates,
                AvgDuration = avgDuration,
                MinDuration = minDuration
            };

            lock (_snapshotLock)
            {
                _latestSnapshot = snapshot;
            }

            _history.Enqueue(snapshot);
            while (_history.Count > _maxHistorySize)
                _history.TryDequeue(out _);
        }

        public static void RecordSlowOperation(long duration, string type = "unknown", string context = "", string stackTrace = null)
        {
            var operation = new SlowOperation
            {
                Time = DateTime.UtcNow,
                Duration = duration,
                Type = type,
                Context = context,
                StackTrace = stackTrace
            };

            _slowOperations.Enqueue(operation);
            while (_slowOperations.Count > _maxSlowOperationsSize)
                _slowOperations.TryDequeue(out _);

            if (duration > SlowOperationThreshold)
            {
                Log.Error("PERFORMANCE", $"发现慢操作: 耗时{duration}ms ({type})");
            }
        }

        public static float GetCpuUsage()
        {
            try
            {
                using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue();
                Thread.Sleep(100);
                return cpuCounter.NextValue();
            }
            catch
            {
                return 0;
            }
        }

        public static float GetMemoryUsageMB()
        {
            return (float)(Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0));
        }

        public static float GetNetworkUsageKBps()
        {
            return 0;
        }

        public static Snapshot GetRealtime()
        {
            lock (_snapshotLock)
            {
                return _latestSnapshot;
            }
        }

        public static List<Snapshot> GetHistory(int minutes = 60)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
            return _history.Where(s => s.Timestamp >= cutoff).OrderBy(s => s.Timestamp).ToList();
        }

        public static List<SlowOperation> GetSlowOperations(int limit = 50, int threshold = 100, DateTime? startTime = null, DateTime? endTime = null)
        {
            var operations = _slowOperations.ToArray().AsEnumerable();

            operations = operations.Where(o => o.Duration >= threshold);

            if (startTime.HasValue)
                operations = operations.Where(o => o.Time >= startTime.Value);

            if (endTime.HasValue)
                operations = operations.Where(o => o.Time <= endTime.Value);

            return operations.OrderByDescending(o => o.Time).Take(limit).ToList();
        }

        public static string GetHealthStatus()
        {
            var snapshot = GetRealtime();
            if (snapshot == null) return "unknown";

            if (snapshot.Frequency > 10_000_000 && snapshot.MaxDuration < 50)
                return "good";
            
            if (snapshot.Frequency > 5_000_000 && snapshot.MaxDuration < 100)
                return "warning";

            return "critical";
        }
    }
}






