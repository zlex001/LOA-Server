namespace Utils.Debug
{
    public class SnapshotData
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
        public float CpuUsage { get; set; }
        public float MemoryUsageMB { get; set; }
        public float NetworkUsageKBps { get; set; }
        public string Tag { get; set; }

        public override bool Equals(object obj)
        {
            return obj is SnapshotData other &&
                   Message == other.Message &&
                   StackTrace == other.StackTrace &&
                   Source == other.Source;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Message, StackTrace, Source);
        }
    }

    public static class Snapshot
    {
        private static readonly HashSet<SnapshotData> _history = new();
        private static readonly object _lock = new();

        public static void Capture(string tag = "Manual", string message = null)
        {
            var snapshot = new SnapshotData
            {
                Timestamp = DateTime.Now,
                Message = message ?? "手动触发调试快照",
                StackTrace = Environment.StackTrace,
                Source = "ManualCall",
                CpuUsage = Performance.GetCpuUsage(),
                MemoryUsageMB = Performance.GetMemoryUsageMB(),
                NetworkUsageKBps = Performance.GetNetworkUsageKBps(),
                Tag = tag
            };

            lock (_lock)
            {
                if (!_history.Contains(snapshot))
                {
                    _history.Add(snapshot);
                }
            }
        }

        public static void CaptureException(Exception e, string source = null)
        {
            var snapshot = new SnapshotData
            {
                Timestamp = DateTime.Now,
                Message = e.Message,
                StackTrace = e.StackTrace,
                Source = source ?? e.TargetSite?.ToString(),
                CpuUsage = Performance.GetCpuUsage(),
                MemoryUsageMB = Performance.GetMemoryUsageMB(),
                NetworkUsageKBps = Performance.GetNetworkUsageKBps(),
                Tag = "Exception"
            };

            lock (_lock)
            {
                if (!_history.Contains(snapshot))
                {
                    _history.Add(snapshot);
                }
            }
        }

        public static List<SnapshotData> GetSnapshots()
        {
            lock (_lock)
            {
                return _history.ToList();
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _history.Clear();
            }
        }
    }
}




