using Newtonsoft.Json;
using System.Linq;

namespace Data.Database
{
    [Serializable]
    public class Part
    {
        public global::Data.Part.Types Type { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }

        public Dictionary<string, object> ToDictionary => new()
        {
            ["Type"] = (int)Type,
            ["Hp"] = Hp,
            ["MaxHp"] = MaxHp,
        };
    }

}
