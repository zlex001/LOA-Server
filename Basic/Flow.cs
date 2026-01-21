using System;
using Basic;

namespace Basic
{
    public class Flow<T> : Element where T : Enum
    {
        public T Current { get; private set; }
        public void Register(T step, Monitor.Function handler)
        {
            monitor.Register(step as Enum, handler);
        }
        public void Goto(T step, params object[] args)
        {
            Current = step;
            monitor.Fire(step as Enum, args); 
        }
    }
}
