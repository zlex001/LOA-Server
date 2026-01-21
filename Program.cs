using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace WorldService
{
    public class Program
    {
        private static bool isShuttingDown = false;
        private static readonly object shutdownLock = new object();

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        [DllImport("kernel32.dll")]
        private static extern uint SetErrorMode(uint uMode);

        private const uint SEM_FAILCRITICALERRORS = 0x0001;
        private const uint SEM_NOGPFAULTERRORBOX = 0x0002;
        private const uint SEM_NOOPENFILEERRORBOX = 0x8000;

        private delegate bool HandlerRoutine(CtrlTypes ctrlType);

        private enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static void DisableWindowsErrorReporting()
        {
            SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX);
        }

        static void Main(string[] args)
        {
            if (args.Contains("--convert-xlsx") || args.Contains("-c"))
            {
                Console.WriteLine("Converting xlsx files to csv...");
                Utils.ConvertXlsxToCsv.ConvertAllDesignData();
                Console.WriteLine("Conversion completed!");
                return;
            }

            var isDevelopment = ParseIsDevelopment(args);
            var serverId = ParseServerId(args);
            
            Utils.Debug.Log.IsDevelopment = isDevelopment;
            
            if (isDevelopment)
            {
                InitializeServer(isDevelopment, serverId);
            }
            else
            {
                try
                {
                    InitializeServer(isDevelopment, serverId);
                }
                catch (Exception ex)
                {
                    ShowStartupFailure(ex);
                    Environment.Exit(1);
                }
            }
        }

        private static void InitializeServer(bool isDevelopment, string serverId)
        {
            DisableWindowsErrorReporting();
            SetConsoleCtrlHandler(ConsoleCtrlHandler, true);
            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            Logic.Agent.Instance.IsDevelopment = isDevelopment;
            Logic.Agent.Instance.ServerId = serverId;
            
            // 开发环境自动启用经济监控
            if (isDevelopment)
            {
                Domain.EconomyMonitor.Enabled = true;
            }
            
            Logic.Agent.Instance.Init();
            Domain.CrashGuard.Instance.Init();
            
            Net.Manager.Instance.Init();
            Domain.Manager.Instance.Init();

            Thread keyboardThread = new Thread(KeyboardListener);
            keyboardThread.IsBackground = true;
            keyboardThread.Start();
            if (isDevelopment)
            {
                Utils.Debug.Log.Info("SERVER", "========================================");
                Utils.Debug.Log.Info("SERVER", "  服务器启动完毕，可以登录测试");
                Utils.Debug.Log.Info("SERVER", "  GM快捷键: F1=农夫 F2=矿工 F3=猎人 F4=商人");
                Utils.Debug.Log.Info("SERVER", "  调试功能: F5=行为树调试 F11=TCP日志开关");
                Utils.Debug.Log.Info("SERVER", "  数据导出: F10=生物属性CSV");
                Utils.Debug.Log.Info("SERVER", "  剧情测试: F6=偷窃 F7=刀法 F8=剑术 F9=驯兽");
                Utils.Debug.Log.Info("SERVER", "  系统操作: Delete=清屏 ESC=退出");
                Utils.Debug.Log.Info("SERVER", "========================================");
            }
            
            Logic.Agent.Instance.Open = true;            
         
        }

        private static void ShowStartupFailure(Exception ex)
        {

            System.Console.WriteLine();
            System.Console.WriteLine("========================================");
            System.Console.WriteLine("   SERVER START FAILED");
            System.Console.WriteLine("   服务器启动失败");
            System.Console.WriteLine("========================================");
            System.Console.WriteLine();
            System.Console.WriteLine($"异常类型: {ex.GetType().Name}");
            System.Console.WriteLine($"异常信息: {ex.Message}");
            System.Console.WriteLine();
            System.Console.WriteLine("详细诊断信息:");
            
            try
            {
                var logFiles = Utils.Debug.Log.GetLogFiles();
                if (logFiles.Count > 0)
                {
                    var latestLog = logFiles.First();
                    System.Console.WriteLine($"  日志文件: {latestLog.FullName}");
                    System.Console.WriteLine($"  文件大小: {FormatFileSize(latestLog.Length)}");
                    System.Console.WriteLine($"  更新时间: {latestLog.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    System.Console.WriteLine("  未找到日志文件");
                }
            }
            catch
            {
                System.Console.WriteLine("  无法读取日志文件信息");
            }
            
            System.Console.WriteLine();
            System.Console.WriteLine("建议排查:");
            System.Console.WriteLine("  1. 查看日志文件了解详细错误");
            System.Console.WriteLine("  2. 检查配置文件 (Config/*.xlsx)");
            System.Console.WriteLine("  3. 检查数据库连接");
            System.Console.WriteLine("  4. 检查端口占用情况");
            System.Console.WriteLine("========================================");
            System.Console.WriteLine();
            System.Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
            return $"{bytes / (1024.0 * 1024):F2} MB";
        }

        private static bool ParseIsDevelopment(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--dev" || args[i] == "-d")
                {
                    return true;
                }
                if (args[i] == "--prod" || args[i] == "-p")
                {
                    return false;
                }
            }

            var configMode = System.Configuration.ConfigurationManager.AppSettings["IsDevelopment"];
            if (!string.IsNullOrEmpty(configMode))
            {
                if (bool.TryParse(configMode, out var isDev))
                {
                    return isDev;
                }
            }

            return true;
        }

        private static string ParseServerId(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--server" && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static bool ConsoleCtrlHandler(CtrlTypes ctrlType)
        {
            string eventName = ctrlType switch
            {
                CtrlTypes.CTRL_C_EVENT => "Ctrl+C",
                CtrlTypes.CTRL_BREAK_EVENT => "Ctrl+Break",
                CtrlTypes.CTRL_CLOSE_EVENT => "控制台关闭",
                CtrlTypes.CTRL_LOGOFF_EVENT => "用户注销",
                CtrlTypes.CTRL_SHUTDOWN_EVENT => "系统关机",
                _ => "未知事件"
            };

            Shutdown($"{eventName}事件");
            Thread.Sleep(3000);
            return true;
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Shutdown("Ctrl+C");
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            if (!isShuttingDown)
            {
                Utils.Debug.Log.Fatal("OnProcessExit触发，开始紧急保存数据...");
                EmergencySave();
                Domain.CrashGuard.Instance.MarkNormalShutdown();
                Utils.Debug.Log.Shutdown();
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var timestamp = DateTime.Now;
            var crashLogDir = Utils.Paths.CrashLogs;
            var crashLogFile = System.IO.Path.Combine(crashLogDir, $"crash_{timestamp:yyyyMMdd_HHmmss}.log");

            try
            {
                Utils.Debug.Log.Fatal("捕获未处理异常，开始紧急保存数据...");
                Utils.Debug.Log.Fatal($"异常信息: {e.ExceptionObject}");

                Directory.CreateDirectory(crashLogDir);

                var basicInfo = $"=== CRASH REPORT ===\nTime: {timestamp:yyyy-MM-dd HH:mm:ss.fff}\nIsTerminating: {e.IsTerminating}\n\n=== EXCEPTION ===\n{e.ExceptionObject?.ToString() ?? "No exception object"}\n";
                File.WriteAllText(crashLogFile, basicInfo, System.Text.Encoding.UTF8);
                Utils.Debug.Log.Fatal("基础崩溃信息已保存");

                try
                {
                    var crashReport = new System.Text.StringBuilder();
                    crashReport.AppendLine("=== CRASH REPORT ===");
                    crashReport.AppendLine($"Time: {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                    crashReport.AppendLine($"IsTerminating: {e.IsTerminating}");
                    
                    try
                    {
                        crashReport.AppendLine($"Server: {(Logic.Agent.Instance.CurrentServer != null ? Logic.Agent.Instance.CurrentServer.name.ToString() : "Unknown")}");
                        crashReport.AppendLine($"Environment: {(Logic.Agent.Instance.IsDevelopment ? "Development" : "Production")}");
                    }
                    catch { crashReport.AppendLine("Server: Failed to get server info"); }
                    
                    crashReport.AppendLine();
                    crashReport.AppendLine("=== EXCEPTION ===");
                    crashReport.AppendLine(e.ExceptionObject?.ToString() ?? "No exception object");
                    crashReport.AppendLine();
                    crashReport.AppendLine("=== LAST 1000 DEBUG LOGS ===");

                    try
                    {
                        var logs = Utils.Debug.Log.GetLogs(limit: 1000);
                        foreach (var log in logs.OrderBy(l => l.Time))
                        {
                            crashReport.AppendLine($"[{log.Time:yyyy-MM-dd HH:mm:ss.fff}] [{log.Level}] [{log.Category}] {log.Message}");
                            if (log.Details != null)
                            {
                                try
                                {
                                    crashReport.AppendLine($"  Details: {Newtonsoft.Json.JsonConvert.SerializeObject(log.Details)}");
                                }
                                catch { }
                            }
                        }
                    }
                    catch (Exception logEx)
                    {
                        crashReport.AppendLine($"Failed to retrieve logs: {logEx.Message}");
                    }

                    File.WriteAllText(crashLogFile, crashReport.ToString(), System.Text.Encoding.UTF8);
                    Utils.Debug.Log.Fatal($"完整崩溃日志已保存到: {crashLogFile}");
                }
                catch (Exception detailEx)
                {
                    Utils.Debug.Log.Fatal($"详细信息保存失败: {detailEx.Message}");
                }

                EmergencySave();
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(crashLogFile, $"CRITICAL CRASH\n{timestamp:yyyy-MM-dd HH:mm:ss}\n{ex.Message}\n{e.ExceptionObject}", System.Text.Encoding.UTF8);
                }
                catch { }
                Utils.Debug.Log.Fatal($"紧急保存失败: {ex.Message}");
            }
        }

        private static void EmergencySave()
        {
            lock (shutdownLock)
            {
                if (isShuttingDown) return;
                isShuttingDown = true;
            }

            try
            {
                var players = Logic.Agent.Instance.Content.Gets<Logic.Player>().ToList();
                Utils.Debug.Log.Fatal($"同步 {players.Count} 个玩家数据到内存...");
                
                foreach (var player in players)
                {
                    try
                    {
                        Domain.Authentication.Logout.Do(player);
                    }
                    catch (Exception ex)
                    {
                        Utils.Debug.Log.Fatal($"玩家 {player.Id} 数据同步失败: {ex.Message}");
                    }
                }

                Utils.Debug.Log.Fatal("保存所有数据到数据库...");
                Logic.Database.Agent.Instance.Save<Logic.Database.Device>(Logic.Config.MySQL.ConnectionString);
                Logic.Database.Agent.Instance.Save<Logic.Database.Player>(Logic.Config.MySQL.ConnectionString);
                
                var playerCount = Logic.Database.Agent.Instance.Content.Gets<Logic.Database.Player>().Count();
                Utils.Debug.Log.Fatal($"紧急保存完成，共保存 {playerCount} 个账号");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Fatal($"紧急保存失败: {ex.Message}");
                Utils.Debug.Log.Fatal($"堆栈: {ex.StackTrace}");
            }
        }

        private static void Shutdown(string reason)
        {
            lock (shutdownLock)
            {
                if (isShuttingDown) return;
                isShuttingDown = true;
            }

            Utils.Debug.Log.Fatal($"检测到{reason}，开始关闭服务器...");

            try
            {
                var players = Logic.Agent.Instance.Content.Gets<Logic.Player>().ToList();
                Utils.Debug.Log.Fatal($"同步 {players.Count} 个玩家数据到内存...");
                
                foreach (var player in players)
                {
                    try
                    {
                        Domain.Authentication.Logout.Do(player);
                    }
                    catch (Exception ex)
                    {
                        Utils.Debug.Log.Fatal($"玩家 {player.Id} 数据同步失败: {ex.Message}");
                    }
                }

                Utils.Debug.Log.Fatal("保存所有数据到数据库...");
                Logic.Database.Agent.Instance.Save<Logic.Database.Device>(Logic.Config.MySQL.ConnectionString);
                Logic.Database.Agent.Instance.Save<Logic.Database.Player>(Logic.Config.MySQL.ConnectionString);
                
                var playerCount = Logic.Database.Agent.Instance.Content.Gets<Logic.Database.Player>().Count();
                Utils.Debug.Log.Fatal($"数据保存完成，共保存 {playerCount} 个账号");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Fatal($"数据保存失败: {ex.Message}");
                Utils.Debug.Log.Fatal($"堆栈: {ex.StackTrace}");
            }

            Domain.CrashGuard.Instance.MarkNormalShutdown();
            Utils.Debug.Log.Shutdown();
            
            Thread.Sleep(500);
            Environment.Exit(0);
        }

        private static void KeyboardListener()
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        Shutdown("ESC键");
                        break;
                    }
                    else if (key.Key == ConsoleKey.Delete && Utils.Debug.Log.IsDevelopment)
                    {
                        Console.Clear();
                    }
                    else if (key.Key == ConsoleKey.F1 && Utils.Debug.Log.IsDevelopment)
                    {
                        TeleportToAndFollow("农夫", 2010000);
                    }
                    else if (key.Key == ConsoleKey.F2 && Utils.Debug.Log.IsDevelopment)
                    {
                        TeleportToAndFollow("矿工", 2010001);
                    }
                    else if (key.Key == ConsoleKey.F3 && Utils.Debug.Log.IsDevelopment)
                    {
                        TeleportToAndFollow("猎人", 2010002);
                    }
                    else if (key.Key == ConsoleKey.F4 && Utils.Debug.Log.IsDevelopment)
                    {
                        TeleportToAndFollow("商人", 2010010);
                    }
                    else if (key.Key == ConsoleKey.F5 && Utils.Debug.Log.IsDevelopment)
                    {
                        ShowFollowedNpcBehaviorTree();
                    }
                    else if (key.Key == ConsoleKey.F6 && Utils.Debug.Log.IsDevelopment)
                    {
                        PlayPlot("传授偷窃", 30001);
                    }
                    else if (key.Key == ConsoleKey.F7 && Utils.Debug.Log.IsDevelopment)
                    {
                        PlayPlot("传授刀法", 30002);
                    }
                    else if (key.Key == ConsoleKey.F8 && Utils.Debug.Log.IsDevelopment)
                    {
                        PlayPlot("传授剑术", 30003);
                    }
                    else if (key.Key == ConsoleKey.F9 && Utils.Debug.Log.IsDevelopment)
                    {
                        PlayPlot("传授驯兽", 30004);
                    }
                    else if (key.Key == ConsoleKey.F10 && Utils.Debug.Log.IsDevelopment)
                    {
                        ExportLifeAttributes();
                    }
                    else if (key.Key == ConsoleKey.F11 && Utils.Debug.Log.IsDevelopment)
                    {
                        ToggleTcpLog();
                    }
                }
                Thread.Sleep(100);
            }
        }

        private static void PlayPlot(string plotName, int plotId)
        {
            try
            {
                var player = Logic.Agent.Instance.Content.Get<Logic.Player>();
                if (player == null)
                {
                    Utils.Debug.Log.Warning("GM", "未找到玩家");
                    return;
                }

                var plotConfig = Logic.Config.Agent.Instance.Content.Get<Logic.Config.Plot>(p => p.Id == plotId);
                if (plotConfig == null)
                {
                    Utils.Debug.Log.Warning("GM", $"未找到剧情: {plotName} (ID:{plotId})");
                    return;
                }

                var plot = new Logic.Plot { Config = plotConfig };
                Domain.Story.DialogueSender.Do(player, plot);
                Utils.Debug.Log.Info("GM", $"[播放剧情] {plotName}");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("GM", $"播放剧情失败: {ex.Message}", ex);
            }
        }

        private static void TeleportToAndFollow(string targetName, int targetId)
        {
            try
            {
                var player = Logic.Agent.Instance.Content.Get<Logic.Player>();
                if (player == null)
                {
                    Utils.Debug.Log.Warning("GM", $"未找到玩家");
                    return;
                }

                var targetLife = Domain.Move.Distance.Nearest<Logic.Life>(player, life => 
                    life.Config?.Id == targetId && !life.State.Is(Logic.Life.States.Unconscious));

                if (targetLife == null)
                {
                    Utils.Debug.Log.Warning("GM", $"未找到目标NPC: {targetName}");
                    return;
                }

                if (targetLife.Map == null)
                {
                    Utils.Debug.Log.Warning("GM", $"目标NPC {targetName} 不在任何地图上");
                    return;
                }

                if (player.Leader != null)
                {
                    Domain.Move.Follow.DoUnFollow(player);
                }

                Domain.Move.Agent.Do(player, targetLife.Map);
                Domain.Move.Follow.Do(player, targetLife);

                string mapInfo = targetLife.Map.Config != null 
                    ? Domain.Text.Agent.Instance.Get(targetLife.Map.Config.Name, player) 
                    : $"Map#{targetLife.Map.Database.id}";
                Utils.Debug.Log.Info("GM", $"[瞬移跟随] 已传送到 {targetName} 所在地图 ({mapInfo})，并开始跟随");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("GM", $"传送并跟随失败: {ex.Message}", ex);
            }
        }

        private static void ShowFollowedNpcBehaviorTree()
        {
            try
            {
                var player = Logic.Agent.Instance.Content.Get<Logic.Player>();
                if (player == null)
                {
                    Utils.Debug.Log.Warning("BEHAVIOR_TREE", "未找到玩家");
                    return;
                }

                if (player.Leader == null)
                {
                    Utils.Debug.Log.Warning("BEHAVIOR_TREE", "玩家未跟随任何NPC，请先使用F1~F4跟随目标");
                    return;
                }

                var npc = player.Leader;
                string npcName = Domain.Text.Agent.Instance.Get(npc.Config?.Name ?? 0, player);
                
                if (npc.BtRoot == null)
                {
                    Utils.Debug.Log.Warning("BEHAVIOR_TREE", $"{npcName} 没有行为树");
                    return;
                }

                Utils.Debug.Log.Info("BEHAVIOR_TREE", $"手动触发 {npcName} 行为树执行 (Config ID: {npc.Config.Id})");
                npc.BtRoot.ExecuteWithDebug(npc);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("BEHAVIOR_TREE", $"执行行为树失败: {ex.Message}", ex);
            }
        }

        private static void ToggleTcpLog()
        {
            try
            {
                var currentState = Utils.Debug.Log.IsCategoryEnabled("TCP");
                Utils.Debug.Log.SetCategoryEnabled("TCP", !currentState);
                var newState = !currentState ? "ON" : "OFF";
                Utils.Debug.Log.Info("DEBUG", $"TCP protocol log switched {newState}");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("DEBUG", $"Failed to toggle TCP log: {ex.Message}", ex);
            }
        }

        private static void ExportLifeAttributes()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"LifeAttributes_{timestamp}.csv";
                var filePath = System.IO.Path.Combine(Utils.Paths.Logs, fileName);
                
                var lives = Logic.Agent.Instance.Content.Gets<Logic.Life>().ToList();
                
                var rows = new List<List<object>>();
                rows.Add(new List<object> { "ConfigID", "Name", "Category", "Gender", "Level", "Age", "Hp", "Mp", "Lp", "Atk", "Def", "Agi", "Ine", "Con", "BTIntervalSec" });
                
                foreach (var life in lives)
                {
                    var configId = life is Logic.Player ? 0 : (life.Config?.Id ?? 0);
                    
                    var name = life is Logic.Player player
                        ? (player.Name ?? player.Id)
                        : (life.Config?.Name != null 
                            ? Domain.Text.Agent.Instance.Get(life.Config.Name, Logic.Text.Languages.ChineseSimplified) 
                            : string.Empty);
                    
                    var parts = life.Content.Gets<Logic.Part>();
                    var totalMaxHp = parts.Sum(p => p.MaxHp);
                    var totalHp = parts.Sum(p => p.Hp);
                    
                    var realAge = life.Age / Domain.Time.Agent.Rate;
                    
                    double speed = Utils.Mathematics.Ratio(life.Agi, 100);
                    double btIntervalMs = 1000 * (1 - speed);
                    double btIntervalSec = btIntervalMs / 1000.0;
                    
                    rows.Add(new List<object>
                    {
                        configId,
                        name,
                        life.Category.ToString(),
                        life.Gender.ToString(),
                        life.Level,
                        realAge.ToString("F1"),
                        $"{totalHp}/{totalMaxHp}",
                        $"{(int)life.Mp}/{(int)life.MaxMp}",
                        $"{(int)life.Lp}/{(int)life.MaxLp}",
                        ((int)life.Atk).ToString(),
                        ((int)life.Def).ToString(),
                        ((int)life.Agi).ToString(),
                        ((int)life.Ine).ToString(),
                        ((int)life.Con).ToString(),
                        btIntervalSec.ToString("F2")
                    });
                }
                
                Utils.Csv.SaveByCells(rows, filePath);
                
                Utils.Debug.Log.Info("GM", $"Life attributes exported: {fileName} ({lives.Count} entities)");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("GM", $"Export life attributes failed: {ex.Message}", ex);
            }
        }
    }
}

