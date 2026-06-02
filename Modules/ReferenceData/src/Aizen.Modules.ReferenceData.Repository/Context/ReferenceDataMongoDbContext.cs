using Aizen.Core.Data.Mongo;
using Aizen.Core.Common.Abstraction.Settings;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.ReferenceData.Repository.Context;

public sealed class ReferenceDataMongoDbContext : AizenMongoContext
{
      public ReferenceDataMongoDbContext(IOptions<DatabaseSettings> option)
            : base(option)
        {
        }

        protected override string ConfigurationKey => "ReferenceDataMongo";
}
