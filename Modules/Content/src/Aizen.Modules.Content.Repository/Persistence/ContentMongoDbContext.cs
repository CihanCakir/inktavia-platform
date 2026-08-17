using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Content.Repository.Persistence;

[DocumentationInfo("Content MongoDB context",
    "MongoDB context for the Content module (items, comments, favorites, categories). " +
    "ConfigurationKey maps to DatabaseSettings:ContentMongo:ConnectionString.")]
public sealed class ContentMongoDbContext : AizenMongoContext
{
    public ContentMongoDbContext(IOptions<DatabaseSettings> option)
        : base(option) { }

    protected override string ConfigurationKey => "ContentMongo";
}
