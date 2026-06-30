using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Domain.Interface.Service;

[DocumentationInfo("File storage service interface", "Handles file metadata completion after S3 upload: validates object existence, size, content type and marks file as uploaded or ready.")]
public interface IFileStorageService
{
    Task<FileDto> CompleteUploadAsync(string uploadSessionCode, string? checksum, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(long fileId, long? deletedByUserId, CancellationToken cancellationToken = default);
    Task<FileDto> UpdateVisibilityAsync(long fileId, FileVisibility visibility, CancellationToken cancellationToken = default);
    Task<bool> RejectFileAsync(long fileId, CancellationToken cancellationToken = default);
}
