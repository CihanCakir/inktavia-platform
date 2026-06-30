using Aizen.Core.Common.Abstraction.Settings;
using Aizen.Core.Data.Mongo;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.FileStorage.Repository.Persistence;

[DocumentationInfo("FileStorage MongoDB context", "MongoDB context for the file storage read-side documents.")]
public sealed class FileStorageMongoDbContext : AizenMongoContext
{
    public FileStorageMongoDbContext(IOptions<DatabaseSettings> option) : base(option) { }

    protected override string ConfigurationKey => "FileStorageMongo";
}
