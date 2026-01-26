using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Aizen.Core.Data.Mongo.Extensions
{
    public static class AizenUpdateDefinitionExtensions
    {
        public static UpdateDefinition<TDocument> Set<TDocument, TField>(
            this UpdateDefinition<TDocument> update,
            Expression<Func<TDocument, TField>> field,
            TField value)
        {
            var update1 = Builders<TDocument>.Update;
            return update1.Combine(update, update1.Set(field, value));
        }
    }
}
