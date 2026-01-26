using Aizen.Core.Data.Mongo.Document;
using Aizen.Core.Data.Mongo.Repository;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace Aizen.Core.Data.Mongo
{
    public class AizenMongoRepositoryFactory<TMongoContext> : IAizenMongoRepositoryFactory<TMongoContext>
        where TMongoContext : AizenMongoContext
    {
        private readonly ConcurrentDictionary<string, object> _repositories;
        private readonly AizenMongoContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AizenMongoRepositoryFactory(
            TMongoContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            this._repositories = new ConcurrentDictionary<string, object>();
            this._context = context;
            this._httpContextAccessor = httpContextAccessor;
        }

        public IAizenMongoRepository<TDocument> GetRepository<TDocument>(
            string collectionName = default)
            where TDocument : AizenDocumentBase
        {
            return this.CreateOrGetRepository<TDocument>(collectionName);
        }

        private AizenMongoRepository<TDocument> CreateOrGetRepository<TDocument>(
            string collectionName)
            where TDocument : AizenDocumentBase
        {
            //Change
            var repositoryConfiguration = new JObject()
            {
                //["userId"] = this._httpContextAccessor.HttpContext?.RequestServices.GetRequiredService<IAizenInfoAccessor>().UserInfoAccessor?.UserInfo.LifeCycle.ToString()
            };

            return (AizenMongoRepository<TDocument>)this._repositories.GetOrAdd(
                !string.IsNullOrEmpty(collectionName)
                    ? collectionName
                    : typeof(TDocument).Name,
                new AizenMongoRepository<TDocument>(
                    this._context,
                    repositoryConfiguration,
                    collectionName));
        }
    }
}
