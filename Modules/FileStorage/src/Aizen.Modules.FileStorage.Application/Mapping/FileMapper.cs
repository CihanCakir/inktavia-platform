using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Dto.Processing;
using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Aizen.Modules.FileStorage.Domain.Entities.File;
using Aizen.Modules.FileStorage.Domain.Entities.Processing;

namespace Aizen.Modules.FileStorage.Application.Mapping;

[DocumentationInfo("File mapper", "Provides static extension methods for mapping FileStorage domain entities to DTOs.")]
public static class FileMapper
{
    public static FileDto ToFileDto(this FileEntity entity) => new()
    {
        FileId = entity.PublicId ?? Guid.Empty,
        FileCode = entity.FileCode,
        OriginalFileName = entity.OriginalFileName,
        StoredFileName = entity.StoredFileName,
        BucketName = entity.BucketName,
        ObjectKey = entity.ObjectKey,
        ContentType = entity.ContentType,
        Extension = entity.Extension,
        SizeInBytes = entity.SizeInBytes,
        Checksum = entity.Checksum,
        StorageProvider = entity.StorageProvider,
        Visibility = entity.Visibility,
        Category = entity.Category,
        Status = entity.Status,
        UploadedAt = entity.UploadedAt,
        UploadedByUserId = entity.UploadedByUserId,
        CreatedAt = entity.CreateDate ?? DateTime.UtcNow
    };

    public static FileMetadataDto ToFileMetadataDto(this FileEntity entity) => new()
    {
        FileId = entity.PublicId ?? Guid.Empty,
        FileCode = entity.FileCode,
        OriginalFileName = entity.OriginalFileName,
        ContentType = entity.ContentType,
        Extension = entity.Extension,
        SizeInBytes = entity.SizeInBytes,
        Checksum = entity.Checksum,
        StorageProvider = entity.StorageProvider,
        Visibility = entity.Visibility,
        Category = entity.Category,
        Status = entity.Status,
        UploadedAt = entity.UploadedAt,
        UploadedByUserId = entity.UploadedByUserId,
        OwnerReferences = entity.OwnerReferences
            .Select(r => r.ToOwnerReferenceDto(entity.PublicId ?? Guid.Empty))
            .ToList()
    };

    public static FileOwnerReferenceDto ToOwnerReferenceDto(this FileOwnerReferenceEntity entity, Guid fileGuid) => new()
    {
        FileId = fileGuid,
        OwnerModule = entity.OwnerModule,
        OwnerEntityType = entity.OwnerEntityType,
        OwnerEntityId = entity.OwnerEntityId,
        LinkedAt = entity.LinkedAt,
        LinkedByUserId = entity.LinkedByUserId,
        IsActive = entity.IsActive
    };

    public static FileProcessingJobDto ToProcessingJobDto(this FileProcessingJobEntity entity, Guid fileGuid) => new()
    {
        FileId = fileGuid,
        ProcessingType = entity.ProcessingType,
        Status = entity.Status,
        RequestedAt = entity.RequestedAt,
        StartedAt = entity.StartedAt,
        CompletedAt = entity.CompletedAt,
        ErrorCode = entity.ErrorCode,
        ErrorMessage = entity.ErrorMessage,
        RetryCount = entity.RetryCount
    };
}
