using System;
using System.IO;
using System.Threading;

namespace Domain
{
    public class CrashGuard
    {
        private static CrashGuard instance;
        public static CrashGuard Instance { get { if (instance == null) { instance = new CrashGuard(); } return instance; } }

        private const string HeartbeatFile = "heartbeat.txt";
        private const string LastCrashFile = "last_crash.txt";
        private Timer heartbeatTimer;
        private DateTime lastHeartbeat;
        private bool isRunning = false;
        private string heartbeatPath;
        private string lastCrashPath;

        public void Init()
        {
            if (Logic.Agent.Instance.IsDevelopment)
            {
                return;
            }

            try
            {
                var logsDir = Utils.Paths.Logs;
                Directory.CreateDirectory(logsDir);
                Directory.CreateDirectory(Utils.Paths.CrashLogs);

                heartbeatPath = System.IO.Path.Combine(logsDir, HeartbeatFile);
                lastCrashPath = System.IO.Path.Combine(Utils.Paths.CrashLogs, LastCrashFile);

                CheckPreviousSession();

                isRunning = true;
                lastHeartbeat = DateTime.Now;
                
                heartbeatTimer = new Timer(OnHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("CRASH", $"Init failed: {ex.Message}");
            }
        }

        private void CheckPreviousSession()
        {
            try
            {
                if (File.Exists(heartbeatPath))
                {
                    var content = File.ReadAllText(heartbeatPath);
                    var lines = content.Split('\n');
                    
                    if (lines.Length >= 2)
                    {
                        var status = lines[0].Trim();
                        var timestampStr = lines[1].Trim();

                        if (status == "RUNNING" && DateTime.TryParse(timestampStr, out var lastTime))
                        {
                            var elapsed = DateTime.Now - lastTime;
                            
                            Utils.Debug.Log.Fatal("*** ABNORMAL SHUTDOWN DETECTED ***");
                            Utils.Debug.Log.Fatal($"Last heartbeat: {lastTime:yyyy-MM-dd HH:mm:ss} ({elapsed.TotalSeconds:F0} seconds ago)");

                            SaveLastCrashInfo(lastTime);
                            
                            TryRecoverCrashLog(lastTime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("CRASH", $"CheckPreviousSession failed: {ex.Message}");
            }
        }

        private void SaveLastCrashInfo(DateTime crashTime)
        {
            try
            {
                var info = $"ABNORMAL_SHUTDOWN\n{crashTime:yyyy-MM-dd HH:mm:ss}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                File.WriteAllText(lastCrashPath, info);
            }
            catch { }
        }

        private void TryRecoverCrashLog(DateTime crashTime)
        {
            try
            {
                var serverLogsDir = Utils.Paths.ServerLogs;
                if (!Directory.Exists(serverLogsDir))
                    return;

                var logFiles = Directory.GetFiles(serverLogsDir, "server_*.log");
                if (logFiles.Length == 0)
                    return;

                var latestLog = logFiles[logFiles.Length - 1];
                
                string logContent;
                using (var fileStream = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8))
                {
                    logContent = reader.ReadToEnd();
                }
                
                var lines = logContent.Split('\n');
                var lastLines = lines.Length > 100 ? lines[(lines.Length - 100)..] : lines;

                var crashReportPath = System.IO.Path.Combine(Utils.Paths.CrashLogs, $"abnormal_shutdown_{crashTime:yyyyMMdd_HHmmss}.log");
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== ABNORMAL SHUTDOWN DETECTED ===");
                report.AppendLine($"Crash Time: {crashTime:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Detected At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Time Elapsed: {(DateTime.Now - crashTime).TotalSeconds:F0} seconds");
                report.AppendLine();
                report.AppendLine("=== LAST 100 LOG LINES ===");
                report.AppendLine(string.Join("\n", lastLines));

                File.WriteAllText(crashReportPath, report.ToString(), System.Text.Encoding.UTF8);
                
                Utils.Debug.Log.Info("CRASH", $"Crash report saved: {crashReportPath}");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("CRASH", $"TryRecoverCrashLog failed: {ex.Message}");
            }
        }

        private void OnHeartbeat(object state)
        {
            if (!isRunning) return;

            try
            {
                lastHeartbeat = DateTime.Now;
                var content = $"RUNNING\n{lastHeartbeat:yyyy-MM-dd HH:mm:ss}\n{Logic.Agent.Instance.Content.Gets<Logic.Player>().Count()} players";
                File.WriteAllText(heartbeatPath, content);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("CRASH", $"Heartbeat failed: {ex.Message}");
            }
        }

        public void MarkNormalShutdown()
        {
            try
            {
                isRunning = false;
                heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                if (!string.IsNullOrEmpty(heartbeatPath))
                {
                    var content = $"NORMAL_SHUTDOWN\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    File.WriteAllText(heartbeatPath, content);
                    Utils.Debug.Log.Info("CRASH", "Marked normal shutdown");
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("CRASH", $"MarkNormalShutdown failed: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            isRunning = false;
            heartbeatTimer?.Dispose();
            heartbeatTimer = null;
        }
    }
}


