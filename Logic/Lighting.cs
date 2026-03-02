using Data;
using System;

namespace Logic
{
    public class Lighting
    {
        public static Lighting Instance => instance ??= new Lighting();
        private static Lighting instance;

        public void Init()
        {
        }

        public string ApplyWorldLighting(string baseColor)
        {
            var period = Time.Agent.Instance.Current.Period;
            
            double brightness = 1.0;
            double tintR = 1.0, tintG = 1.0, tintB = 1.0;

            GetLightingParams(period, out brightness, out tintR, out tintG, out tintB);

            return AdjustColor(baseColor, brightness, tintR, tintG, tintB);
        }

        private void GetLightingParams(Time.Agent.Period period, out double brightness, out double tintR, out double tintG, out double tintB)
        {
            switch (period)
            {
                case Time.Agent.Period.Dawn:
                    brightness = global::Data.Constant.LightingDawnBrightness;
                    tintR = global::Data.Constant.LightingDawnColorTintR;
                    tintG = global::Data.Constant.LightingDawnColorTintG;
                    tintB = global::Data.Constant.LightingDawnColorTintB;
                    break;
                case Time.Agent.Period.Morning:
                    brightness = global::Data.Constant.LightingMorningBrightness;
                    tintR = global::Data.Constant.LightingMorningColorTintR;
                    tintG = global::Data.Constant.LightingMorningColorTintG;
                    tintB = global::Data.Constant.LightingMorningColorTintB;
                    break;
                case Time.Agent.Period.Afternoon:
                    brightness = global::Data.Constant.LightingAfternoonBrightness;
                    tintR = global::Data.Constant.LightingAfternoonColorTintR;
                    tintG = global::Data.Constant.LightingAfternoonColorTintG;
                    tintB = global::Data.Constant.LightingAfternoonColorTintB;
                    break;
                case Time.Agent.Period.Evening:
                    brightness = global::Data.Constant.LightingEveningBrightness;
                    tintR = global::Data.Constant.LightingEveningColorTintR;
                    tintG = global::Data.Constant.LightingEveningColorTintG;
                    tintB = global::Data.Constant.LightingEveningColorTintB;
                    break;
                case Time.Agent.Period.Night:
                    brightness = global::Data.Constant.LightingNightBrightness;
                    tintR = global::Data.Constant.LightingNightColorTintR;
                    tintG = global::Data.Constant.LightingNightColorTintG;
                    tintB = global::Data.Constant.LightingNightColorTintB;
                    break;
                default:
                    brightness = 1.0;
                    tintR = tintG = tintB = 1.0;
                    break;
            }
        }

        private string AdjustColor(string hexColor, double brightness, double tintR, double tintG, double tintB)
        {
            if (string.IsNullOrEmpty(hexColor) || hexColor.Length < 7) return hexColor;

            string hex = hexColor.TrimStart('#');
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            string alpha = hex.Length >= 8 ? hex.Substring(6, 2) : "FF";

            r = Clamp((int)(r * brightness * tintR), 0, 255);
            g = Clamp((int)(g * brightness * tintG), 0, 255);
            b = Clamp((int)(b * brightness * tintB), 0, 255);

            return $"#{r:X2}{g:X2}{b:X2}{alpha}";
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public void UpdateAllPlayers()
        {
            foreach (Player player in global::Data.Agent.Instance.Content.Gets<Player>())
            {
                if (player.Map?.Scene != null)
                {
                    var scene = CreateSceneWithLighting(player, player.Map);
                    Net.Tcp.Instance.Send(player, scene);
                }
            }
        }

        private Net.Protocol.Scene CreateSceneWithLighting(Player player, global::Data.Map map)
        {
            var pos = map.Database.pos;
            var maps = new System.Collections.Generic.List<Net.Protocol.Map>();
            string sceneName = "";
            
            var scene = map?.Scene;
            if (scene != null)
            {
                sceneName = Logic.Text.Agent.Instance.Get(scene.Config.Name, player);
                
                foreach (global::Data.Map m in scene.Content.Gets<global::Data.Map>(m => !(m.Copy != null)))
                {
                    if (m != null)
                    {
                        var name = Text.Name.Map(m, player);
                        var mapPos = m.Database.pos;
                        var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(m.Type);
                        var colorWithLighting = ApplyWorldLighting(baseColor);
                        maps.Add(new Net.Protocol.Map(name, mapPos, colorWithLighting));
                    }
                }
            }
            else
            {
                int startX = map.Database.pos[0] - 1;
                int endX = map.Database.pos[0] + 1;
                int startY = map.Database.pos[1] - 1;
                int endY = map.Database.pos[1] + 1;
                
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        string name = (x == map.Database.pos[0] && y == map.Database.pos[1]) ? 
                            (map.Scene != null ? 
                                Logic.Text.Agent.Instance.Get(map.Scene.Config.Name, player) 
                                : "") 
                            : " ";
                        var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(global::Data.Map.Types.Default);
                        var colorWithLighting = ApplyWorldLighting(baseColor);
                        maps.Add(new Net.Protocol.Map(name, new int[] { x, y, map.Database.pos[2] }, colorWithLighting));
                    }
                }
            }
            
            return new Net.Protocol.Scene(pos, maps, sceneName);
        }
    }
}

