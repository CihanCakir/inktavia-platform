using System;
using System.Globalization;
using MongoDB.Bson;

namespace Aizen.Core.Data.Mongo.Extensions
{
    public static class BsonDateExtensions
    {
        private static readonly string[] _formats =
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ", "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK",
        };


        /// <summary>
        /// When you call BsonDocument.Parse on a JSON string containing ISO8601 dates (e.g., "2018-04-02T08:03:12.3456789-04:00")
        /// it does not interpret them as datetime values; it just treats them as strings.
        /// </summary>
        /// <remarks>I logged it at https://jira.mongodb.org/browse/CSHARP-2233 ... they closed it "as designed" so this will
        /// be an enduring solution.</remarks>
        public static BsonDocument ConvertToIsoDates(this BsonDocument bsonDocument)
        {
            for (var i = 0; i < bsonDocument.ElementCount; ++i)
            {
                var bsonValue = bsonDocument[i];
                switch (bsonValue.BsonType)
                {
                    case BsonType.String:
                        if (DateTime.TryParseExact(bsonValue.AsString,
                            _formats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var result))
                        {
                            bsonDocument[i] = new BsonDateTime(result);
                        }
                        else if(DateTimeOffset.TryParse(bsonValue.AsString,out var dateTimeOffset))
                        {
                            bsonDocument[i] = new BsonDateTime(dateTimeOffset.ToUnixTimeMilliseconds());
                        }

                        break;
                    case BsonType.Array:
                        bsonDocument[i].AsBsonArray.ConvertToIsoDates();
                        break;
                    case BsonType.Document:
                        bsonDocument[i].AsBsonDocument.ConvertToIsoDates();
                        break;
                }
            }

            return bsonDocument;
        }


        /// <summary>
        /// When you call BsonDocument.Parse on a JSON string containing ISO8601 dates (e.g., "2018-04-02T08:03:12.3456789-04:00")
        /// it does not interpret them as datetime values; it just treats them as strings.
        /// </summary>
        /// <remarks>I logged it at https://jira.mongodb.org/browse/CSHARP-2233 ... they closed it "as designed" so this will
        /// be an enduring solution.</remarks>
        public static BsonArray ConvertToIsoDates(this BsonArray bsonArray)
        {
            for (var i = 0; i < bsonArray.Count; ++i)
            {
                var bsonValue = bsonArray[i];
                switch (bsonValue.BsonType)
                {
                    case BsonType.String:
                        if (DateTime.TryParseExact(bsonValue.AsString,
                            _formats,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var result))
                        {
                            bsonArray[i] = new BsonDateTime(result);
                        }

                        break;
                    case BsonType.Array:
                        bsonArray[i].AsBsonArray.ConvertToIsoDates();
                        break;
                    case BsonType.Document:
                        bsonArray[i].AsBsonDocument.ConvertToIsoDates();
                        break;
                }
            }

            return bsonArray;
        }

        public static BsonValue FindProperty(this BsonDocument bsonDocument, string name)
        {
            var segments = name.Split('.');
            var thisDocument = bsonDocument;
            for (var i = 0; i < segments.Length; i++)
            {
                if (thisDocument.Contains(segments[i]) == false)
                {
                    return null;
                }

                if (i == segments.Length - 1)
                {
                    return thisDocument.GetValue(segments[i]);
                }

                if (thisDocument.IsBsonDocument)
                {
                    thisDocument = (BsonDocument)thisDocument.GetValue(segments[i]);
                }
            }
            return thisDocument;
        }
    }
}
