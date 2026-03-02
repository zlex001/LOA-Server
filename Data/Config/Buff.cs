namespace Data.Config
{
    public class Buff : Ability, IName
    {
        public int Name { get; set; }
        public double Interval { get; set; }
        public Dictionary<string, int> Broadcasts { get; set; } = new();

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            Name = Get<int>(dict, "name");
            
            string intervalStr = Get<string>(dict, "interval");
            Interval = intervalStr == "-" ? -1 : double.TryParse(intervalStr, out var interval) ? interval : -1;
            
            string description = Get<string>(dict, "description");
            if (!string.IsNullOrEmpty(description) && description != "-")
            {
                foreach (string part in description.Split(';'))
                {
                    string[] pair = part.Split(':');
                    if (pair.Length == 2)
                    {
                        string eventName = pair[0].Trim();
                        if (int.TryParse(pair[1].Trim(), out int langKey))
                        {
                            Broadcasts[eventName] = langKey;
                        }
                    }
                }
            }
        }
    }
}

