using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Aizen.Core.Serialization.Abstraction;


namespace Aizen.Core.Data.Mongo.Extensions
{
    public static class ConvertExtensions
    {
        public static JObject ToJObject(this BsonDocument source)
        {
            return JObject.Parse(JsonConvert.SerializeObject(BsonTypeMapper.MapToDotNetValue(source),
                new AizenSerializerSettings()));
        }

        public static BsonDocument ToBsonDocument(this JObject source)
        {
            return BsonDocument.Parse(JsonConvert.SerializeObject(source, new AizenSerializerSettings()));
        }

        public static BsonDocument ToBsonDocumentWithDateConversion(this string source)
        {
            return BsonDocument.Parse(source).ConvertToIsoDates();
        }

        public static BsonDocument ToBsonDocumentWithDateConversion(this JObject source)
        {
            return BsonDocument.Parse(source.ToString()).ConvertToIsoDates();
        }
    }
}
