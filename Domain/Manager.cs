using Logic;
using System.Diagnostics;
using System.Linq;
using Utils;

namespace Domain
{
    public class Manager : Basic.Ability
    {
        #region Singleton Pattern
        private static Manager instance;
        public static Manager Instance { get { if (instance == null) { instance = new Manager(); } return instance; } }
        #endregion

        #region Staggered Execution
        private const int Buckets = 10;
        private int slice = 0;
        #endregion

        #region Performance Monitoring
        private System.Diagnostics.Stopwatch performanceTimer = new System.Diagnostics.Stopwatch();
        private long lastPerformanceLog = Environment.TickCount64;
        private int totalBtExecutions = 0;
        private long totalUpdateTime = 0;


        private static long _lastMainLoopTime = Environment.TickCount64;
        private static long _mainLoopCount = 0;
        private static long _slowUpdateCount = 0;
        private static long _maxUpdateDuration = 0;


        private static double _currentFrequency = 0;
        private static long _lastMaxUpdateDuration = 0;
        private static long _lastSlowUpdateCount = 0;
        #endregion

        public override void Init(params object[] args)
        {
            Utils.Debug.Log.Info("DOMAIN", "[Manager.Init] Starting domain initialization...");
            
            Logic.Life.ConditionChecker = (target, conditions) => Domain.Condition.CheckWithReason(target, conditions);

            Logic.Config.SingleCondition.ConditionChecker = (ability, conditions, eventArgs) => Domain.Condition.Check((Logic.Ability)ability, conditions, eventArgs);
            Logic.Config.SingleCondition.SingleConditionChecker = (target, condition) => Domain.Condition.Check(target, condition);

            Utils.Debug.Log.Info("DOMAIN", "[Manager.Init] Initializing Authentication.Agent...");
            Authentication.Agent.Instance.Init();
            Utils.Debug.Log.Info("DOMAIN", "[Manager.Init] Authentication.Agent initialized");
            Administrator.Agent.Instance.Init();
            Move.Agent.Init();
            Battle.Agent.Instance.Init();
            Cast.Agent.Instance.Init();
            Buff.Agent.Instance.Init();
            Domain.BehaviorTree.Agent.Init();
            PVP.Offline.Instance.Init();
            Recharge.Instance.Init();
            Hot.Instance.Init();
            Quest.Agent.Instance.Init();
            Infrastructure.Agent.Init();
            Click.Agent.Init();
            Operation.Agent.Instance.Init();
            Exchange.Agent.Init();
            Companion.Agent.Init();
            Pet.Agent.Init();
            Deposit.Instance.Init();
            Settings.Instance.Init();
            Talk.Agent.Instance.Init();
            Use.Agent.Instance.Init();
            Text.Agent.Instance.Init();
            EconomyMonitor.Instance.Init();
            State.Agent.Init();
            Perception.Agent.Instance.Init();
            Display.Agent.Instance.Init();
            Time.Agent.Instance.Init();
            Time.Season.Instance.Init();
            Lighting.Instance.Init();
            Develop.Agent.Init();
            Subscription.Agent.Init();
            PVP.Offline.Instance.Init();
            Chat.Instance.Init();
            Tutorial.Instance.Init();

            Logic.Agent.Instance.data.after.Register(Logic.Agent.Data.Open, OnLogicManagerOpenChanged);
            
            //StartAutoSave();
        }

        //private void StartAutoSave()
        //{
        //    Basic.Time.Manager.Instance.Scheduler.Repeat(300000, (_) =>
        //    {
        //        System.Threading.Tasks.Task.Run(() =>
        //        {
        //            SafetyNet.Execute(() =>
        //            {
        //                try
        //                {
        //                    var players = Logic.Agent.Instance.Content.Gets<Logic.Player>().ToList();
        //                    if (players.Count == 0) return;

        //                    Utils.Debug.Log.Info("AUTOSAVE", $"开始自动存档，共 {players.Count} 个在线玩家");
        //                    var startTime = DateTime.Now;

        //                    foreach (var player in players)
        //                    {
        //                        try
        //                        {
        //                            Authentication.Logout.Do(player);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Utils.Debug.Log.Error("AUTOSAVE", $"玩家 {player.Id} 数据同步失败: {ex.Message}");
        //                        }
        //                    }

        //                    Logic.Database.Agent.Instance.Save<Logic.Database.Device>(Logic.Config.MySQL.ConnectionString);
        //                    Logic.Database.Agent.Instance.Save<Logic.Database.Player>(Logic.Config.MySQL.ConnectionString);

        //                    var duration = (DateTime.Now - startTime).TotalMilliseconds;
        //                    Utils.Debug.Log.Info("AUTOSAVE", $"自动存档完成，耗时 {duration:F0}ms");
        //                }
        //                catch (Exception ex)
        //                {
        //                    Utils.Debug.Log.Error("AUTOSAVE", $"自动存档异常: {ex.Message}");
        //                }
        //            }, "AutoSave");
        //        });
        //    });
        //}

        private void OnLogicManagerOpenChanged(params object[] args)
        {
            Utils.Debug.Log.Info("DOMAIN", $"[OnLogicManagerOpenChanged] Called with args[0]={args[0]}");
            bool open = (bool)args[0];
            if (open)
            {
                Utils.Debug.Log.Info("DOMAIN", "[OnLogicManagerOpenChanged] open=true, initializing NPCs...");
                Time.Daily.Instance.InitializeAllScenesNPCs();
                ScheduleShutdown();
                
                Utils.Debug.Log.Info("DOMAIN", "[OnLogicManagerOpenChanged] Entering main loop (multi-threaded network mode)...");
                while (Logic.Agent.Instance.data.Get<bool>(Logic.Agent.Data.Open))
                {
                    SafetyNet.Execute(() =>
                    {
                        long updateStart = Environment.TickCount64;
                        
                        // PRIORITY 1: Process network messages first (never blocked by game logic)
                        // Network IO threads have already received data into queue
                        Net.Tcp.Instance.ProcessNetwork();
                        
                        // PRIORITY 2: Advance behavior tree bucket for frame distribution
                        State.Agent.AdvanceBtBucket();
                        
                        // PRIORITY 3: Then process game logic via scheduler
                        Basic.Time.Manager.Instance.Update();
                        
                        long updateEnd = Environment.TickCount64;
                        long duration = updateEnd - updateStart;
                        
                        if (duration > 200)
                        {
                            Utils.Debug.Performance.RecordSlowOperation(duration, "MainLoop", "Main loop update");
                        }

                        _mainLoopCount++;
                        if (duration > _maxUpdateDuration) _maxUpdateDuration = duration;
                        if (duration > 50) _slowUpdateCount++;

                        LogMainLoopPerformanceIfNeeded();
                    }, "MainLoop");
                }
                
                // Shutdown network threads when exiting
                Net.Tcp.Instance.Shutdown();
            }
            else
            {
                Utils.Debug.Log.Info("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] 开始执行关服流程...");
                var players = Logic.Agent.Instance.Content.Gets<Player>().ToList();
                Utils.Debug.Log.Info("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] 需要同步 {players.Count} 个玩家数据到内存");

                foreach (var player in players)
                {
                    try
                    {
                        Authentication.Logout.Do(player);
                    }
                    catch (Exception ex)
                    {
                        Utils.Debug.Log.Error("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] 玩家数据同步失败: {ex.Message}");
                    }
                }

                Utils.Debug.Log.Info("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] 开始保存所有数据到数据库...");
                try
                {
                    Logic.Database.Agent.Instance.Save<Logic.Database.Device>(Logic.Config.MySQL.ConnectionString);
                    Utils.Debug.Log.Info("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] Device数据保存完成");

                    Logic.Database.Agent.Instance.Save<Logic.Database.Player>(Logic.Config.MySQL.ConnectionString);
                    var playerCount = Logic.Database.Agent.Instance.Content.Gets<Logic.Database.Player>().Count();
                    Utils.Debug.Log.Info("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] Player数据保存完成，共 {playerCount} 个账号");
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Error("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] 数据库保存失败: {ex.Message}");
                }

                EconomyMonitor.Instance.Shutdown();
                Utils.Debug.Log.Info("SHUTDOWN", $"[{DateTime.Now:HH:mm:ss}] 所有数据保存完成，程序即将退出");
                System.Threading.Thread.Sleep(500);
                Environment.Exit(0);
            }
        }

        private void ScheduleShutdown()
        {
            DateTime now = DateTime.Now;
            DateTime shutdownTime = DateTime.Today.AddHours(8);
            if (now >= shutdownTime)
            {
                shutdownTime = shutdownTime.AddDays(1);
            }

            long delayMs = (long)(shutdownTime - now).TotalMilliseconds;

            Time.Agent.Instance.Scheduler.Once(delayMs, (_) =>
            {
                Logic.Agent.Instance.data.Change(Logic.Agent.Data.Open, false);
            });

            if (delayMs > 60000)
            {
                Time.Agent.Instance.Scheduler.Once(delayMs - 60000, (_) =>
                {
                    GenerateAndPrintDailyReport();
                });
            }
        }

        private void OnDecisecondUpdate(params object[] args)
        {

            bool completelySkipDecisecondUpdate = false;
            if (completelySkipDecisecondUpdate)
            {
                return;
            }


            long startTime = Environment.TickCount64;

            var allLives = Logic.Agent.Instance.Content.Gets<Life>().ToList();
            long afterGetLives = Environment.TickCount64;

            bool skipDecisecond = false;
            bool skipSecond = false;
            bool skipGetLives = false;

            // Legacy time event dispatch removed - all listeners migrated to Scheduler
            long afterDecisecond = Environment.TickCount64;

            var currentBucket = slice++ % Buckets;
            var bucketLives = allLives.Where((life, index) => index % Buckets == currentBucket).ToList();

            if (!skipSecond)
            {
                // Legacy time event dispatch removed - all listeners migrated to Scheduler
            }
            else
            {
            }
            long afterSecond = Environment.TickCount64;


            long getLivesTime = afterGetLives - startTime;
            long decisecondTime = afterDecisecond - afterGetLives;
            long secondTime = afterSecond - afterDecisecond;
            long totalTime = afterSecond - startTime;

            if (totalTime > 100)
            {

                int totalAffects = 0;
                int activeBehaviorTrees = 0;
                foreach (var life in allLives)
                {
                    if (life.BtRoot != null && !life.State.Is(Logic.Life.States.Unconscious)) activeBehaviorTrees++;
                }

                Utils.Debug.Performance.RecordSlowOperation(totalTime, "Decisecond", $"Decisecond事件处理 - Lives:{allLives.Count}, 本轮:{bucketLives.Count}, 行为树:{activeBehaviorTrees}");
            }

            totalUpdateTime += totalTime;
            LogPerformanceIfNeeded(allLives.Count, bucketLives.Count);
        }

        private void LogPerformanceIfNeeded(int totalLives, int bucketLives)
        {
            long now = Environment.TickCount64;
            if (now - lastPerformanceLog >= 30000)
            {
                totalUpdateTime = 0;
                lastPerformanceLog = now;
            }
        }


        private static void LogMainLoopPerformanceIfNeeded()
        {
            long now = Environment.TickCount64;
            if (now - _lastMainLoopTime >= 30000)
            {
                double elapsed = (now - _lastMainLoopTime) / 1000.0;
                double frequency = _mainLoopCount / elapsed;


                _currentFrequency = frequency;
                _lastMaxUpdateDuration = _maxUpdateDuration;
                _lastSlowUpdateCount = _slowUpdateCount;

                Utils.Debug.Performance.RecordSnapshot(frequency, _maxUpdateDuration, _slowUpdateCount);


                _mainLoopCount = 0;
                _slowUpdateCount = 0;
                _maxUpdateDuration = 0;
                _lastMainLoopTime = now;
            }
        }


        public static double GetMainLoopFrequency() => _currentFrequency;
        public static long GetMaxUpdateDuration() => _lastMaxUpdateDuration;
        public static long GetSlowUpdateCount() => _lastSlowUpdateCount;

        private void OnSecondUpdate(params object[] args)
        {

            bool skipSecondUpdate = true;
            if (skipSecondUpdate)
            {
                return;
            }


            long startTime = Environment.TickCount64;

            var allItems = Logic.Agent.Instance.Content.Gets<Item>().ToList();
            long afterGetItems = Environment.TickCount64;

            // Legacy time event dispatch removed - all listeners migrated to Scheduler
            long afterItemProcess = Environment.TickCount64;

            foreach (Player player in Logic.Agent.Instance.Content.Gets<Player>())
            {
                //player.Send(new Protocol.Ping());
            }
            long endTime = Environment.TickCount64;


            long totalTime = endTime - startTime;
            long getItemsTime = afterGetItems - startTime;
            long itemProcessTime = afterItemProcess - afterGetItems;
            long playerProcessTime = endTime - afterItemProcess;

            if (totalTime > 100)
            {
                Utils.Debug.Performance.RecordSlowOperation(totalTime, "Second", $"Second事件处理 - Items:{allItems.Count}个");
            }
        }

        private void OnMinuteUpdate(params object[] args)
        {
            //if (!content.Has<Life>(l => l.Id == "�Ž���" && !(l.Map is Copy)))
            //{
            //    BossReset();
            //}
            //Life boss = content.Get<Life>(l => l.Id == "�Ž���" && !(l.Map is Copy));
            //if (random.Next(0, 60) == 1)
            //{
            //           monitor.Fire(Channel.Rumor, this, Utils.Text.Color(Utils.Text.Colors.ChannelRumor, $"{boss.Name}��{boss.Map.NameWithScene}��"));
            //}
        }

        private void OnHourUpdate(params object[] args)
        {
            //BossReset();
            //Utils.Debug.Instance.Log($"' {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}*ÿСʱ1�θ���");
        }
        private void OnDayUpdate(params object[] args)
        {
            //Utils.Debug.Instance.Log($"' {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}*ÿ��1�θ���");
        }


        private void OnExactMinuteUpdate(params object[] args)
        {
            //Utils.Debug.Instance.Log($"' {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}*���ָ���");
        }

        private void OnExactHourUpdate(params object[] args)
        {
            //Utils.Debug.Instance.Log($"' {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}*��ʱ����");
        }


        private void GenerateAndPrintDailyReport()
        {
            try
            {
                DateTime yesterday = DateTime.Today.AddDays(-1);
                var report = new Analytics.Daily(yesterday);

                Utils.Debug.Log.Info("REPORT", $"");
                Utils.Debug.Log.Info("REPORT", $"========================================");
                Utils.Debug.Log.Info("REPORT", $"         每日运营报告");
                Utils.Debug.Log.Info("REPORT", $"========================================");
                Utils.Debug.Log.Info("REPORT", $"日期: {report.Date} ({report.Weekday})");
                Utils.Debug.Log.Info("REPORT", $"----------------------------------------");
                Utils.Debug.Log.Info("REPORT", $"用户数据:");
                Utils.Debug.Log.Info("REPORT", $"  活跃用户: {report.ActivePlayers}");
                Utils.Debug.Log.Info("REPORT", $"  新增用户: {report.NewPlayers}");
                Utils.Debug.Log.Info("REPORT", $"  新增有效用户: {report.NewValidPlayers}");
                Utils.Debug.Log.Info("REPORT", $"  留存率: {report.RetentionRate:P2}");
                Utils.Debug.Log.Info("REPORT", $"----------------------------------------");
                Utils.Debug.Log.Info("REPORT", $"设备数据:");
                Utils.Debug.Log.Info("REPORT", $"  新增设备: {report.NewDevices}");
                Utils.Debug.Log.Info("REPORT", $"  新增设备用户: {report.NewDevicePlayers}");
                Utils.Debug.Log.Info("REPORT", $"  新增有效设备用户: {report.NewValidDevicePlayers}");
                Utils.Debug.Log.Info("REPORT", $"----------------------------------------");
                Utils.Debug.Log.Info("REPORT", $"付费数据:");
                Utils.Debug.Log.Info("REPORT", $"  转化率: {report.ConversionRate:P2}");
                Utils.Debug.Log.Info("REPORT", $"  ARPU: ¥{report.ARPU:F2}");
                Utils.Debug.Log.Info("REPORT", $"  ARPPU: ¥{report.ARPPU:F2}");
                Utils.Debug.Log.Info("REPORT", $"  LTV: ¥{report.LTV:F2}");
                Utils.Debug.Log.Info("REPORT", $"========================================");
                Utils.Debug.Log.Info("REPORT", $"");

                Analytics.Instance.SaveToDatabase(yesterday);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("REPORT", $"生成每日报告失败: {ex.Message}");
            }
        }
    }
}


