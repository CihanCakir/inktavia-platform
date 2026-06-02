using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File cache key service interface", "Centralises cache key computation for all file-related queries.")]
public interface IFileCacheKeyService
{
    string FileById(long fileId);
    string FileByGuid(Guid fileId);
    string FileByCode(string fileCode);
    string FilesByOwner(string ownerModule, string ownerEntityType, Guid ownerEntityId);
    string UploadSession(string uploadSessionCode);
    string ReadUrl(Guid fileId, long? userId);
    string ProcessingJobs(long fileId);
}
