using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Messaging.Repository.Persistence;

public sealed class MessagingMongoDbContext : AizenMongoContext
{
    public MessagingMongoDbContext(IOptions<DatabaseSettings> option)
        : base(option) { }

    protected override string ConfigurationKey => "MessagingMongo";
}
