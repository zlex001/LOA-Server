using Basic;
using Data;
using System;
using System.Linq;

namespace Logic.State
{
    public static class Agent
    {
        public static readonly double LpDrainPerSecond = 100.0 / global::Data.Constant.NormalLpDepletionSeconds;
        
        // Behavior tree frame bucketing - distributes execution across frames
        private const int BT_BUCKET_COUNT = 10;
        private static int _currentBtBucket = 0;
        private static long _lastBucketAdvance = 0;
        private const long BUCKET_ADVANCE_INTERVAL_MS = 100; // Advance bucket every 100ms
        
        // Statistics
        private static long _btExecutionsTotal = 0;
        private static long _btExecutionsSkipped = 0;
        private static long _lastStatLog = 0;

        public static void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(global::Data.Life), OnAddLife);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(global::Data.Life), OnRemoveLife);
        }
        
        /// <summary>
        /// Advance behavior tree bucket. Called from main loop.
        /// This distributes BT execution across frames.
        /// </summary>
        public static void AdvanceBtBucket()
        {
            long now = Environment.TickCount64;
            if (now - _lastBucketAdvance >= BUCKET_ADVANCE_INTERVAL_MS)
            {
                _currentBtBucket = (_currentBtBucket + 1) % BT_BUCKET_COUNT;
                _lastBucketAdvance = now;
            }
            
            // Log statistics every 30 seconds
            if (now - _lastStatLog >= 30000)
            {
                if (_btExecutionsTotal > 0)
                {
                    double skipRate = _btExecutionsSkipped * 100.0 / (_btExecutionsTotal + _btExecutionsSkipped);
                    // Utils.Debug.Log.Info("BT", $"[BT Stats] Executed={_btExecutionsTotal}, Skipped={_btExecutionsSkipped}, SkipRate={skipRate:F1}%");
                }
                _btExecutionsTotal = 0;
                _btExecutionsSkipped = 0;
                _lastStatLog = now;
            }
        }

        public static void RestoreHp(global::Data.Life life, double hpPerSecond)
        {
            if (life.Lp <= 0) return;
            if (!life.Content.Has<global::Data.Part>(p => p.Hp < p.MaxHp)) return;

            var tickInterval = life.State.CurrentKey == global::Data.Life.States.Battle 
                ? global::Data.Constant.StateBattleTickIntervalMs 
                : (life.State.CurrentKey == global::Data.Life.States.Normal 
                    ? global::Data.Constant.StateNormalTickIntervalMs 
                    : global::Data.Constant.StateDefaultTickIntervalMs);

            int healAmount = Math.Max(1, (int)(hpPerSecond * tickInterval / 1000.0));
            
            var part = life.Content.RandomGet<global::Data.Part>(p => p.Hp < p.MaxHp);
            if (part != null)
            {
                part.Hp += healAmount;
            }
        }

        public static void DrainLp(global::Data.Life life, double lpPerSecond)
        {
            var tickInterval = life.State.CurrentKey == global::Data.Life.States.Battle 
                ? global::Data.Constant.StateBattleTickIntervalMs 
                : (life.State.CurrentKey == global::Data.Life.States.Normal 
                    ? global::Data.Constant.StateNormalTickIntervalMs 
                    : global::Data.Constant.StateDefaultTickIntervalMs);

            double drainAmount = lpPerSecond * tickInterval / 1000.0;

            if (life.Lp > drainAmount)
            {
                life.Lp -= drainAmount;
            }
            else if (life.Lp > 0)
            {
                life.Lp = 0;
                DamageFromHunger(life);
            }
            else
            {
                DamageFromHunger(life);
            }
        }

        private static void DamageFromHunger(global::Data.Life life)
        {
            int damageValue = Math.Max(1, (int)(100 - life.Con / 10.0));
            var availableParts = life.Content.Gets<global::Data.Part>().Where(p => p.Hp > -p.MaxHp).ToList();
            if (availableParts.Any())
            {
                var randomPart = availableParts[Utils.Random.Instance.Next(availableParts.Count)];
                randomPart.Hp -= damageValue;
            }
        }

        private static double CalculateBehaviorTreeInterval(global::Data.Life life, bool isInitial = false)
        {
            // Increased base interval from 100000 to 150000 (50% longer intervals)
            // This reduces task count from ~170/frame to ~113/frame
            double baseInterval = 150000.0 / (life.Agi + 100);
            
            double multiplier = life.CurrentExecutingNode?.Config?.IntervalMultiplier ?? 1.0;
            
            if (multiplier <= 0)
            {
                multiplier = 1.0;
            }
            
            double interval = baseInterval * multiplier;
            
            // Add random jitter (0-20%) to prevent task bunching
            // This spreads out tasks that would otherwise fire at the same time
            double jitter = Utils.Random.Instance.NextDouble() * 0.2;
            interval *= (1.0 + jitter);
            
            // For initial scheduling, add extra random delay (0-2 seconds)
            // This prevents all NPCs from starting behavior trees simultaneously
            if (isInitial)
            {
                interval += Utils.Random.Instance.Next(0, 2000);
            }
            
            return interval < 100 ? 100 : interval; // Minimum 100ms (was 10ms)
        }

        public static void StartBehaviorTree(global::Data.Life life)
        {
            if (life.BtRoot == null) return;
            if (life.BtTaskId > 0) return;
            
            ScheduleNextBehaviorTreeExecution(life, isInitial: true);
        }

        public static void StopBehaviorTree(global::Data.Life life)
        {
            if (life.BtTaskId > 0)
            {
                Logic.Time.Agent.Instance.Scheduler.CancelTask(life.BtTaskId);
                life.BtTaskId = 0;
            }
        }

        private static void ScheduleNextBehaviorTreeExecution(global::Data.Life life, bool isInitial = false)
        {
            if (life.BtRoot == null) return;
            
            double interval = CalculateBehaviorTreeInterval(life, isInitial);
            life.BtTaskId = Logic.Time.Agent.Instance.Scheduler.Once((long)interval, (_) => 
            {
                ExecuteBehaviorTree(life);
            });
        }

        private static void ExecuteBehaviorTree(global::Data.Life life)
        {
            life.BtTaskId = 0;
            
            if (life.BtRoot == null)
            {
                life.CurrentExecutingNode = null;
                return;
            }
            
            if (!life.State.Is(global::Data.Life.States.Normal))
            {
                // Not in normal state, just reschedule without executing
                ScheduleNextBehaviorTreeExecution(life);
                return;
            }
            
            // Frame bucketing: only execute if this NPC belongs to current bucket
            // This distributes execution across 10 frames instead of all at once
            int lifeBucket = Math.Abs(life.GetHashCode()) % BT_BUCKET_COUNT;
            if (lifeBucket != _currentBtBucket)
            {
                // Not our turn, reschedule with short delay
                _btExecutionsSkipped++;
                life.BtTaskId = Logic.Time.Agent.Instance.Scheduler.Once(50, (_) => 
                {
                    ExecuteBehaviorTree(life);
                });
                return;
            }
            
            _btExecutionsTotal++;
            life.CurrentExecutingNode = null;
            
            SafetyNet.ExecuteForEntity(
                () => life.BtRoot.Execute(life), 
                life, 
                $"BehaviorTree[{life.BtRoot?.Config?.Id ?? 0}]");
            
            ScheduleNextBehaviorTreeExecution(life);
        }

        private static void OnAddLife(params object[] args)
        {
            global::Data.Life life = (global::Data.Life)args[1];
            life.State = new State<global::Data.Life.States>();
            life.State.Init(life);
            life.State.Register(new Normal(life), new Battle(life), new Unconscious(life));
            life.State.Change(global::Data.Life.States.Normal);
        }
        private static void OnRemoveLife(params object[] args)
        {
            global::Data.Life life = (global::Data.Life)args[1];
            StopBehaviorTree(life);
        }

    }
}
