using System;
using System.Linq.Expressions;
using MongoDB.Driver.Linq;
using Aizen.Core.Data.Mongo.Document;

namespace Aizen.Core.Data.Mongo.Repository
{
    public partial interface IAizenMongoRepository<TDocument> : IAizenMongoRepositoryAsync<TDocument>, IAizenMongoRepositorySync<TDocument>
        where TDocument : AizenDocumentBase
    {
        IMongoQueryable<TDocument> CreateQuery(
            Expression<Func<TDocument, bool>> predicate = null,
            Func<IMongoQueryable<TDocument>, IOrderedMongoQueryable<TDocument>> orderBy = null);
    }
}
