using System;
using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Aizen.Core.Data.Mongo.Builders
{
    public class AizenFilterDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCoordinates"></typeparam>
        /// <param name="field"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        public FilterDefinition<TDocument> GeoIntersects<TCoordinates>(
            Expression<Func<TDocument, object>> field,
            GeoJsonGeometry<TCoordinates> geometry)
            where TCoordinates : GeoJsonCoordinates
        {
            var filterDefinitionBuilder = new FilterDefinitionBuilder<TDocument>();
            return filterDefinitionBuilder.GeoIntersects(field, geometry);
        }
    }
}
