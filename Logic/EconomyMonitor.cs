using Data;
using System.Text;

namespace Logic
{
    public class EconomyMonitor
    {
        private static EconomyMonitor instance;
        public static EconomyMonitor Instance { get { if (instance == null) { instance = new EconomyMonitor(); } return instance; } }

        public static bool Enabled { get; set; } = false;
        
        private readonly string logDirectory = System.IO.Path.Combine(Utils.Paths.Logs, "Economy");
        private int dayCounter = 0;

        public void Init()
        {
            if (!Enabled) return;
            
            if (!System.IO.Directory.Exists(logDirectory))
            {
                System.IO.Directory.CreateDirectory(logDirectory);
            }
        }
        
        public void OnDayChanged(int oldDay)
        {
            if (!Enabled) return;
            dayCounter++;
            GenerateReport();
        }

        private void GenerateReport()
        {
            if (!Enabled) return;
            
            var now = Time.Agent.Now;
            
            foreach (var scene in global::Data.Agent.Instance.Content.Gets<Scene>())
            {
                if (scene is Maze) continue;
                
                var data = CollectSceneData(scene);
                
                if (!data.HasShop) continue;
                
                WriteToCSV(scene, data);
            }
        }
        
        private class SceneEconomyData
        {
            public bool HasShop;
            public int CookMaterialCount;
            public int CookMaterialValue;
            public int CookProduct;
            public int CookProductValue;
            public int SewMaterialCount;
            public int SewMaterialValue;
            public int SewProduct;
            public int SewProductValue;
            public int ForgeMaterialCount;
            public int ForgeMaterialValue;
            public int ForgeProduct;
            public int ForgeProductValue;
        }
        
        private SceneEconomyData CollectSceneData(Scene scene)
        {
            var data = new SceneEconomyData();
            
            foreach (var map in scene.Content.Gets<Map>())
            {
                if (map.Type == Map.Types.Restaurant)
                {
                    data.HasShop = true;
                    GetShopData(map, out data.CookMaterialCount, out data.CookMaterialValue, out data.CookProduct, out data.CookProductValue);
                }
                else if (map.Type == Map.Types.LightGearShop)
                {
                    data.HasShop = true;
                    GetShopData(map, out data.SewMaterialCount, out data.SewMaterialValue, out data.SewProduct, out data.SewProductValue);
                }
                else if (map.Type == Map.Types.HeavyGearShop)
                {
                    data.HasShop = true;
                    GetShopData(map, out data.ForgeMaterialCount, out data.ForgeMaterialValue, out data.ForgeProduct, out data.ForgeProductValue);
                }
            }
            
            return data;
        }
        
        private void GetShopData(Map map, out int materialCount, out int materialValue, out int productCount, out int productValue)
        {
            materialCount = 0;
            materialValue = 0;
            productCount = 0;
            productValue = 0;
            
            var materialBox = Infrastructure.Agent.GetBox(map, Infrastructure.ContainerType.Material);
            if (materialBox != null)
            {
                foreach (var item in materialBox.Content.Gets<Item>())
                {
                    materialCount += item.Count;
                    materialValue += item.Config.value * item.Count;
                }
            }
            
            var productBox = Infrastructure.Agent.GetBox(map, Infrastructure.ContainerType.Product);
            if (productBox != null)
            {
                foreach (var item in productBox.Content.Gets<Item>())
                {
                    productCount += item.Count;
                    productValue += item.Config.value * item.Count;
                }
            }
        }
        
        private void WriteToCSV(Scene scene, SceneEconomyData data)
        {
            string fileName = $"{logDirectory}/Scene_{scene.Config.Id}_{GetSceneName(scene)}.csv";
            bool fileExists = System.IO.File.Exists(fileName);
            
            var sb = new StringBuilder();
            
            if (!fileExists)
            {
                sb.AppendLine("游戏日,烹饪店,轻装店,重装店");
            }
            
            string cookShop = $"{data.CookMaterialCount}[{data.CookMaterialValue}] - {data.CookProduct}[{data.CookProductValue}]";
            string sewShop = $"{data.SewMaterialCount}[{data.SewMaterialValue}] - {data.SewProduct}[{data.SewProductValue}]";
            string forgeShop = $"{data.ForgeMaterialCount}[{data.ForgeMaterialValue}] - {data.ForgeProduct}[{data.ForgeProductValue}]";
            
            sb.AppendLine($"{dayCounter},{cookShop},{sewShop},{forgeShop}");
            
            try
            {
                System.IO.File.AppendAllText(fileName, sb.ToString(), Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Utils.Debug.Log.Error("EconomyMonitor", $"CSV write failed: {ex.Message}");
            }
        }
        
        private string GetSceneName(Scene scene)
        {
            if (scene.Config == null) return $"未知({scene.Database?.id})";
            
            if (global::Data.Text.Instance.Multilingual.TryGetValue(scene.Config.Name, out var map) 
                && map.TryGetValue(global::Data.Text.Languages.ChineseSimplified, out var name))
            {
                return name;
            }
            
            return $"场景{scene.Config.Id}";
        }

        public void Shutdown()
        {
            if (!Enabled) return;
            
            GenerateReport();
        }
    }
}

