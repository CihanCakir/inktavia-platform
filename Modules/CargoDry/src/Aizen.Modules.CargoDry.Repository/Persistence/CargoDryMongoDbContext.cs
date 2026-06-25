using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo;
using Aizen.Modules.CargoDry.Abstraction.Model;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.CargoDry.Repository.Persistence;

[DocumentationInfo("CargoDry MongoDB context",
    "MongoDB context for CargoDry activation logs and analytics snapshots. " +
    "ConfigurationKey maps to DatabaseSettings:CargoDryMongo:ConnectionString.")]
public sealed class CargoDryMongoDbContext : AizenMongoContext
{
    public CargoDryMongoDbContext(IOptions<DatabaseSettings> option)
        : base(option) { }

    protected override string ConfigurationKey => "CargoDryMongo";
}
