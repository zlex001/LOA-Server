using Data;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Click
{
    public class Scene
    {
        public static void On(params object[] args)
        {
            global::Data.Player player = (global::Data.Player)args[0];
            int[] scenePos = (int[])args[1];
            
            if (scenePos == null || scenePos.Length < 3)
            {
                Utils.Debug.Log.Warning("CLICK", $"Invalid scene position received");
                return;
            }
            
            var sceneInfo = global::Data.Design.World.SceneCoordinates
                .FirstOrDefault(coord => coord.pos != null 
                    && coord.pos.Length >= 3 
                    && coord.pos[0] == scenePos[0] 
                    && coord.pos[1] == scenePos[1] 
                    && coord.pos[2] == scenePos[2]);
            
            if (sceneInfo.sceneCid == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene not found at position [{scenePos[0]}, {scenePos[1]}, {scenePos[2]}]");
                return;
            }
            
            var sceneDesign = global::Data.Design.Agent.Instance.Content.Get<global::Data.Design.Scene>(s => s.cid == sceneInfo.sceneCid);
            if (sceneDesign == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene design not found for cid: {sceneInfo.sceneCid}");
                return;
            }
            
            var scene = global::Data.Agent.Instance.Content.Get<global::Data.Scene>(s => s.Config.Id == sceneDesign.id);
            if (scene == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene instance not found for id: {sceneDesign.id}");
                return;
            }
            
            var representativeMap = scene.Content.Gets<global::Data.Map>().FirstOrDefault();
            if (representativeMap == null)
            {
                Utils.Debug.Log.Warning("CLICK", $"Scene has no maps: {sceneInfo.sceneCid}");
                return;
            }
            
            var sceneProtocol = CreateScene(player, representativeMap);
            Net.Tcp.Instance.Send(player, sceneProtocol);
        }
        
        private static Net.Protocol.Scene CreateScene(global::Data.Player player, global::Data.Map map)
        {
            var pos = map.Database.pos;
            var maps = new List<Net.Protocol.Map>();
            string sceneName = "";
            
            var scene = map?.Scene;
            if (scene != null)
            {
                sceneName = Logic.Text.Agent.Instance.Get(scene.Config.Name, player);
                
                foreach (global::Data.Map m in scene.Content.Gets<global::Data.Map>(m => !(m.Copy != null)))
                {
                    if (m != null)
                    {
                        var name = Logic.Text.Name.Map(m, player);
                        var mapPos = m.Database.pos;
                        var baseColor = Net.Protocol.MapColorHelper.GetMapTypeColor(m.Type);
                        var color = Lighting.Instance.ApplyWorldLighting(baseColor);
                        maps.Add(new Net.Protocol.Map(name, mapPos, color));
                    }
                }
            }
            
            return new Net.Protocol.Scene(pos, maps, sceneName);
        }
    }
}

