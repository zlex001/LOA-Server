using Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Time
{
    public class State
    {
        public static State Instance => instance ??= new State();
        private static State instance;

        private readonly List<Life> _battleList = new();
        private readonly List<Life> _normalList = new();
        private readonly List<Life> _defaultList = new();
        
        // Bucket-based state machine update
        // Instead of updating all NPCs at once, we split them into buckets
        private const int NORMAL_BUCKET_COUNT = 5;  // Split normal updates into 5 buckets
        private int _normalBucket = 0;
        
        // Performance statistics
        private long _lastStatLog = 0;
        private long _totalNormalUpdates = 0;
        private long _totalBattleUpdates = 0;
        private long _maxNormalTickMs = 0;

        public void Init()
        {
            global::Data.Agent.Instance.Content.Add.Register(typeof(Life), OnLifeAdded);
            global::Data.Agent.Instance.Content.Remove.Register(typeof(Life), OnLifeRemoved);
            
            foreach (var life in global::Data.Agent.Instance.Content.Gets<Life>())
            {
                AddToStateList(life);
                life.State.monitor.Register(Basic.State<Life.States>.Event.Changed, (args) => OnLifeStateChanged(life, args));
            }

            Agent.Instance.Scheduler.Once(0, (_) => Agent.Instance.Scheduler.Repeat(global::Data.Constant.StateBattleTickIntervalMs, (_) => OnBattleTick()));
            // Normal tick now runs more frequently but only processes a fraction each time
            // Original: 300ms, process all NPCs
            // New: 60ms (300/5), process 1/5 of NPCs each time -> same effective rate per NPC
            Agent.Instance.Scheduler.Once(50, (_) => Agent.Instance.Scheduler.Repeat(60, (_) => OnNormalTick()));
            Agent.Instance.Scheduler.Once(150, (_) => Agent.Instance.Scheduler.Repeat(global::Data.Constant.StateDefaultTickIntervalMs, (_) => OnDefaultTick()));
            
            Utils.Debug.Log.Info("STATE", $"[State.Init] Bucketed state machine initialized - NormalBuckets={NORMAL_BUCKET_COUNT}");
        }

        private void OnLifeAdded(params object[] args)
        {
            if (args[1] is Life life)
            {
                AddToStateList(life);
                life.State.monitor.Register(Basic.State<Life.States>.Event.Changed, (stateArgs) => OnLifeStateChanged(life, stateArgs));
            }
        }

        private void OnLifeRemoved(params object[] args)
        {
            if (args[1] is Life life)
            {
                RemoveFromAllLists(life);
            }
        }

        private void OnLifeStateChanged(Life life, params object[] args)
        {
            RemoveFromAllLists(life);
            AddToStateList(life);
        }

        private void AddToStateList(Life life)
        {
            var state = life.State.CurrentKey;
            if (state == Life.States.Battle && !_battleList.Contains(life))
                _battleList.Add(life);
            else if (state == Life.States.Normal && !_normalList.Contains(life))
                _normalList.Add(life);
            else if (state != Life.States.Battle && state != Life.States.Normal && !_defaultList.Contains(life))
                _defaultList.Add(life);
        }

        private void RemoveFromAllLists(Life life)
        {
            _battleList.Remove(life);
            _normalList.Remove(life);
            _defaultList.Remove(life);
        }

    private void OnBattleTick()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = _battleList.Count - 1; i >= 0; i--)
        {
            if (i < _battleList.Count)
            {
                try
                {
                    _battleList[i].State.Update(_battleList[i]);
                }
                catch (Exception ex)
                {
                    var lifeName = _battleList[i]?.GetType().Name ?? "Unknown";
                    Utils.Debug.Log.Error("STATE", 
                        $"OnBattleTick EXCEPTION - Life={lifeName}: {ex.Message}", 
                        new { StackTrace = ex.StackTrace, LifeType = lifeName });
                    
                    // 移除问题Life，防止持续崩溃
                    try { _battleList.RemoveAt(i); } catch { }
                }
            }
        }
        sw.Stop();
    }

    private void OnNormalTick()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        int count = _normalList.Count;
        if (count == 0)
        {
            _normalBucket = (_normalBucket + 1) % NORMAL_BUCKET_COUNT;
            return;
        }
        
        // Only process NPCs that belong to current bucket
        // Each NPC gets updated once every (NORMAL_BUCKET_COUNT * tick_interval) ms
        // With 5 buckets and 60ms interval, each NPC updates every 300ms (same as before)
        int updatedCount = 0;
        for (int i = count - 1; i >= 0; i--)
        {
            if (i >= _normalList.Count) continue;
            
            // Bucket assignment based on index
            if (i % NORMAL_BUCKET_COUNT != _normalBucket) continue;
            
            try
            {
                _normalList[i].State.Update(_normalList[i]);
                updatedCount++;
                _totalNormalUpdates++;
            }
            catch (Exception ex)
            {
                var lifeName = _normalList[i]?.GetType().Name ?? "Unknown";
                Utils.Debug.Log.Error("STATE", 
                    $"OnNormalTick EXCEPTION - Life={lifeName}: {ex.Message}", 
                    new { StackTrace = ex.StackTrace, LifeType = lifeName });
                
                try { _normalList.RemoveAt(i); } catch { }
            }
        }
        
        // Advance to next bucket
        _normalBucket = (_normalBucket + 1) % NORMAL_BUCKET_COUNT;
        
        sw.Stop();
        long elapsedMs = sw.ElapsedMilliseconds;
        if (elapsedMs > _maxNormalTickMs) _maxNormalTickMs = elapsedMs;
        
        // Log statistics every 30 seconds
        long now = Environment.TickCount64;
        if (now - _lastStatLog >= 30000)
        {
            Utils.Debug.Log.Info("STATE", $"[State Stats] NormalList={count}, BattleList={_battleList.Count}, DefaultList={_defaultList.Count}, TotalUpdates={_totalNormalUpdates}, MaxTickMs={_maxNormalTickMs}");
            _totalNormalUpdates = 0;
            _maxNormalTickMs = 0;
            _lastStatLog = now;
        }
    }

    private void OnDefaultTick()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = _defaultList.Count - 1; i >= 0; i--)
        {
            if (i < _defaultList.Count)
            {
                try
                {
                    _defaultList[i].State.Update(_defaultList[i]);
                }
                catch (Exception ex)
                {
                    var lifeName = _defaultList[i]?.GetType().Name ?? "Unknown";
                    Utils.Debug.Log.Error("STATE", 
                        $"OnDefaultTick EXCEPTION - Life={lifeName}: {ex.Message}", 
                        new { StackTrace = ex.StackTrace, LifeType = lifeName });
                    
                    // 移除问题Life，防止持续崩溃
                    try { _defaultList.RemoveAt(i); } catch { }
                }
            }
        }
        sw.Stop();
    }
    }
}
