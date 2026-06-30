
namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File cache invalidation service interface", "Invalidates cached query results when file data changes.")]
public interface IFileCacheInvalidationService
{
    Task InvalidateFileAsync(long fileId, Guid fileGuid, string fileCode, CancellationToken ct = default);
    Task InvalidateOwnerFilesAsync(string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default);
    Task InvalidateUploadSessionAsync(string uploadSessionCode, CancellationToken ct = default);
    Task InvalidateReadUrlAsync(Guid fileId, long? userId, CancellationToken ct = default);
    Task InvalidateProcessingJobsAsync(long fileId, CancellationToken ct = default);
}
