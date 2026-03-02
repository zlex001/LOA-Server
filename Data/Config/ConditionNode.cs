using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data.Config
{
    [JsonConverter(typeof(ConditionNodeConverter))]
    public abstract class ConditionNode
    {
        public abstract bool Evaluate(object target, object[] eventArgs = null);
    }

    public class SingleCondition : ConditionNode
    {
        public string Value { get; set; }

        public SingleCondition() { }

        public SingleCondition(string value)
        {
            Value = value;
        }

        // Condition checker callback, set by Logic layer
        public static System.Func<object, List<string>, object[], bool> ConditionChecker { get; set; }
        public static System.Func<object, string, bool> SingleConditionChecker { get; set; }

        public override bool Evaluate(object target, object[] eventArgs = null)
        {
            if (string.IsNullOrEmpty(Value))
                return true;

            bool result;
            if (target is global::Data.Ability ability)
            {
                result = ConditionChecker?.Invoke(ability, new List<string> { Value }, eventArgs) ?? true;
            }
            else
            {
                result = SingleConditionChecker?.Invoke(target, Value) ?? true;
            }
            
            return result;
        }
    }

    public class AndCondition : ConditionNode
    {
        public ConditionNode Left { get; set; }
        public ConditionNode Right { get; set; }

        public AndCondition() { }

        public AndCondition(ConditionNode left, ConditionNode right)
        {
            Left = left;
            Right = right;
        }

        public override bool Evaluate(object target, object[] eventArgs = null)
        {
            bool leftResult = Left?.Evaluate(target, eventArgs) ?? true;
            bool rightResult = Right?.Evaluate(target, eventArgs) ?? true;
            return leftResult && rightResult;
        }
    }

    public class OrCondition : ConditionNode
    {
        public ConditionNode Left { get; set; }
        public ConditionNode Right { get; set; }

        public OrCondition() { }

        public OrCondition(ConditionNode left, ConditionNode right)
        {
            Left = left;
            Right = right;
        }

        public override bool Evaluate(object target, object[] eventArgs = null)
        {
            bool leftResult = Left?.Evaluate(target, eventArgs) ?? true;
            bool rightResult = Right?.Evaluate(target, eventArgs) ?? true;
            return leftResult || rightResult;
        }
    }

    public class NotCondition : ConditionNode
    {
        public ConditionNode Child { get; set; }

        public NotCondition() { }

        public NotCondition(ConditionNode child)
        {
            Child = child;
        }

        public override bool Evaluate(object target, object[] eventArgs = null)
        {
            bool childResult = Child?.Evaluate(target, eventArgs) ?? true;
            return !childResult;
        }
    }

    // JSON序列化支持多态类型
    public class ConditionNodeConverter : JsonConverter<ConditionNode>
    {
        public override void WriteJson(JsonWriter writer, ConditionNode value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            if (value is SingleCondition single)
            {
                writer.WritePropertyName("type");
                writer.WriteValue("single");
                writer.WritePropertyName("value");
                writer.WriteValue(single.Value);
            }
            else if (value is AndCondition and)
            {
                writer.WritePropertyName("type");
                writer.WriteValue("and");
                writer.WritePropertyName("left");
                serializer.Serialize(writer, and.Left);
                writer.WritePropertyName("right");
                serializer.Serialize(writer, and.Right);
            }
            else if (value is OrCondition or)
            {
                writer.WritePropertyName("type");
                writer.WriteValue("or");
                writer.WritePropertyName("left");
                serializer.Serialize(writer, or.Left);
                writer.WritePropertyName("right");
                serializer.Serialize(writer, or.Right);
            }
            else if (value is NotCondition not)
            {
                writer.WritePropertyName("type");
                writer.WriteValue("not");
                writer.WritePropertyName("child");
                serializer.Serialize(writer, not.Child);
            }
            
            writer.WriteEndObject();
        }

        public override ConditionNode ReadJson(JsonReader reader, Type objectType, ConditionNode existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = serializer.Deserialize<Dictionary<string, object>>(reader);
            
            if (!obj.ContainsKey("type"))
                return null;

            string type = obj["type"].ToString();
            
            switch (type)
            {
                case "single":
                    return new SingleCondition(obj["value"]?.ToString());
                
                case "and":
                    var leftJson = obj["left"];
                    var rightJson = obj["right"];
                    var left = serializer.Deserialize<ConditionNode>(Newtonsoft.Json.Linq.JToken.FromObject(leftJson).CreateReader());
                    var right = serializer.Deserialize<ConditionNode>(Newtonsoft.Json.Linq.JToken.FromObject(rightJson).CreateReader());
                    return new AndCondition(left, right);
                
                case "or":
                    var leftOrJson = obj["left"];
                    var rightOrJson = obj["right"];
                    var leftOr = serializer.Deserialize<ConditionNode>(Newtonsoft.Json.Linq.JToken.FromObject(leftOrJson).CreateReader());
                    var rightOr = serializer.Deserialize<ConditionNode>(Newtonsoft.Json.Linq.JToken.FromObject(rightOrJson).CreateReader());
                    return new OrCondition(leftOr, rightOr);
                
                case "not":
                    var childJson = obj["child"];
                    var child = serializer.Deserialize<ConditionNode>(Newtonsoft.Json.Linq.JToken.FromObject(childJson).CreateReader());
                    return new NotCondition(child);
                
                default:
                    return null;
            }
        }
    }
}
