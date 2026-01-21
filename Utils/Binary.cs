using System.Text.Json;
using MessagePack;
namespace Utils
{
    public enum SerializeFormat
    {
        Json,
        Binary
    }

    public static class Binary
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };

        public static void Serialize<T>(T obj, string filePath, SerializeFormat format)
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (format == SerializeFormat.Json)
                {
                    string json = JsonSerializer.Serialize(obj, JsonOptions);
                    File.WriteAllText(filePath, json);
                }
                else if (format == SerializeFormat.Binary)
                {
                    byte[] bytes = MessagePackSerializer.Serialize(obj);
                    File.WriteAllBytes(filePath, bytes);
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("BINARY", $"序列化失败: {ex.Message}");
            }
        }

        public static T Deserialize<T>(string filePath, SerializeFormat format)
        {
            try
            {
                if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
                    return default;

                if (format == SerializeFormat.Json)
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<T>(json, JsonOptions);
                }
                else if (format == SerializeFormat.Binary)
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    return MessagePackSerializer.Deserialize<T>(bytes);
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("BINARY", $"反序列化失败: {ex.Message}");
            }

            return default;
        }

        public static List<T> Deserializes<T>(string path, SerializeFormat format, string extension)
        {
            var list = new List<T>();
            try
            {
                string[] files = Directory.GetFiles(path, $"*.{extension}");
                foreach (string file in files)
                {
                    var obj = Deserialize<T>(file, format);
                    if (obj != null)
                        list.Add(obj);
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("BINARY", $"批量反序列化失败: {ex.Message}");
            }
            return list;
        }
    }
}