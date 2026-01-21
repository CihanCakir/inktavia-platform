using MongoDB.Bson;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Aizen.Core.Data.Mongo.Helpers
{
    public static class MongoHelpers
    {
        public static string MongoFilterBuilder(object parameter, string query)
        {
            if (parameter != null)
            {
                if (parameter is ExpandoObject)
                {
                    foreach (var item in parameter as IDictionary<string, object>)
                    {
                        var value = ConvertToMongoDbValueType(item.Key, item.Value);
                        query = query.Replace("@" + item.Key, value.ToString());
                    }
                }
                else
                {
                    var properties = parameter.GetType().GetProperties();
                    foreach (var property in properties)
                    {
                        var value = ConvertToMongoDbValueType(property.PropertyType.Name, property.GetValue(parameter));
                        query = query.Replace("@" + property.Name, value.ToString());
                    }
                }
            }

            return query;
        }

        public static object? ConvertToMongoDbValueType(string propertyName, object value)
        {
            return propertyName switch
            {
                nameof(ObjectId) => $"ObjectId('{value}')",
                nameof(DateTime) => $"ISODate('{((DateTime)value).ToString("o", CultureInfo.InvariantCulture)}')",
                nameof(String) => $"'" + value + "'",
                _ => value,
            };
        }

        public static string ReplaceWithCurrentDateTime(string text)
        {
            return text.Replace("Date()", $"Date(\"{DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)}\")");
        }
    }
}
