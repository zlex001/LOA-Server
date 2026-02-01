using NPOI.SS.Formula.Functions;
using Utils;

namespace Logic
{
    public class Map : Ability<Config.Map>
    {
        public enum Event
        {
            AfterAdd,
            AfterRemove,
            Transport,
            Arrived = 26739,
        }
        public enum Functions
        {
            Translate,
            Drop,
            Xiulian,
        }
        public enum Direction
        {
            East,
            South,
            West,
            North,
            Northeast,
            Southeast,
            Southwest,
            Northwest,
            Up,
            Down,
        }
        public enum Show
        {
            Land,
            Room,
            Seal,
            Hide,
        }
        public enum Types
        {
            Default = 0,
            Teleport = 51,
            Bank = 32,
            Guild = 31,
            Market = 33,
            FoodShop,       // 食品店
            PotionShop,     // 药品店
            MagicShop,      // 魔法店
            LightGearShop,  // 轻装店（含轻武器与轻甲）
            HeavyGearShop,   // 重装店（含重武器与重甲）

            Road = 100,          // 路
            Forest = 101,        // 树林
            Cave = 102,          // 洞穴
            Shore = 103,         // 岸边
            Slope = 104,         // 山坡
            Grass = 105,         // 草地
            Snow = 106,          // 雪地
            Swamp = 107,         // 沼泽
            Sand = 108,          // 沙地
            Bridge = 109,        // 桥
            Mountain = 110,      // 山地
            VegetableGarden = 111, // 菜园
            Farm = 112,          // 田园
            Orchard = 113,       // 果园
            HerbGarden = 114,    // 药园
            Wall = 115,          // 墙
            Room = 116,          // 房间
            Well = 117,          // 井
            Ranch = 118,         // 牧场
            Pond = 119,          // 池塘
            MazeEntrance = 120,  // 迷宫入口
            PoliceStation,       // 警察局
            Prison,              // 牢房
            Restaurant,          // 餐馆
            Hospital,            // 医院
            FarmerHouse,         // 农夫的家
            MinerHouse,          // 矿工的家
            Ruins,               // 遗迹
            RicePaddy,           // 稻田
            WheatField,          // 麦田
            MelonField,          // 瓜地
            VegetableField,      // 菜地
        }


        public Copy Copy { get; set; }
        public Scene Scene => Copy != null ? Copy.Parent as Scene : Parent as Scene;
        public Types Type { get; set; }
        public Database.Map Database { get; private set; }
        public override void Init(params object[] args)
        {
            Database = (Database.Map)args[0];
            Config = Logic.Config.Agent.Instance.Content.Get<Config.Map>(m => m.Id == Database.id);
            Type = Enum.TryParse<Types>(Config.type, true, out var type) ? type : Types.Default;
            Agent.Instance.Add(this);
        }
    }
}