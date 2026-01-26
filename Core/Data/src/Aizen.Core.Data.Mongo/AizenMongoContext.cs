using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo.Internals;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Aizen.Core.Data.Mongo
{
    public abstract class AizenMongoContext
    {
        protected abstract string ConfigurationKey { get; }

        protected AizenMongoContext(IOptions<DatabaseSettings> option)
        {
            var clientItem = MongoClientContainer.GetInstance().GetClient(option, this.ConfigurationKey);
            this.Database = clientItem.Database;
        }

        public IMongoDatabase Database { get; }
    }
}
