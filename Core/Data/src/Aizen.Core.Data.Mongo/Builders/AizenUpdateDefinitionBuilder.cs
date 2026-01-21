using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Aizen.Core.Data.Mongo.Builders
{
    public class AizenUpdateDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// <see cref="UpdateDefinition{TDocument}"/>
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public UpdateDefinition<TDocument> Set<TField>(
            Expression<Func<TDocument, TField>> field,
            TField value)
        {
            var updateDefinitionBuilder = new UpdateDefinitionBuilder<TDocument>();
            return updateDefinitionBuilder.Set(field, value);
        }

        /// <summary>
        /// <see cref="UpdateDefinition{TDocument}"/>
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <typeparam name="TField"></typeparam>
        /// <returns></returns>
        public UpdateDefinition<TDocument> Inc<TField>(
            Expression<Func<TDocument, TField>> field,
            TField value)
        {
            var updateDefinitionBuilder = new UpdateDefinitionBuilder<TDocument>();
            return updateDefinitionBuilder.Inc(field,value);
        }
    }
}
