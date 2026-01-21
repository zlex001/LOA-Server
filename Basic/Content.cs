using System;
using System.Collections.Generic;
using System.Linq;


namespace Basic
{
    public class Content
    {
        public Monitor Add { get; } = new Monitor();
        public Monitor Remove { get; } = new Monitor();
        public List<object> objs = new List<object>();
        readonly Random random = new Random();

        private Dictionary<Type, List<object>> _typeCache = new Dictionary<Type, List<object>>();
        private bool _cacheValid = true;

        private List<object> GetOrCreateTypeCache(Type type)
        {
            if (!_cacheValid)
            {
                _typeCache.Clear();
                _cacheValid = true;
            }

            if (!_typeCache.TryGetValue(type, out var cachedList))
            {
                cachedList = new List<object>();
                for (int i = 0; i < objs.Count; i++)
                {
                    if (type.IsInstanceOfType(objs[i]))
                    {
                        cachedList.Add(objs[i]);
                    }
                }
                _typeCache[type] = cachedList;
            }

            return cachedList;
        }

        public int Index(object obj)
        {
            return objs.IndexOf(obj);
        }

        public List<T> Gets<T>() where T : class
        {
            Type type = typeof(T);
            var cachedList = GetOrCreateTypeCache(type);
            var result = new List<T>(cachedList.Count);
            for (int i = 0; i < cachedList.Count; i++)
            {
                result.Add((T)cachedList[i]);
            }
            return result;
        }

        public T Get<T>() where T : class
        {
            Type type = typeof(T);
            var cachedList = GetOrCreateTypeCache(type);

            return cachedList.Count > 0 ? (T)cachedList[0] : null;
        }

        public T RandomGet<T>() where T : class
        {
            Type type = typeof(T);
            var cachedList = GetOrCreateTypeCache(type);

            return cachedList.Count > 0 ? (T)cachedList[random.Next(0, cachedList.Count)] : null;
        }

        public T RandomGet<T>(Func<T, bool> predicate) where T : class
        {
            if (predicate == null)
            {
                return RandomGet<T>();
            }
            var cachedList = GetOrCreateTypeCache(typeof(T));
            var matches = new List<T>();
            for (int i = 0; i < cachedList.Count; i++)
            {
                if (cachedList[i] is T typedObj && predicate(typedObj))
                {
                    matches.Add(typedObj);
                }
            }
            return matches.Count > 0 ? matches[random.Next(matches.Count)] : null;
        }


        public T Get<T>(Func<T, bool> predicate) where T : class
        {
            if (predicate == null)
            {
                return Get<T>();
            }
            var cachedList = GetOrCreateTypeCache(typeof(T));
            for (int i = 0; i < cachedList.Count; i++)
            {
                if (cachedList[i] is T typedObj && predicate(typedObj))
                {
                    return typedObj;
                }
            }
            return null;
        }

        public List<T> Gets<T>(Func<T, bool> predicate) where T : class
        {
            if (predicate == null)
            {
                return Gets<T>();
            }
            var cachedList = GetOrCreateTypeCache(typeof(T));
            var result = new List<T>();
            for (int i = 0; i < cachedList.Count; i++)
            {
                if (cachedList[i] is T typedObj && predicate(typedObj))
                {
                    result.Add(typedObj);
                }
            }
            return result;
        }
        public bool Has<T>() where T : class
        {
            var cachedList = GetOrCreateTypeCache(typeof(T));
            return cachedList.Count > 0;
        }

        public bool Has<T>(Func<T, bool> predicate) where T : class
        {
            if (predicate == null)
            {
                return Has<T>();
            }
            var cachedList = GetOrCreateTypeCache(typeof(T));
            for (int i = 0; i < cachedList.Count; i++)
            {
                if (cachedList[i] is T typedObj && predicate(typedObj))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Has<T>(Func<T, bool> predicate, out T result) where T : class
        {
            result = Get<T>(predicate);
            return result != null;
        }

        public int Count<T>() where T : class
        {
            return GetOrCreateTypeCache(typeof(T)).Count;
        }

        public int Count<T>(Func<T, bool> predicate) where T : class
        {
            if (predicate == null)
            {
                return Count<T>();
            }
            var cachedList = GetOrCreateTypeCache(typeof(T));
            int count = 0;
            for (int i = 0; i < cachedList.Count; i++)
            {
                if (cachedList[i] is T typedObj && predicate(typedObj))
                {
                    count++;
                }
            }
            return count;
        }


        public bool Has(object o)
        {
            return objs.Contains(o);
        }

        // 添加对象时更新缓存
        internal void ObjectAdded(object obj)
        {
            if (_cacheValid)
            {
                var types = _typeCache.Keys.ToArray();
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i].IsInstanceOfType(obj))
                    {
                        _typeCache[types[i]].Add(obj);
                    }
                }
            }
        }

        // 移除对象时更新缓存
        internal void ObjectRemoved(object obj)
        {
            if (_cacheValid)
            {
                var lists = _typeCache.Values.ToArray();
                for (int i = 0; i < lists.Length; i++)
                {
                    lists[i].Remove(obj);
                }
            }
        }
        public TChild SafeRandomNested<TParent, TChild>(
    Func<TParent, bool> parentPredicate,
    Func<TChild, bool> childPredicate)
    where TParent : class
    where TChild : class
        {
            // 从当前 Content 中随机选择一个满足条件的 TParent
            var parent = this.RandomGet(parentPredicate);
            if (parent == null) return null;

            // 假定每个 TParent 都继承自 Element，拥有 Content 字段
            if (parent is not Manager manager)
                return null;

            // 从该元素的 Content 中获取子对象
            return manager.Content.RandomGet(childPredicate);
        }
    }
}
