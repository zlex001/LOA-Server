using System;
using System.Collections.Generic;
using Basic;

namespace Logic
{
    /// <summary>
    /// 统一的对象加载器，处理例外场景下的对象创建和注册
    /// 确保所有Logic层对象都能被Agent管理并触发Domain层事件
    /// </summary>
    public static class ObjectLoader
    {
        /// <summary>
        /// 从数据库数据创建并注册对象
        /// 用于替代直接new + Init的模式
        /// </summary>
        public static T LoadFromDatabase<T>(params object[] args) where T : Element, new()
        {
            var obj = new T();
            obj.Init(args);
            
            // 统一注册到Agent，触发Domain层监听
            if (ShouldRegisterToAgent<T>())
            {
                Agent.Instance.Add(obj);
            }
            
            return obj;
        }

        /// <summary>
        /// 从存档/序列化数据恢复对象
        /// </summary>
        public static T RestoreFromSave<T>(Dictionary<string, object> data) where T : Element, new()
        {
            var obj = new T();
            obj.Init(data);
            
            if (ShouldRegisterToAgent<T>())
            {
                Agent.Instance.Add(obj);
            }
            
            return obj;
        }

        /// <summary>
        /// 注册模板对象或配置实例
        /// </summary>
        public static void RegisterTemplate<T>(T obj) where T : Element
        {
            if (ShouldRegisterToAgent<T>())
            {
                Agent.Instance.Add(obj);
            }
        }

        /// <summary>
        /// 判断对象类型是否需要注册到Logic.Agent
        /// </summary>
        private static bool ShouldRegisterToAgent<T>() where T : Element
        {
            Type type = typeof(T);
            return type == typeof(Life) || 
                   type == typeof(Item) || 
                   type == typeof(Player) || 
                   type == typeof(Copy) || 
                   type == typeof(Map) ||
                   type.IsSubclassOf(typeof(Ability));
        }
    }
}
