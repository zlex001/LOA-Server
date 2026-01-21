using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Basic.Time
{
    public class Scheduler
    {
        private const int WHEEL_SIZE = 512;
        private const long TICK_PRECISION_MS = 100;
        public const int TICKS_PER_SECOND = 10;
        
        private readonly List<Task>[] _wheel;
        private readonly Queue<Task> _taskPool;
        private readonly Dictionary<long, TaskLocation> _taskLocations;
        
        private long _currentTick;
        private int _currentSlot;
        private long _lastTs = Stopwatch.GetTimestamp();
        private long _accumTs = 0;
        
        private readonly List<Task> _dueBuffer = new List<Task>(64);
        private bool _isTicking;
        private int _pendingTicks;
        
        public long CurrentTick => _currentTick;
        
        private long _totalTasksExecuted;
        private long _maxTasksPerFrame;
        private static long _taskIdCounter = 1;
        private Task _executing;
        public int MaxTasksPerFrame { get; set; } = 100;

        public Scheduler()
        {
            _wheel = new List<Task>[WHEEL_SIZE];
            for (int i = 0; i < WHEEL_SIZE; i++)
            {
                _wheel[i] = new List<Task>(16);
            }
            _taskPool = new Queue<Task>(1024);
            _taskLocations = new Dictionary<long, TaskLocation>(1024);
            _currentTick = 0;
            _currentSlot = 0;
        }

        public long Once(long delayMs, Action<object> callback, object context = null)
        {
            var task = GetOrCreateTask();
            task.Id = GenerateTaskId();
            task.Callback = callback;
            task.Context = context;
            task.IsRepeating = false;
            
            Enqueue(task, delayMs);
            return task.Id;
        }

        public long Repeat(long intervalMs, Action<object> callback, object context = null)
        {
            var task = GetOrCreateTask();
            task.Id = GenerateTaskId();
            task.Callback = callback;
            task.Context = context;
            task.IsRepeating = true;
            task.IntervalMs = intervalMs;
            
            var callbackName = callback?.Method.DeclaringType?.Name + "." + callback?.Method.Name;
            Utils.Debug.Log.Info("SCHEDULER", $"[Repeat] Registered task ID={task.Id}, Interval={intervalMs}ms, Callback={callbackName}");
            
            Enqueue(task, intervalMs);
            return task.Id;
        }

        public bool CancelTask(long taskId)
        {
            if (_executing != null && _executing.Id == taskId)
            {
                _executing.IsRepeating = false;
                return true;
            }
            
            if (!_taskLocations.TryGetValue(taskId, out var location)) return false;
            
            var slot = _wheel[location.SlotIndex];
            for (int i = 0; i < slot.Count; i++)
            {
                if (slot[i].Id == taskId)
                {
                    RecycleTask(slot[i]);
                    slot.RemoveAt(i);
                    _taskLocations.Remove(taskId);
                    return true;
                }
            }
            
            return false;
        }

        public void Tick()
        {
            if (_isTicking) 
            { 
                _pendingTicks++; 
                return; 
            }
            
            _isTicking = true;
            try
            {
                do
                {
                    _pendingTicks = 0;
                    CoreTickOnce();
                } while (_pendingTicks > 0);
            }
            finally 
            { 
                _isTicking = false; 
            }
        }

        private void CoreTickOnce()
        {
            long nowTs = Stopwatch.GetTimestamp();
            long deltaTs = nowTs - _lastTs;
            _lastTs = nowTs;

            _accumTs += deltaTs;

            long tsPerWheelTick = Stopwatch.Frequency * TICK_PRECISION_MS / 1000;
            long ticksToAdvance = _accumTs / tsPerWheelTick;
            
            if (ticksToAdvance <= 0) return;

            _accumTs -= ticksToAdvance * tsPerWheelTick;
            ticksToAdvance = Math.Min(ticksToAdvance, WHEEL_SIZE * 2);

            var tasksExecuted = 0;
            
            for (long i = 0; i < ticksToAdvance && tasksExecuted < MaxTasksPerFrame; i++)
            {
                _currentTick++;
                _currentSlot = (int)(_currentTick % WHEEL_SIZE);
                
                var slot = _wheel[_currentSlot];
                if (slot.Count == 0) continue;

                _dueBuffer.Clear();
                
                // 阶段1：只摘出到期任务，不执行回调
                var remaining = MaxTasksPerFrame - tasksExecuted;
                for (int j = slot.Count - 1; j >= 0 && remaining > 0; j--)
                {
                    var task = slot[j];
                    
                    if (task.Rounds > 0)
                    {
                        task.Rounds--;
                        continue;
                    }
                    
                    slot.RemoveAt(j);
                    _taskLocations.Remove(task.Id);
                    _dueBuffer.Add(task);
                    remaining--;
                }
                
                // 阶段2：执行回调（此时slot遍历已完成）
                foreach (var task in _dueBuffer)
                {
                    try
                    {
                        long taskStart = Environment.TickCount64;
                        
                        _executing = task;
                        task.Callback?.Invoke(task.Context);
                        _executing = null;
                        
                        long taskEnd = Environment.TickCount64;
                        long taskDuration = taskEnd - taskStart;
                        
                        if (taskDuration > 100)
                        {
                            var contextType = task.Context?.GetType().Name ?? "null";
                            var callbackName = task.Callback?.Method.DeclaringType?.Name + "." + task.Callback?.Method.Name;
                            Utils.Debug.Log.Warning("SCHEDULER", $"Slow task: ID={task.Id}, Duration={taskDuration}ms, Context={contextType}, Callback={callbackName}");
                        }
                        
                        tasksExecuted++;
                        _totalTasksExecuted++;
                    }
                    catch (Exception ex)
                    {
                        _executing = null;
                        var contextType = task.Context?.GetType().Name ?? "null";
                        var contextInfo = task.Context != null ? $"{contextType}" : "null";
                        Utils.Debug.Log.Error("SCHEDULER", $"Task exception - ID: {task.Id}, IsRepeating: {task.IsRepeating}, IntervalMs: {task.IntervalMs}, Context: {contextInfo}, Callback: {task.Callback?.Method.DeclaringType?.Name}.{task.Callback?.Method.Name}");
                        Utils.Debug.Log.Error("SCHEDULER", $"Full exception:\n{ex}");
                    }

                    if (task.IsRepeating)
                    {
                        Enqueue(task, task.IntervalMs);
                    }
                    else
                    {
                        RecycleTask(task);
                    }
                }
                
                if (tasksExecuted >= MaxTasksPerFrame)
                {
                    int nextSlot = (_currentSlot + 1) % WHEEL_SIZE;
                    for (int k = slot.Count - 1; k >= 0; k--)
                    {
                        var t = slot[k];
                        if (t.Rounds == 0) 
                        { 
                            slot.RemoveAt(k); 
                            _wheel[nextSlot].Add(t); 
                            _taskLocations[t.Id] = new TaskLocation{ SlotIndex = nextSlot }; 
                        }
                    }
                }
            }

            if (tasksExecuted > _maxTasksPerFrame)
            {
                _maxTasksPerFrame = tasksExecuted;
            }
        }

        public (long TotalTasks, long MaxTasksPerFrame, int QueuedTasks) GetStats()
        {
            var queuedTasks = 0;
            foreach (var slot in _wheel)
            {
                queuedTasks += slot.Count;
            }
            return (_totalTasksExecuted, _maxTasksPerFrame, queuedTasks);
        }

        private void Enqueue(Task task, long delayMs)
        {
            // 最小间隔钳制：防止0间隔递归风暴
            var ticksToWait = Math.Max(1, (delayMs + TICK_PRECISION_MS - 1) / TICK_PRECISION_MS);
            int offset = (int)(ticksToWait % WHEEL_SIZE);
            int targetSlot = (_currentSlot + offset) % WHEEL_SIZE;
            int rounds = (int)((ticksToWait - 1) / WHEEL_SIZE);
            
            // 顺延保护：如果在Tick期间且目标是当前slot且本轮执行，顺延1个slot
            if (_isTicking && rounds == 0 && targetSlot == _currentSlot)
            {
                targetSlot = (_currentSlot + 1) % WHEEL_SIZE;
            }
            
            task.Rounds = rounds;
            _wheel[targetSlot].Add(task);
            _taskLocations[task.Id] = new TaskLocation { SlotIndex = targetSlot };
        }

        private Task GetOrCreateTask()
        {
            if (_taskPool.Count > 0)
            {
                var task = _taskPool.Dequeue();
                task.Reset();
                return task;
            }
            return new Task();
        }

        private void RecycleTask(Task task)
        {
            if (_taskPool.Count < 1024)
            {
                task.Reset();
                _taskPool.Enqueue(task);
            }
        }

        private static long GenerateTaskId() => _taskIdCounter++;

        private struct TaskLocation
        {
            public int SlotIndex;
        }

        private class Task
        {
            public long Id { get; set; }
            public Action<object> Callback { get; set; }
            public object Context { get; set; }
            public bool IsRepeating { get; set; }
            public long IntervalMs { get; set; }
            public int Rounds { get; set; }

            public void Reset()
            {
                Id = 0;
                Callback = null;
                Context = null;
                IsRepeating = false;
                IntervalMs = 0;
                Rounds = 0;
            }
        }
    }
}
