using System;
using System.Collections.Generic;
using Data;
using Utils;

namespace Logic
{
    public abstract class Singleton<T> where T : new()
    {
        private static T instance;
        public static T Instance => instance ??= new T();
    }

    public abstract class Agent<T> : Singleton<T> where T : new()
    {
        private readonly Dictionary<(Basic.Monitor monitor, Enum key), Basic.Monitor.Function> callbacks = new();

        protected void Register<TContext>(Basic.Monitor monitor, Enum key, TContext context, Action<TContext, object[]> handler)
        {
            var callback = (Basic.Monitor.Function)(args => handler(context, args));
            monitor.Register(key, callback);
            callbacks[(monitor, key)] = callback;
        }

        protected void Unregister(Basic.Monitor monitor, Enum key)
        {
            if (callbacks.TryGetValue((monitor, key), out var callback))
            {
                monitor.Unregister(key, callback);
                callbacks.Remove((monitor, key));
            }
        }
    }

    public abstract class TagFunction<TFunction, TTarget, TUser> : Agent<TFunction> where TFunction : TagFunction<TFunction, TTarget, TUser>, new()
    {
        protected readonly Dictionary<string, Action<TTarget, TUser>> Handlers = new();

        public abstract void Init();

        public virtual void Do(TTarget target, TUser user)
        {
            if (target is Data.ITag provider)
            {
                foreach (var tag in provider.GetTags())
                {
                    if (Handlers.TryGetValue(tag, out var action))
                    {
                        action(target, user);
                        break;
                    }
                }
            }
        }

    }
}
