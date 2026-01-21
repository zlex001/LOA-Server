using System;
using System.Collections.Generic;

namespace Basic
{
    [Serializable]
    public class Data : Element
    {
        public T Get<T>(Dictionary<string, object> dict, string key) where T : IConvertible
        {
            if (dict != null && dict.TryGetValue(key, out object value) && !string.IsNullOrEmpty(value?.ToString()))
            {
                var targetType = typeof(T);
                
                if (targetType.IsEnum)
                {
                    if (value is string strValue)
                    {
                        if (int.TryParse(strValue, out int intValue))
                        {
                            return (T)Enum.ToObject(targetType, intValue);
                        }
                        else
                        {
                            return (T)Enum.Parse(targetType, strValue);
                        }
                    }
                    else
                    {
                        return (T)Enum.ToObject(targetType, value);
                    }
                }
                
                return (T)System.Convert.ChangeType(value, targetType);
            }
            return default;
        }
        public virtual Dictionary<string, object> ToDictionary => new Dictionary<string, object>();
    }
}
