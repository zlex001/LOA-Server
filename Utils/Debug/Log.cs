using System.Collections.Concurrent;

namespace Utils.Debug
{
    public static class Log
    {
        public enum Level
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            Fatal = 3
        }

        public class Entry
        {
            public DateTime Time { get; set; }
            public Level Level { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
            public object Details { get; set; }
        }

        private static readonly ConcurrentQueue<Entry> _buffer = new();
        private static readonly int _maxBufferSize = 1000;
        private static readonly Dictionary<string, bool> _categoryEnabled = new()
        {
            // === Core Systems (Enabled by default) ===
            ["LOGIC"] = true,           // Logic layer initialization and core logic
            ["AUTH"] = true,            // Authentication and login system
            ["LOGOUT"] = true,          // Player logout process
            ["DATABASE"] = true,        // Database operations
            ["MIGRATION"] = true,       // Data migration
            
            // === Management & Monitoring (Enabled by default) ===
            ["ADMIN"] = true,           // Admin backend
            ["ANALYTICS"] = true,       // Data analytics
            ["CRASH"] = true,           // Crash recovery and heartbeat
            ["SAFETY_NET"] = true,      // Safety net for error handling
            ["HOT"] = true,             // Hot update system
            
            // === Business Features (Enabled by default) ===
            ["DISPLAY"] = true,         // Display system
            ["PLAYER"] = true,          // Player system
            ["MERGE"] = true,           // Server merge
            ["SHUTDOWN"] = true,        // Server shutdown process
            ["REPORT"] = true,          // Daily operation reports
            
            // === Tools & Config (Enabled by default) ===
            ["CONFIG"] = true,          // Configuration loading
            ["BINARY"] = true,          // Serialization
            ["JSON"] = true,            // JSON conversion
            ["NET"] = true,             // Network communication
            ["DESIGN"] = true,          // Design data generation
            ["BEHAVIOR_TREE"] = true,   // Behavior tree
            ["STATE"] = true,           // State machine error handling
            ["SCHEDULER"] = true,       // Scheduler exception handling
            
            // === Verbose Debug (Disabled by default) ===
            ["TCP"] = false,            // TCP protocol send/receive logs
            
        };

        public static bool IsDevelopment { get; set; } = false;
        private static bool _fileLoggingEnabled = true;
        public static bool EnableFileLogging => !IsDevelopment && _fileLoggingEnabled;

        private static StreamWriter _logFileWriter;
        private static string _currentLogFile;
        private static readonly object _fileLock = new();
        private static bool _fileInitialized = false;

        public static void Info(string category, string message, object details = null)
        {
            LogInternal(Level.Info, category, message, details);
        }

        public static void Warning(string category, string message, object details = null)
        {
            LogInternal(Level.Warning, category, message, details);
        }

        public static void Error(string category, string message, object details = null)
        {
            LogInternal(Level.Error, category, message, details);
        }

        public static void Fatal(string message, object details = null)
        {
            LogInternal(Level.Fatal, "FATAL", message, details);
        }

        private static void InitializeLogFile()
        {
            if (!EnableFileLogging) return;

            try
            {
                var logDir = Paths.ServerLogs;
                Directory.CreateDirectory(logDir);

                _currentLogFile = System.IO.Path.Combine(logDir, $"server_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                var fileStream = new FileStream(
                    _currentLogFile,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read
                );

                _logFileWriter = new StreamWriter(fileStream, System.Text.Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
            catch
            {
                _fileLoggingEnabled = false;
            }
        }

        private static void WriteToFile(Entry entry)
        {
            try
            {
                lock (_fileLock)
                {
                    if (!_fileInitialized)
                    {
                        InitializeLogFile();
                        _fileInitialized = true;
                    }

                    if (_logFileWriter == null) return;

                    var logLine = $"[{entry.Time:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}] {entry.Message}";

                    if (entry.Details != null)
                    {
                        try
                        {
                            var detailsJson = Newtonsoft.Json.JsonConvert.SerializeObject(entry.Details);
                            logLine += $" | {detailsJson}";
                        }
                        catch
                        {
                            logLine += $" | {entry.Details}";
                        }
                    }

                    _logFileWriter.WriteLine(logLine);
                }
            }
            catch
            {
                // ��Ĭʧ�ܣ�������־ϵͳӰ��������
            }
        }

        private static void LogInternal(Level level, string category, string message, object details = null)
        {
            if (level != Level.Fatal && !IsCategoryEnabled(category))
                return;

            var entry = new Entry
            {
                Time = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                Details = details
            };

            _buffer.Enqueue(entry);
            while (_buffer.Count > _maxBufferSize)
                _buffer.TryDequeue(out _);

            if (level == Level.Fatal || IsDevelopment)
            {
                var prefix = level == Level.Fatal ? "[FATAL] " : $"[{level}] [{category}] ";
                System.Console.WriteLine(prefix + message);
            }

            if (EnableFileLogging)
            {
                WriteToFile(entry);
            }
        }

        public static bool IsCategoryEnabled(string category)
        {
            lock (_categoryEnabled)
            {
                return _categoryEnabled.GetValueOrDefault(category, true);
            }
        }

        public static void SetCategoryEnabled(string category, bool enabled)
        {
            lock (_categoryEnabled)
            {
                _categoryEnabled[category] = enabled;
            }
        }

        public static Dictionary<string, bool> GetCategories()
        {
            lock (_categoryEnabled)
            {
                return new Dictionary<string, bool>(_categoryEnabled);
            }
        }

        public static List<Entry> GetLogs(string category = null, string keyword = null, int limit = 100, DateTime? startTime = null, DateTime? endTime = null, Level? level = null)
        {
            var logs = _buffer.ToArray().AsEnumerable();

            if (!string.IsNullOrEmpty(category))
                logs = logs.Where(e => e.Category == category);

            if (!string.IsNullOrEmpty(keyword))
                logs = logs.Where(e => e.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (startTime.HasValue)
                logs = logs.Where(e => e.Time >= startTime.Value);

            if (endTime.HasValue)
                logs = logs.Where(e => e.Time <= endTime.Value);

            if (level.HasValue)
                logs = logs.Where(e => e.Level == level.Value);

            return logs.OrderByDescending(e => e.Time).Take(limit).ToList();
        }

        public static int GetTotalCount()
        {
            return _buffer.Count;
        }

        public static void Shutdown()
        {
            lock (_fileLock)
            {
                _logFileWriter?.Flush();
                _logFileWriter?.Close();
                _logFileWriter = null;
            }
        }

        public static List<FileInfo> GetLogFiles()
        {
            try
            {
                var logDir = Paths.ServerLogs;

                if (!Directory.Exists(logDir))
                    return new List<FileInfo>();

                return new DirectoryInfo(logDir)
                    .GetFiles("server_*.log")
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
            }
            catch
            {
                return new List<FileInfo>();
            }
        }
    }
}