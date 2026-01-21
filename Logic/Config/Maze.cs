using System.Collections.Generic;
using System.Linq;

namespace Logic.Config
{
    public class Maze : Ability
    {
        public int width;                          // 迷宫宽度
        public int height;                         // 迷宫高度
        public float fillRate;                     // 初始墙体密度，0~1
        public int iterations;                     // 元胞自动机迭代次数
        public string fixedRooms;                  // 固定房型及数量的JSON字符串，如：{"boss_room":1,"trap_room":3}
        public string roomPool;                    // 随机房间池及权重的JSON字符串，如：{"normal_1":0.6,"normal_2":0.4}

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<int>(dict, "id");
            width = Get<int>(dict, "width");
            height = Get<int>(dict, "height");
            fillRate = Get<float>(dict, "fillRate");
            iterations = Get<int>(dict, "iterations");
            fixedRooms = Get<string>(dict, "fixedRooms");
            roomPool = Get<string>(dict, "roomPool");
        }

    }
}