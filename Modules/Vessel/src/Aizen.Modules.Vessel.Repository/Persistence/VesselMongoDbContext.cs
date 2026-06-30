using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Vessel.Repository.Persistence;

[DocumentationInfo("Vessel MongoDB context", "MongoDB context for the vessel read-side documents.")]
public sealed class VesselMongoDbContext : AizenMongoContext
{
    public VesselMongoDbContext(IOptions<DatabaseSettings> option)
        : base(option)
    {
    }

    protected override string ConfigurationKey => "VesselMongo";
}
