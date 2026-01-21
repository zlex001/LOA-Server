using System;
using System.Collections.Generic;

namespace Basic
{
    public class Store
    {
        public Dictionary<Enum, Delegate> min = new Dictionary<Enum, Delegate>();

        public Dictionary<Enum, Delegate> max = new Dictionary<Enum, Delegate>();

        public Dictionary<Enum, object> raw = new Dictionary<Enum, object>();

        public Monitor before = new Monitor();

        public Monitor after = new Monitor();

        public virtual void Change<T>(Enum e, T v, Element element = null)
        {
            // 获取当前值
            T o = Get<T>(e);

            if (!before._condition.ContainsKey(e) || before._condition[e](o, v))
            {
                // 检查最小值约束
                if (v is IComparable comparableV)
                {
                    if (min.TryGetValue(e, out var minDelegate) && minDelegate is Func<T> minFunc)
                    {
                        T minValue = minFunc();
                        if (minValue is IComparable comparableMin && comparableV.CompareTo(comparableMin) < 0)
                        {
                            v = minValue;
                        }
                    }

                    // 检查最大值约束
                    if (max.TryGetValue(e, out var maxDelegate) && maxDelegate is Func<T> maxFunc)
                    {
                        T maxValue = maxFunc();
                        if (maxValue is IComparable comparableMax && comparableV.CompareTo(comparableMax) > 0)
                        {
                            v = maxValue;
                        }
                    }
                }

                // 更新数据并触发事件
                before.Fire(e, o, v, element);
                raw[e] = v;
                after.Fire(e, v, element);
            }
        }

        public T Get<T>(Enum e) => raw.ContainsKey(e) && raw[e] is T t ? t : default;

        public T GetLoss<T>(Enum e) where T : struct, IComparable<T>
        {
            T maxValue = GetMax<T>(e);
            T currentValue = Get<T>(e);
            dynamic maxDynamic = maxValue;
            dynamic currentDynamic = currentValue;
            return maxDynamic - currentDynamic;
        }

        public double GetRatio<T>(Enum e) where T : struct, IComparable<T>
        {
            T maxValue = GetMax<T>(e);
            T currentValue = Get<T>(e);
            if ((dynamic)maxValue == 0)
            {
                return 0;
            }
            return (double)(dynamic)currentValue / (double)(dynamic)maxValue;
        }

        public bool IsFull<T>(Enum e) where T : IComparable<T>
        {
            T maxValue = GetMax<T>(e);
            if (!raw.ContainsKey(e))
            {
                return false;
            }
            T currentValue = Get<T>(e);
            return currentValue.CompareTo(maxValue) >= 0;
        }

        public T GetMax<T>(Enum e)
        {
            if (max.TryGetValue(e, out var maxDelegate) && maxDelegate is Func<T> maxFunc)
            {
                return maxFunc();
            }
            return default;
        }

        public T GetMin<T>(Enum e)
        {
            if (min.TryGetValue(e, out var minDelegate) && minDelegate is Func<T> minFunc)
            {
                return minFunc();
            }
            return default;
        }

        public bool IsEmpty<T>(Enum e) where T : IComparable<T>
        {
            T minValue = GetMin<T>(e);
            if (!raw.ContainsKey(e))
            {
                return false;
            }
            T currentValue = Get<T>(e);
            return currentValue.CompareTo(minValue) <= 0;
        }

        public void Full<T>(Enum e)
        {
            if (max.TryGetValue(e, out var maxDelegate) && maxDelegate is Func<T> maxFunc)
            {
                T maxValue = maxFunc();
                Change(e, maxValue);
            }
        }
    }
}
