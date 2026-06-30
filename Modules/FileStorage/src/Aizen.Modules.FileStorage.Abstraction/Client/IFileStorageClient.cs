using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;

namespace Aizen.Modules.FileStorage.Abstraction.Client;

[DocumentationInfo("FileStorage client interface", "Defines in-process synchronous contracts for FileStorage operations used by other modules.")]
public interface IFileStorageClient
{
    Task<FileMetadataDto?> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task<FileValidationResultDto> ValidateFileOwnershipAsync(Guid fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken cancellationToken = default);
    Task<FileAccessUrlDto> CreateReadUrlAsync(Guid fileId, TimeSpan expiresIn, CancellationToken cancellationToken = default);
}
