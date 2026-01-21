
using System;
using Newtonsoft.Json;

namespace Basic
{
    [Serializable]
    public  class Element
    {

        public enum Data
        {
            Parent,
        }

        [JsonIgnore]
        public Store data = new Store();
        [JsonIgnore]
        public Random random = new Random();
        [JsonIgnore]
        public Manager Parent { get => data.Get<Manager>(Data.Parent); set => data.Change(Data.Parent, value); } 
        [JsonIgnore]
        public Monitor monitor = new Monitor();
        public void Destroy()
        {
            Release();
            Parent?.Remove(this);
            if (Parent != null)
            {
                Parent = null;
            }
        }

        public virtual void Init(params object[] args) 
        {
        
        }
        public virtual void Release()
        {
            monitor.ClearAll();
            data.before.ClearAll();
            data.after.ClearAll();
        }
    }
}
