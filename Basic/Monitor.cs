using System;
using System.Collections.Generic;

namespace Basic
{
    public class Monitor
    {
        #region Delegates
        public delegate int Int(params object[] args);
        public delegate object[] Object(params object[] args);
        public delegate void Function(params object[] args);

        public delegate bool Condition(params object[] args);
        public delegate double Double(params object[] args);
        public delegate string String(params object[] args);
        #endregion

        #region Fields
        public Dictionary<Enum, Object> _object = new Dictionary<Enum, Object>();
        public Dictionary<Enum, Function> enumFunc = new Dictionary<Enum, Function>();
        public Dictionary<Type, Function> typeFunc = new Dictionary<Type, Function>();

        public Dictionary<Enum, Condition> _condition = new Dictionary<Enum, Condition>();
        public Dictionary<Enum, Double> _double = new Dictionary<Enum, Double>();
        public Dictionary<Enum, String> _string = new Dictionary<Enum, String>();
        #endregion

        #region Double Operations
        public void Register(Enum key, Double e)
        {
            _double[key] = e;
        }

        public double GetDouble(Enum key, params object[] args)
        {
            if (_double.TryGetValue(key, out var current))
            {
                return current(args);
            }
            return default;
        }
        #endregion

        #region String Operations
        public void Register(Enum key, String e)
        {
            _string[key] = e;
        }

        public string GetString(Enum key, params object[] args)
        {
            if (_string.TryGetValue(key, out var current))
            {
                return current(args);
            }
            return default;
        }
        #endregion

        #region Object Operations
        public void Register(Enum key, Object e)
        {
            _object[key] = e;
        }

        public void Unregister(Enum key, Object e)
        {
            if (_object.TryGetValue(key, out var current) && current == e)
            {
                _object.Remove(key);
            }
        }

 

        public object[] Execution(Enum key, params object[] args)
        {
            if (_object.TryGetValue(key, out var current) && Check(key, args))
            {
                return current(args);
            }
            return null;
        }
        #endregion

        #region Enum Function Operations
        public void Register(Enum key, Function e)
        {
            if (enumFunc.ContainsKey(key))
            {
                enumFunc[key] += e;
            }
            else
            {
                enumFunc[key] = e;
            }
        }

        public void Unregister(Enum key, Function e)
        {
            if (enumFunc.TryGetValue(key, out var current))
            {
                enumFunc[key] = (Function)Delegate.Remove(current, e);
                if (enumFunc[key] == null)
                {
                    enumFunc.Remove(key);
                }
            }
        }

        public bool Has(Enum key, Function e)
        {
            if (enumFunc.TryGetValue(key, out var current))
            {
                foreach (Function func in current.GetInvocationList())
                {
                    if (func.Equals(e))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Fire(Enum key, params object[] args)
        {
            if (enumFunc.TryGetValue(key, out var current) && Check(key, args))
            {
                current(args);
            }
        }
        #endregion

        #region Condition Operations
        public void Register(Enum key, Condition e)
        {
            if (_condition.ContainsKey(key))
            {
                _condition[key] += e;
            }
            else
            {
                _condition[key] = e;
            }
        }

        public void Unregister(Enum key, Condition e)
        {
            if (_condition.TryGetValue(key, out var current))
            {
                _condition[key] = (Condition)Delegate.Remove(current, e);
                if (_condition[key] == null)
                {
                    _condition.Remove(key);
                }
            }
        }

        public bool Check(Enum key, params object[] args)
        {
            if (!_condition.TryGetValue(key, out var conditionFunc))
            {
                return true;
            }

            foreach (Condition cond in conditionFunc.GetInvocationList())
            {
                if (!cond(args))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region Type Function Operations
        public void Register(Type objType, Function e)
        {
            if (typeFunc.ContainsKey(objType))
            {
                typeFunc[objType] += e;
            }
            else
            {
                typeFunc[objType] = e;
            }
        }
        
        public void Unregister(Type objType, Function e)
        {
            if (typeFunc.TryGetValue(objType, out var current))
            {
                typeFunc[objType] = (Function)Delegate.Remove(current, e);

                if (typeFunc[objType] == null)
                {
                    typeFunc.Remove(objType);
                }
            }
        }

        public void Fire(Type objType, params object[] args)
        {
            var typeFuncCopy = new Dictionary<Type, Function>(typeFunc);
            foreach (var kvp in typeFuncCopy)
            {
                Type registeredType = kvp.Key;
                if (registeredType.IsAssignableFrom(objType)) 
                {
                    kvp.Value?.Invoke(args);
                }
            }
        }
        #endregion
        public void ClearAll()
        {
            _object.Clear();
            enumFunc.Clear();
            typeFunc.Clear();
            _condition.Clear();
            _double.Clear();
            _string.Clear();
        }
    }
}
