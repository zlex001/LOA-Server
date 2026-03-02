using Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Basic
{
    public partial class Ability : Basic.Manager
    {
        public O Load<C, O>(int id) where O : Element where C : global::Data.Config.Ability
        {
            C config = global::Data.Config.Agent.Instance.GetCached<C>(id);
            if (config == null)
            {
                config = global::Data.Config.Agent.Instance.Content.Get<C>(c => c.Id == id);
            }
            return Create<O>(config);
        }
   
        public O Load<O>(params object[] args) where O : Element
        {
            return Create<O>(args);
        }

        public O Load<C, O>(int id, params object[] args) where O : Element where C : global::Data.Config.Ability
        {
            C config = global::Data.Config.Agent.Instance.GetCached<C>(id);
            if (config == null)
            {
                config = global::Data.Config.Agent.Instance.Content.Get<C>(c => c.Id == id);
            }
            
            object[] combinedArgs = new object[args.Length + 1];
            combinedArgs[0] = config;
            Array.Copy(args, 0, combinedArgs, 1, args.Length);
            return Create<O>(combinedArgs);
        }
 
        public void LoadByRow<T>(string path) where T : Element
        {
            List<Dictionary<string, object>> dicts;
            if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                dicts = Utils.Csv.LoadAsRows(path);
            }
            else
            {
                dicts = Utils.Excel.LoadAsRows(path);
            }
            foreach (var dict in dicts)
            {
                Create<T>(dict); 
            }
        }
   
        public void LoadByRow<T>(string path, string name) where T : Element
        {
            List<Dictionary<string, object>> dicts = Utils.Excel.LoadAsRows(path, name);
            foreach (var dict in dicts)
            {
                Create<T>(dict); 
            }
        }

        public void LoadByCell<T>(string path) where T : Element
        {
            List<List<object>> datass;
            if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                datass = Utils.Csv.LoadAsCells(path);
            }
            else
            {
                datass = Utils.Excel.LoadAsCells(path);
            }
            for (int y = 1; y < datass.Count; y++)
            {
                List<object> datas = datass[y]; 
                for (int x = 1; x < datas.Count; x++)
                {
                    if (!string.IsNullOrEmpty(datas[x]?.ToString()))
                    {
                        Create<T>(datas[x], x, y);
                    }
                }
            }
        }
    }
}


