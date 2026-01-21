using Basic;

namespace Logic.Validation
{
    public class Error : Basic.Element
    {
        public string Category { get; set; }
        public string Module { get; set; }
        public string ConfigType { get; set; }
        public string Value { get; set; }
        public string Details { get; set; }

        public override void Init(params object[] args)
        {
            if (args.Length >= 4)
            {
                Category = args[0]?.ToString();
                Module = args[1]?.ToString();
                ConfigType = args[2]?.ToString();
                Value = args[3]?.ToString();
                Details = args.Length >= 5 ? args[4]?.ToString() : null;
            }
        }
    }
}