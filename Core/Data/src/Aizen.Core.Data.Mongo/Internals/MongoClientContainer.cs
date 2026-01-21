using Aizen.Core.Common.Abstraction.Patterns;
using Aizen.Core.Common.Abstraction.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.Concurrent;
using Elastic.Apm.MongoDb;

namespace Aizen.Core.Data.Mongo.Internals
{
    public class MongoClientItem
    {
        public MongoUrl Url { get; }

        public MongoClientSettings Settings { get; }

        public MongoClient Client { get; }

        public IMongoDatabase Database { get; set; }

        public MongoClientItem(MongoUrl url, MongoClientSettings settings, MongoClient client)
        {
            this.Url = url;
            this.Settings = settings;
            this.Client = client;

            this.Database = this.Client.GetDatabase(this.Url.DatabaseName);
        }
    }

    public class MongoClientContainer : SingletonBase<MongoClientContainer>
    {
        private readonly ConcurrentDictionary<string, MongoClientItem> _mongoClients;

        public MongoClientContainer()
        {
            this._mongoClients = new ConcurrentDictionary<string, MongoClientItem>();
        }

        public MongoClientItem GetClient( IOptions<DatabaseSettings> option,
            string databaseName)
        {
            var client =
                this._mongoClients.GetOrAdd(databaseName, _ => this.InitClient(option, databaseName));

            return client;
        }

        public MongoClientItem InitClient(IOptions<DatabaseSettings> option,
            string databaseName)
        {
            var connection =
                option.Value.First(x => x.Key == databaseName && x.Value.Type == DatabaseType.Mongo);
            var mongoConnectionUrl = new MongoUrl(connection.Value.ConnectionString);

            var mongoClientSettings = MongoClientSettings.FromUrl(mongoConnectionUrl);
            mongoClientSettings.ClusterConfigurator = builder => builder.Subscribe(new MongoDbEventSubscriber());

            var client = new MongoClient(mongoClientSettings);

            return new MongoClientItem(mongoConnectionUrl, mongoClientSettings, client);
        }
    }
}
