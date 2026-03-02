using Newtonsoft.Json;
using System;

namespace Data
{
    public class ChannelConverter : JsonConverter<Channel>
    {
        public override Channel ReadJson(JsonReader reader, Type objectType, Channel existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Channel.System;

            string value = reader.Value?.ToString();
            if (string.IsNullOrEmpty(value))
                return Channel.System;

            if (value == "ChannelAll")
                return Channel.All;

            if (Enum.TryParse<Channel>(value, true, out var result))
                return result;

            return Channel.System;
        }

        public override void WriteJson(JsonWriter writer, Channel value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}

