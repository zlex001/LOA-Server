using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Domain
{
    public static class SafetyNet
    {
        private static ConcurrentDictionary<string, int> errorStats = new ConcurrentDictionary<string, int>();
        private static ConcurrentDictionary<object, int> entityErrors = new ConcurrentDictionary<object, int>();
        private const int MaxEntityErrors = 20;

        public static bool Execute(Func<bool> func, string context)
        {
            try { return func(); }
            catch (Exception ex) 
            { 
                LogError(ex, context);
                if (Logic.Agent.Instance.IsDevelopment) throw;
                return false; 
            }
        }

        public static void Execute(Action action, string context)
        {
            try { action(); }
            catch (Exception ex) 
            { 
                LogError(ex, context);
                if (Logic.Agent.Instance.IsDevelopment) throw;
            }
        }

        public static bool ExecuteForEntity(Func<bool> func, object entity, string context)
        {
            try 
            { 
                var result = func();
                if (result) 
                {
                    entityErrors.TryRemove(entity, out _);
                }
                return result;
            }
            catch (Exception ex) 
            { 
                LogError(ex, context);
                if (Logic.Agent.Instance.IsDevelopment) throw;
                IncrementEntityError(entity);
                return false; 
            }
        }

        private static void LogError(Exception ex, string context)
        {
            var errorKey = $"{context}:{ex.GetType().Name}";
            var count = errorStats.AddOrUpdate(errorKey, 1, (k, v) => v + 1);

            if (count == 1 || count == 10 || count == 100 || count % 1000 == 0)
            {
                Utils.Debug.Log.Error("SAFETY_NET", 
                    $"[{context}] {ex.GetType().Name}: {ex.Message} (累计{count}次)", 
                    new { StackTrace = ex.StackTrace });
            }
        }

        private static void IncrementEntityError(object entity)
        {
            if (entity == null) return;
            
            var count = entityErrors.AddOrUpdate(entity, 1, (k, v) => v + 1);
            
            if (count >= MaxEntityErrors)
            {
                HandleBrokenEntity(entity);
            }
        }

        private static void HandleBrokenEntity(object entity)
        {
            if (entity is Logic.Life life && !(entity is Logic.Player))
            {
                Utils.Debug.Log.Error("SAFETY_NET", $"Life[{entity.GetHashCode()}]累计{MaxEntityErrors}次错误，已移除");
                try { Logic.Agent.Instance.Remove(life); } catch { }
            }
            else if (entity is Logic.Player player)
            {
                Utils.Debug.Log.Error("SAFETY_NET", $"Player[{player.Id}]累计{MaxEntityErrors}次错误，已踢出");
                try { Authentication.Logout.Do(player); } catch { }
            }
            
            entityErrors.TryRemove(entity, out _);
        }

        public static Dictionary<string, int> GetErrorStats()
        {
            return new Dictionary<string, int>(errorStats);
        }

        public static void ClearStats()
        {
            errorStats.Clear();
        }
    }
}

