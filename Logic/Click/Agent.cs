using System;
using Data;
using Utils;

namespace Logic.Click
{
    public class Agent
    {
 
        public static void Init()
        {
            global::Data.Agent.Instance.monitor.Register(Player.Click.Map, Map.On);
            global::Data.Agent.Instance.monitor.Register(Player.Click.Character, Character.On);
            global::Data.Agent.Instance.monitor.Register(Player.Click.Scene, Scene.On);
        }
    }
}
