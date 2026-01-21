using System;
using Logic;
using Utils;

namespace Domain.Click
{
    public class Agent
    {
 
        public static void Init()
        {
            Logic.Agent.Instance.monitor.Register(Player.Click.Map, Map.On);
            Logic.Agent.Instance.monitor.Register(Player.Click.Character, Character.On);
            Logic.Agent.Instance.monitor.Register(Player.Click.Scene, Scene.On);
        }
    }
}
