using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Domain.Interface.Service;

[DocumentationInfo("Vessel file storage service interface", "Centralizes FileStorage RemoteCall operations for the Vessel module. Prevents command handlers from coupling directly to FileStorage request/response contracts.")]
public interface IVesselFileStorageService
{
    Task<FileMetadataDto> GetRequiredFileMetadataAsync(
        Guid fileId,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task EnsureFileCanBeAttachedToVesselAsync(
        Guid fileId,
        IReadOnlyList<string> allowedContentTypes,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<FileOwnerReferenceDto> LinkFileToVesselOwnerAsync(
        Guid fileId,
        long vesselRelatedEntityId,
        string ownerEntityType,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<FileAccessUrlDto?> CreateReadUrlAsync(
        Guid fileId,
        TimeSpan expiresIn,
        string accessToken,
        CancellationToken cancellationToken = default);
}
