using MessagePack;
using System;
using System.Collections.Generic;

namespace Logic.Database
{

    [MessagePackObject]
    public class Scene
    {
        [Key(0)] public int id;
        [Key(1)] public Dictionary<int, List<int>> shortest = new Dictionary<int, List<int>>();
        [Key(2)] public List<Map> maps=new List<Map>();

        public Scene() { }

        public Scene(int id, List<int> exit)
        {
            this.id = id;
        }

    }

}

