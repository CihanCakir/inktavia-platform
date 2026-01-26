using Aizen.Core.Data.Mongo.Document;
using Aizen.Core.Data.Mongo.Repository;

namespace Aizen.Core.Data.Mongo
{
    public interface IAizenMongoRepositoryFactory<TMongoContext>
        where TMongoContext : AizenMongoContext
    {
        IAizenMongoRepository<TDocument> GetRepository<TDocument>(string collectionName = default)
            where TDocument : AizenDocumentBase;
    }
}
