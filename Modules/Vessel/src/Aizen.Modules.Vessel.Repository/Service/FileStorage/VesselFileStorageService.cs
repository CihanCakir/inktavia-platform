using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Repository.Service.FileStorage;

[DocumentationInfo("Vessel file storage service", "Centralizes FileStorage RemoteCall usage for the Vessel module.")]
public sealed class VesselFileStorageService : IVesselFileStorageService
{
    private readonly IFileStorageRemoteCall _remoteCall;

    public VesselFileStorageService(IFileStorageRemoteCall remoteCall)
    {
        _remoteCall = remoteCall;
    }

    public async Task<FileMetadataDto> GetRequiredFileMetadataAsync(
        Guid fileId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var response = await _remoteCall.GetFileMetadata(fileId, $"Bearer {accessToken}");

        if (response is null)
            throw new KeyNotFoundException($"File {fileId} not found in FileStorage.");

        return new FileMetadataDto
        {
            FileId = response.FileId,
            FileCode = response.FileCode,
            OriginalFileName = response.OriginalFileName,
            ContentType = response.ContentType,
            Extension = response.Extension,
            SizeInBytes = response.SizeInBytes,
            Visibility = response.Visibility,
            Status = response.Status,
            UploadedAt = response.UploadedAt
        };
    }

    public async Task EnsureFileCanBeAttachedToVesselAsync(
        Guid fileId,
        IReadOnlyList<string> allowedContentTypes,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetRequiredFileMetadataAsync(fileId, accessToken, cancellationToken);

        if (metadata.Status == FileStatus.Deleted)
            throw new InvalidOperationException($"File {fileId} is deleted and cannot be attached to a vessel.");

        if (metadata.Status != FileStatus.Uploaded && metadata.Status != FileStatus.Ready)
            throw new InvalidOperationException($"File {fileId} is not in an attachable state (current status: {metadata.Status}). File must be Uploaded or Ready.");

        if (allowedContentTypes.Count > 0 &&
            !allowedContentTypes.Contains(metadata.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"File content type '{metadata.ContentType}' is not allowed. Allowed types: {string.Join(", ", allowedContentTypes)}");
        }
    }

    public async Task<FileOwnerReferenceDto> LinkFileToVesselOwnerAsync(
        Guid fileId,
        long vesselRelatedEntityId,
        string ownerEntityType,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var ownerEntityGuid = LongToGuid(vesselRelatedEntityId);

        var request = new LinkFileToOwnerRemoteCallRequest
        {
            OwnerModule = nameof(FileOwnerModule.Vessel),
            OwnerEntityType = ownerEntityType,
            OwnerEntityId = ownerEntityGuid
        };

        var response = await _remoteCall.LinkFileToOwner(fileId, request, $"Bearer {accessToken}");

        return new FileOwnerReferenceDto
        {
            FileId = response.FileId,
            OwnerModule = nameof(FileOwnerModule.Vessel),
            OwnerEntityType = ownerEntityType,
            OwnerEntityId = ownerEntityGuid,
            LinkedAt = DateTime.UtcNow,
            IsActive = response.IsLinked
        };
    }

    public async Task<FileAccessUrlDto?> CreateReadUrlAsync(
        Guid fileId,
        TimeSpan expiresIn,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _remoteCall.CreateReadUrl(
                fileId,
                new CreateFileReadUrlRemoteCallRequest { ExpiresIn = expiresIn },
                $"Bearer {accessToken}");

            if (response is null)
                return null;

            return new FileAccessUrlDto
            {
                FileId = response.FileId,
                ReadUrl = response.ReadUrl,
                ExpiresAt = response.ExpiresAt
            };
        }
        catch
        {
            return null;
        }
    }

    private static Guid LongToGuid(long id)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}
