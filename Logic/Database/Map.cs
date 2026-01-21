using MessagePack;
using System;
using System.Collections.Generic;

namespace Logic.Database
{
    [MessagePackObject]
    public class Map 
    {
        [Key(0)] public int id;
        [Key(1)] public int gid;
        [Key(2)] public int[] pos;
        [Key(4)] public Dictionary<int, List<int>> shortest;
        [Key(5)] public int[] teleport; 

        public Map() { }
   
        public Map(int id, int gid, int[] pos,  Dictionary<int, List<int>> shortest)
        {
            this.id = id;
            this.gid = gid;
            this.pos = pos;
            this.shortest = shortest;
        }
    }
  
}

