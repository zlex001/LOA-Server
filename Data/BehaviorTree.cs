using Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;



namespace Data
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BehaviorActionAttribute : Attribute
    {
        public int Id { get; }
        public BehaviorActionAttribute(int id)
        {
            Id = id;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class BehaviorConditionAttribute : Attribute
    {
        public int Id { get; }
        public BehaviorConditionAttribute(int id)
        {
            Id = id;
        }
    }
}

namespace Data.BehaviorTree
{
    /// <summary>
    /// Behavior tree node
    /// Supports Action, Condition, Sequence, Selector, Inverter node types
    /// </summary>
    public class Node : Core
    {
        /// <summary>
        /// Behavior tree node type enumeration
        /// Action: Execute specific actions
        /// Condition: Check state conditions
        /// Sequence: Execute all children in order, succeed if all succeed
        /// Selector: Try children in order, succeed if any succeeds
        /// Inverter: Invert result of first child
        /// </summary>
        public enum Types
        {
            Action,
            Condition,
            Sequence,
            Selector,
            Inverter
        }

        public global::Data.Config.BehaviorTree Config { get; set; }
        public List<Node> Children => Content.Gets<Node>();
        public Types Type { get; private set; }
        public Func<Character, bool> Handler { get; private set; }

        public Node()
        {
        }

        public override void Init(params object[] args)
        {
            Config = (global::Data.Config.BehaviorTree)args[0];
            Type = Enum.TryParse<Types>(Config.type, true, out var nodeType) ? nodeType : Types.Action;
            Handler = FindBehaviorFunction(Config.Id, Type);
        }

        public void BuildChildRelations()
        {
            foreach (var childId in Config.nodes)
            {
                var existingNode = global::Data.Agent.Instance.Content.Get<Node>(n => n.Config.Id == childId);
                if (existingNode != null)
                {
                    Add(existingNode);
                }
            }
        }

        private Func<Character, bool> FindBehaviorFunction(int id, Types nodeType)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var method in methods)
                        {
                            if (nodeType == Types.Action)
                            {
                                var actionAttr = method.GetCustomAttribute<BehaviorActionAttribute>();
                                if (actionAttr?.Id == id &&
                                    method.ReturnType == typeof(bool) &&
                                    method.GetParameters().Length == 1 &&
                                    method.GetParameters()[0].ParameterType == typeof(Character))
                                {
                                    return (Func<Character, bool>)Delegate.CreateDelegate(typeof(Func<Character, bool>), method);
                                }
                            }
                            else if (nodeType == Types.Condition)
                            {
                                var conditionAttr = method.GetCustomAttribute<BehaviorConditionAttribute>();
                                if (conditionAttr?.Id == id &&
                                    method.ReturnType == typeof(bool) &&
                                    method.GetParameters().Length == 1 &&
                                    method.GetParameters()[0].ParameterType == typeof(Character))
                                {
                                    return (Func<Character, bool>)Delegate.CreateDelegate(typeof(Func<Character, bool>), method);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Warning("BEHAVIOR_TREE", $"[Behavior Function Search Failed] Assembly: {assembly.GetName().Name}, NodeType: {nodeType}, Id: {id}, Exception: {ex.Message}");
                }
            }
            return null;
        }

        public bool Execute(Character character)
        {
            switch (Type)
            {
                case Types.Action:
                    if (character is Life life)
                    {
                        life.CurrentExecutingNode = this;
                    }
                    return Handler?.Invoke(character) ?? false;
                    
                case Types.Condition:
                    return Handler?.Invoke(character) ?? false;

                case Types.Sequence:
                    foreach (var child in Children)
                        if (!child.Execute(character))
                            return false;
                    return true;

                case Types.Selector:
                    foreach (var child in Children)
                        if (child.Execute(character))
                            return true;
                    return false;

                case Types.Inverter:
                    return Children.Count > 0 ? !Children[0].Execute(character) : false;

                default:
                    return false;
            }
        }

        public bool ExecuteWithDebug(Character character)
        {
            PrintColoredLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
            PrintColoredLog($"开始执行行为树: {DateTime.Now:HH:mm:ss.fff}", ConsoleColor.Cyan);
            var result = ExecuteWithTreeDebug(character, "", true, 0);
            PrintColoredLog($"执行完毕，总结果: {(result ? "成功" : "失败")}", result ? ConsoleColor.Green : ConsoleColor.Red);
            PrintColoredLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
            return result;
        }

        private bool ExecuteWithTreeDebug(Character character, string prefix, bool isRoot, int childIndex)
        {
            bool result;
            string nodeSymbol = GetTreeSymbol(isRoot, childIndex);
            string nodeName = GetName();
            string nodeTypeDisplay = GetNodeTypeDisplay();

            switch (Type)
            {
                case Types.Action:
                    if (character is Life life)
                    {
                        life.CurrentExecutingNode = this;
                    }
                    result = Handler?.Invoke(character) ?? false;
                    PrintNodeResult(prefix + nodeSymbol, nodeName, nodeTypeDisplay, result);
                    break;
                    
                case Types.Condition:
                    result = Handler?.Invoke(character) ?? false;
                    PrintNodeResult(prefix + nodeSymbol, nodeName, nodeTypeDisplay, result);
                    break;

                case Types.Sequence:
                    PrintNodeHeader(prefix + nodeSymbol, nodeName, nodeTypeDisplay);
                    result = true;
                    for (int i = 0; i < Children.Count; i++)
                    {
                        var child = Children[i];
                        bool isLast = i == Children.Count - 1;
                        string childPrefix = prefix + GetChildPrefix(isRoot, isLast);

                        bool childResult = child.ExecuteWithTreeDebug(character, childPrefix, false, i);
                        if (!childResult)
                        {
                            result = false;
                            break;
                        }
                    }
                    PrintNodeFinalResult(prefix + nodeSymbol, nodeName, nodeTypeDisplay, result);
                    break;

                case Types.Selector:
                    PrintNodeHeader(prefix + nodeSymbol, nodeName, nodeTypeDisplay);
                    result = false;
                    for (int i = 0; i < Children.Count; i++)
                    {
                        var child = Children[i];
                        bool isLast = i == Children.Count - 1;
                        string childPrefix = prefix + GetChildPrefix(isRoot, isLast);

                        bool childResult = child.ExecuteWithTreeDebug(character, childPrefix, false, i);
                        if (childResult)
                        {
                            result = true;
                            break;
                        }
                    }
                    PrintNodeFinalResult(prefix + nodeSymbol, nodeName, nodeTypeDisplay, result);
                    break;

                case Types.Inverter:
                    PrintNodeHeader(prefix + nodeSymbol, nodeName, nodeTypeDisplay);
                    if (Children.Count > 0)
                    {
                        var child = Children[0];
                        string childPrefix = prefix + GetChildPrefix(isRoot, true);
                        bool childResult = child.ExecuteWithTreeDebug(character, childPrefix, false, 0);
                        result = !childResult;
                    }
                    else
                    {
                        result = false;
                    }
                    PrintNodeFinalResult(prefix + nodeSymbol, nodeName, nodeTypeDisplay, result);
                    break;

                default:
                    result = false;
                    PrintNodeResult(prefix + nodeSymbol, nodeName, nodeTypeDisplay, result);
                    break;
            }

            return result;
        }
        private string GetName()
        {
            global::Data.Design.BehaviorTree design = global::Data.Design.Agent.Instance.Content.Get<global::Data.Design.BehaviorTree>(b => b.id == Config.Id);
            return $"{Config.Id}·{design.cid}";
        }

        private string GetTreeSymbol(bool isRoot, int childIndex)
        {
            if (isRoot) return "";
            return "├── ";
        }

        private string GetChildPrefix(bool isParentRoot, bool isLastChild)
        {
            if (isParentRoot) return "";
            return isLastChild ? "    " : "│   ";
        }

        private string GetNodeTypeDisplay()
        {
            return Type switch
            {
                Types.Action => "Action",
                Types.Condition => "Condition",
                Types.Sequence => "Sequence",
                Types.Selector => "Selector",
                Types.Inverter => "Inverter",
                _ => "Unknown"
            };
        }

        private void PrintNodeResult(string prefix, string nodeName, string nodeType, bool result)
        {
            var resultText = result ? "成功" : "失败";
            var color = result ? ConsoleColor.Green : ConsoleColor.Red;
            PrintColoredLog($"{prefix}[{nodeType}] {nodeName} -> {resultText}", color);
        }

        private void PrintNodeHeader(string prefix, string nodeName, string nodeType)
        {
            PrintColoredLog($"{prefix}[{nodeType}] {nodeName} {{", ConsoleColor.White);
        }

        private void PrintNodeFinalResult(string prefix, string nodeName, string nodeType, bool result)
        {
            var resultText = result ? "成功" : "失败";
            var color = result ? ConsoleColor.Green : ConsoleColor.Red;
            PrintColoredLog($"{prefix}}} -> {resultText}", color);
        }

        private void PrintColoredLog(string message, ConsoleColor color)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [BEHAVIOR_TREE] {message}");
            Console.ForegroundColor = originalColor;
        }

        public void PrintTreeStructure()
        {
            PrintTreeStructureRecursive("", true, 0);
        }

        private void PrintTreeStructureRecursive(string prefix, bool isRoot, int childIndex)
        {
            string nodeSymbol = GetTreeSymbol(isRoot, childIndex);
            string nodeName = GetName();
            string nodeTypeDisplay = GetNodeTypeDisplay();

            Utils.Debug.Log.Info("BEHAVIOR_TREE", $"{prefix}{nodeSymbol}[{nodeTypeDisplay}] {nodeName}");

            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                bool isLast = i == Children.Count - 1;
                string childPrefix = prefix + GetChildPrefix(isRoot, isLast);
                child.PrintTreeStructureRecursive(childPrefix, false, i);
            }
        }
    }

    // Context枚举已删除 - 改为动态查找目标，不再使用黑板系统
}