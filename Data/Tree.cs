using System;
using System.Collections.Generic;

namespace Basic
{
    public class Tree : global::Data.Ability
    {
        public class Node : Manager
        {
            public enum Data
            {
                Name,
            }
            public List<Node> Children => Content.Gets<Node>();
            public string Name
            {
                get => data.Get<string>(Data.Name);
                set => data.Change(Data.Name, value);
            }
            public override void Init(params object[] args)
            {
                string name = (string)args[0];
                data.raw[Data.Name] = name;
            }
            public void AddChild(Node childNode)
            {
                Content.objs.Add(childNode);
            }
        }

        public enum Data
        {
            Root,
        }
        public Node RootNode { get => data.Get<Node>(Data.Root); set => data.Change(Data.Root, value); }
        public override void Init(params object[] args)
        {
            Node root = (Node)args[0];
            RootNode = root;
        }
        public List<Node> GetRootChildren()
        {
            return RootNode.Children;
        }
        private void PrintTree(Node node, string indent)
        {
            foreach (var child in node.Children)
            {
                PrintTree(child, indent + "  ");
            }
        }
        public void Print()
        {
            PrintTree(RootNode, "");
        }
    }
}
