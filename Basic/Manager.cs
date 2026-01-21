namespace Basic
{
    public class Manager : Element
    {
        public enum Event
        {
            Addable,
            Removable,
        }
        public Manager() : base()
        {
            monitor.Register(Event.Addable, OnAddable);
            monitor.Register(Event.Removable, OnRemovable);
        }
        public Content Content { get; } = new Content();
        private bool OnAddable(params object[] args)
        {
            Element obj = (Element)args[0];
            return obj != null && !Content.objs.Contains(obj);
        }

        private bool OnRemovable(params object[] args)
        {
            Element obj = (Element)args[0];
            return obj != null && Content.objs.Contains(obj);
        }
   
        public T Create<T>(params object[] args) where T : Element
        {
        
            T obj = Activator.CreateInstance<T>();
            obj.Init(args);
            if (AddAsParent(obj))
            {
                return obj;
            }
            else
            {
                return null;
            }
        }
        public void Destroy<T>(T obj) where T : Element
        {
            obj.Release();
            Remove(obj);
        }

        public bool Add<T>(T obj) where T : Element
        {
            if (!Content.objs.Contains(obj))
            {
                if (monitor.Check(Event.Addable, obj))
                {
                    Content.objs.Add(obj);
                    Content.ObjectAdded(obj);
                    Content.Add.Fire(obj.GetType(), this, obj);
                    return true;
                  
                }
            }
            return false;
        }
        public bool AddAsParent<T>(T obj) where T : Element
        {
            if (!Content.objs.Contains(obj))
            {
                if (monitor.Check(Event.Addable, obj))
                {
                    if (obj.Parent == null || obj.Parent == this || obj.Parent.Remove(obj))
                    {
                        obj.Parent = this;
                        Content.objs.Add(obj);
                        Content.ObjectAdded(obj);
                        Content.Add.Fire(obj.GetType(),this, obj);
                        return true;
                    }
                }
            }
            return false;
        }
        public bool Remove<T>(T obj) where T : Element
        {
            if (monitor.Check(Event.Removable, obj))
            {
                Content.objs.Remove(obj);
                Content.ObjectRemoved(obj);
                Content.Remove.Fire(obj.GetType(),this, obj);
                
                // 修复：清理Parent关系，确保对象生命周期管理对称性
                if (obj.Parent == this)
                {
                    obj.Parent = null;
                }
                
                return true;
            }
            return false;
        }
        public void Remove<T>() where T : Element
        {
            if (Content.Has<T>())
            {
                foreach (T obj in Content.Gets<T>())
                {
                    Remove(obj);
                }
            }
        }
        public void Remove<T>(Func<T, bool> predicate) where T : Element
        {
            if (Content.Has<T>(predicate))
            {
                foreach (T obj in Content.Gets<T>(predicate))
                {
                    Remove(obj);
                }
            }

        }
        public void Remove<T>(Func<T, bool> predicate, int count) where T : Element
        {
            if (Content.Has<T>(predicate))
            {
                foreach (T obj in Content.Gets<T>(predicate).Take(count))
                {
                    Remove(obj);
                }
            }
        }
    }
}
