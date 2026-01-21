using System;
using System.Collections.Generic;

namespace Basic
{
    public class State<T> : Element where T : Enum
    {
        public enum Data
        {
            Current,
            Previous,
        }

        public enum Event
        {
            Changed,
        }

        private readonly Dictionary<T, IState<T>> _states = new();

        public IState<T> Current => data.Get<IState<T>>(Data.Current);
        public IState<T> Previous => data.Get<IState<T>>(Data.Previous);
        
        // 便捷方法
        public T CurrentKey => Current != null ? Current.Key : (T)Enum.ToObject(typeof(T), 0);
        public bool Is(T state) => Current != null && Current.Key.Equals(state);

        public void Register(params IState<T>[] states)
        {
            foreach (var state in states)
            {
                if (state == null || _states.ContainsKey(state.Key)) continue;
                _states[state.Key] = state;
            }
        }

        public IState<T> Get(T key) => _states.TryGetValue(key, out var state) ? state : null;

        public void Change(T key, object context = null)
        {
            if (!_states.TryGetValue(key, out var next)) return;
            if (Current != null && !Current.CanExit()) return;

            var prev = Current;
            prev?.Exit(context);

            data.Change(Data.Previous, prev);
            data.Change(Data.Current, next);

            next.Enter(context);

            monitor.Fire(Event.Changed, prev, next);
        }
        public void Update(object context = null)
        {
            Current?.Update(context);
        }

        public void OnEnter(T key, Monitor.Function handler)
        {
            if (Get(key) is StateBase<T> state)
                state.monitor.Register(StateBase<T>.Event.Entered, handler);
        }

        public void OnExit(T key, Monitor.Function handler)
        {
            if (Get(key) is StateBase<T> state)
                state.monitor.Register(StateBase<T>.Event.Exited, handler);
        }

        public void OnUpdated(T key, Monitor.Function handler)
        {
            if (Get(key) is StateBase<T> state)
                state.monitor.Register(StateBase<T>.Event.Updated, handler);
        }
    }

    public interface IState<T> where T : Enum
    {
        T Key { get; }

        void Enter(object context = null);
        void Exit(object context = null);
        void Update(object context = null);
        bool CanExit();
    }

    public abstract class StateBase<T> : Element, IState<T> where T : Enum
    {
        public enum Event
        {
            Entered,
            Exited,
            Updated
        }

        public abstract T Key { get; }

        public virtual void Enter(object context = null)
        {
            OnEnter(context);
            monitor.Fire(Event.Entered, context);
        }

        public virtual void Exit(object context = null)
        {
            OnExit(context);
            monitor.Fire(Event.Exited, context);
        }
        public virtual void Update(object context = null)
        {
            monitor.Fire(Event.Updated, context);
        }

        public virtual bool CanExit() => true;

        protected virtual void OnEnter(object context) { }
        protected virtual void OnExit(object context) { }
    }
}
