using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Daemon
{
    class Program
    {
        private static void PrintVersionInfo(string baseDir)
        {
            try
            {
                string logicDllPath = Path.Combine(baseDir, "Logic.dll");
                if (File.Exists(logicDllPath))
                {
                    var assembly = System.Reflection.Assembly.LoadFrom(logicDllPath);
                    var agentType = assembly.GetType("Logic.Config.Agent");
                    if (agentType != null)
                    {
                        var instanceProp = agentType.GetProperty("Instance");
                        var instance = instanceProp?.GetValue(null);
                        if (instance != null)
                        {
                            var versionField = agentType.GetField("Version");
                            var clientVersionField = agentType.GetField("ClientVersion");
                            
                            var version = versionField?.GetValue(instance) as int[];
                            var clientVersion = clientVersionField?.GetValue(instance) as int[];
                            
                            if (version != null && clientVersion != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Server Version: {string.Join(".", version)}");
                                Console.WriteLine($"Client Version: {string.Join(".", clientVersion)}");
                                Console.ResetColor();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[警告] 无法读取版本信息: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void Main(string[] args)
        {
            string targetExeName = "world-service.exe";
            string[] targetArgs = args.Length > 0 ? args : null;
            int restartDelaySeconds = 5;
            int maxConsecutiveFailures = 10;
            int healthCheckIntervalSeconds = 30;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetExe = Path.Combine(baseDir, targetExeName);

            if (!File.Exists(targetExe))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Daemon] 错误：找不到目标程序 {targetExe}");
                Console.ResetColor();
                Console.WriteLine($"[Daemon] 当前目录：{baseDir}");
                Console.WriteLine("\n按任意键退出...");
                Console.ReadKey();
                return;
            }

            int consecutiveFailures = 0;
            int restartCount = 0;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("========================================");
            Console.WriteLine("         守护进程已启动");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine($"目标程序：{targetExeName}");
            Console.WriteLine($"程序路径：{targetExe}");
            Console.WriteLine($"启动参数：{(targetArgs != null && targetArgs.Length > 0 ? string.Join(" ", targetArgs) : "无")}");
            Console.WriteLine($"健康检查：每{healthCheckIntervalSeconds}秒");
            Console.WriteLine($"重启延迟：{restartDelaySeconds}秒");
            Console.WriteLine($"失败阈值：{maxConsecutiveFailures}次");
            Console.WriteLine("========================================");
            
            PrintVersionInfo(baseDir);
            
            Console.WriteLine();

            while (true)
            {
                Process process = null;
                try
                {
                    DateTime startTime = DateTime.Now;
                    restartCount++;

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n[{startTime:yyyy-MM-dd HH:mm:ss}] 第 {restartCount} 次启动 {targetExeName}");
                    Console.ResetColor();

                    process = new Process();
                    process.StartInfo.FileName = targetExe;
                    process.StartInfo.Arguments = targetArgs != null && targetArgs.Length > 0 ? string.Join(" ", targetArgs) : "";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.WorkingDirectory = baseDir;

                    bool started = process.Start();
                    if (!started)
                    {
                        throw new Exception("进程启动失败");
                    }

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 进程ID：{process.Id}");

                    DateTime lastHealthCheck = DateTime.Now;
                    while (!process.HasExited)
                    {
                        Thread.Sleep(1000);
                        
                        if ((DateTime.Now - lastHealthCheck).TotalSeconds >= healthCheckIntervalSeconds)
                        {
                            lastHealthCheck = DateTime.Now;
                            try
                            {
                                string heartbeatFile = Path.Combine(baseDir, "Library", "Logs", "heartbeat.txt");
                                if (File.Exists(heartbeatFile))
                                {
                                    var lines = File.ReadAllLines(heartbeatFile);
                                    if (lines.Length >= 2)
                                    {
                                        var status = lines[0].Trim();
                                        if (DateTime.TryParse(lines[1].Trim(), out var lastBeat))
                                        {
                                            var elapsed = (DateTime.Now - lastBeat).TotalSeconds;
                                            if (elapsed > 60)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] ⚠️  进程无响应（心跳超时{elapsed:F0}秒），强制终止...");
                                                Console.ResetColor();
                                                process.Kill();
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    process.WaitForExit();

                    DateTime exitTime = DateTime.Now;
                    TimeSpan runTime = exitTime - startTime;
                    int exitCode = process.ExitCode;

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[{exitTime:yyyy-MM-dd HH:mm:ss}] 程序已退出");
                    Console.ResetColor();
                    Console.WriteLine($"  运行时长：{runTime.TotalHours:F2}小时 ({runTime.TotalMinutes:F1}分钟)");
                    Console.WriteLine($"  退出代码：{exitCode}");

                    if (runTime.TotalSeconds < 60)
                    {
                        consecutiveFailures++;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ⚠️  检测到快速失败！连续次数：{consecutiveFailures}/{maxConsecutiveFailures}");
                        Console.ResetColor();

                        if (consecutiveFailures >= maxConsecutiveFailures)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n╔════════════════════════════════════════╗");
                            Console.WriteLine($"║  错误：连续失败 {maxConsecutiveFailures} 次，守护进程终止  ║");
                            Console.WriteLine($"╚════════════════════════════════════════╝");
                            Console.ResetColor();
                            Console.WriteLine("\n请检查程序配置或日志文件");
                            Console.WriteLine("按任意键退出...");
                            Console.ReadKey();
                            break;
                        }
                    }
                    else
                    {
                        consecutiveFailures = 0;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n等待 {restartDelaySeconds} 秒后自动重启...");
                    Console.ResetColor();
                    Thread.Sleep(restartDelaySeconds * 1000);
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[异常] 启动过程中出现错误：{ex.Message}");
                    Console.WriteLine($"连续失败次数：{consecutiveFailures}/{maxConsecutiveFailures}");
                    Console.ResetColor();

                    if (consecutiveFailures >= maxConsecutiveFailures)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n连续异常 {maxConsecutiveFailures} 次，守护进程终止");
                        Console.ResetColor();
                        Console.WriteLine("按任意键退出...");
                        Console.ReadKey();
                        break;
                    }

                    Console.WriteLine($"等待 {restartDelaySeconds} 秒后重试...");
                    Thread.Sleep(restartDelaySeconds * 1000);
                }
                finally
                {
                    try { process?.Kill(); } catch { }
                    try { process?.Dispose(); } catch { }
                }
            }
        }
    }
}

