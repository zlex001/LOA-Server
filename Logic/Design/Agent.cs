using Basic;
using System.Text;
using System.Text.RegularExpressions;
using Utils;
using System.Collections.Generic;
using System.Linq;

namespace Logic.Design
{
    public class Agent : Basic.Ability
    {
        private static Agent instance;
        public static Agent Instance { get { if (instance == null) { instance = new Agent(); } return instance; } }
        public override void Init(params object[] args)
        {
            LoadByRow<Plot>($"{Utils.Paths.DesignData}/剧情.csv");
            LoadByRow<Life>($"{Utils.Paths.DesignData}/生物.csv");
            LoadByRow<Item>($"{Utils.Paths.DesignData}/道具.csv");
            LoadByRow<Skill>($"{Utils.Paths.DesignData}/技能.csv");
            LoadByRow<Movement>($"{Utils.Paths.DesignData}/招式.csv");
            LoadByRow<Buff>($"{Utils.Paths.DesignData}/效应.csv");
            LoadByRow<Scene>($"{Utils.Paths.DesignData}/场景.csv");
            LoadByRow<Map>($"{Utils.Paths.DesignData}/地图.csv");
            LoadByRow<BehaviorTree>($"{Utils.Paths.DesignData}/行为树.csv");
            LoadByRow<Mall>($"{Utils.Paths.DesignData}/商城.csv");
            LoadByRow<Maze>($"{Utils.Paths.DesignData}/迷宫.csv");
            LoadByRow<Multilingual>($"{Utils.Paths.DesignData}/语言.csv");
            LoadByRow<Dialogue>($"{Utils.Paths.DesignData}/对白.csv");
            
            Multilingual.Convert();
            Dialogue.Convert();
            Scene.Convert();
            Map.Convert();
            Life.Convert();
            Item.Convert();
            Skill.Convert();
            Plot.Convert();
            Movement.Convert();
            Buff.Convert();
            BehaviorTree.Convert();
            Mall.Convert();
            Maze.Convert();

            Logic.Validation.Agent.Instance.RunDesignValidation();
            
            Code.Init();
            World.Init();
        }
    }
}

