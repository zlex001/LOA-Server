using Basic;
using System;
using System.Collections.Generic;

namespace Logic
{


    public partial class Check : Basic.Ability
    {
        public enum Types
        {
            Error,
            Warning,
        }
        public enum Data
        {
            Error,
            Warning
        }

        public enum Event
        {
            Execute
        }

        public List<string> Error { get => data.Get<List<string>>(Check.Data.Error); set => data.Change(Check.Data.Error, value); }
        public List<string> Warning { get => data.Get<List<string>>(Check.Data.Warning); set => data.Change(Check.Data.Warning, value); }

        public Check()
        {
            data.raw[Data.Error] = new List<string>();
            data.raw[Data.Warning] = new List<string>();
        }

        public void Report(Types type, List<Dictionary<string, object>> data, string excel, string reason)
        {
            Utils.Csv.SaveByRows(data, System.IO.Path.Combine(Utils.Paths.DesignData, $"{excel}.csv"));
            if (Equals(type, Types.Error))
            {
                Error.Add($"{reason}���� {data.Count} �������鿴��{excel}������");
            }
            else if (Equals(type, Types.Warning))
            {
                Warning.Add($"{reason}���� {data.Count} �������鿴��{excel}������");
            }
        }

        public void Start()
        {
            monitor.Fire(Check.Event.Execute);
            ShowWarningsAndErrors();
        }

        private void ShowWarningsAndErrors()
        {
            if (Error.Count > 0)
            {
                Console.ReadLine();
            }
        }
    }
}

