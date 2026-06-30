using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File cache key service", "Centralises cache key computation for all file-related queries.")]
public sealed class FileCacheKeyService : IFileCacheKeyService
{
    public string FileById(long fileId) => $"filestorage:file:id:{fileId}";
    public string FileByGuid(Guid fileId) => $"filestorage:file:guid:{fileId}";
    public string FileByCode(string fileCode) => $"filestorage:file:code:{fileCode}";
    public string FilesByOwner(string ownerModule, string ownerEntityType, Guid ownerEntityId) => $"filestorage:owner:{ownerModule}:{ownerEntityType}:{ownerEntityId}:files";
    public string UploadSession(string uploadSessionCode) => $"filestorage:session:{uploadSessionCode}";
    public string ReadUrl(Guid fileId, long? userId) => $"filestorage:readurl:{fileId}:{userId}";
    public string ProcessingJobs(long fileId) => $"filestorage:jobs:{fileId}";
}
