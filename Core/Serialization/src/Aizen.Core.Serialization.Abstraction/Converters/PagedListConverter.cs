using Aizen.Core.Common.Abstraction.Collections;
using Aizen.Core.Common.Abstraction.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Aizen.Core.Serialization.Abstraction.Converters
{
    public class PagedListConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var jsonSerializer = new JsonSerializer
            {
                ContractResolver = serializer.ContractResolver,
                Formatting = serializer.Formatting,
            };
            jsonSerializer.Converters.Add(new StringEnumConverter());
            var jobject = JToken.FromObject(value, jsonSerializer);
            writer.WriteToken(jobject.CreateReader());
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            var dtoType = objectType.GenericTypeArguments[0];
            var resultType = typeof(PagedList<>).MakeGenericType(dtoType);
            var jsonSerializer = new JsonSerializer
            {
                ContractResolver = serializer.ContractResolver,
                Formatting = serializer.Formatting,
            };
            jsonSerializer.Converters.Add(new StringEnumConverter());
            return JToken.Load(reader).ToObject(resultType, jsonSerializer);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableToGenericType(typeof(IPagedList<>));
        }
    }
}
