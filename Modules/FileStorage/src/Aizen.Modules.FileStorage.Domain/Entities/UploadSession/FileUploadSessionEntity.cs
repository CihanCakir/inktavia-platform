using Aizen.Core.Domain;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Entities.File;

namespace Aizen.Modules.FileStorage.Domain.Entities.UploadSession;

[DocumentationInfo("File upload session entity", "Tracks an S3 pre-signed upload session, its expiration and lifecycle state.")]
public sealed class FileUploadSessionEntity : AizenEntityWithAudit
{
    public long FileId { get; private set; }
    public string UploadSessionCode { get; private set; } = default!;
    public string BucketName { get; private set; } = default!;
    public string ObjectKey { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public UploadSessionStatus Status { get; private set; }
    public string RequestedFileName { get; private set; } = default!;
    public string RequestedContentType { get; private set; } = default!;
    public long RequestedSizeInBytes { get; private set; }
    public long? RequestedByUserId { get; private set; }
    public string? ClientId { get; private set; }
    public string? DeviceId { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public FileEntity? File { get; private set; }

    public FileUploadSessionEntity() { }

    public static FileUploadSessionEntity Create(
        long fileId, string uploadSessionCode, string bucketName, string objectKey,
        DateTime expiresAt, string requestedFileName, string requestedContentType,
        long requestedSizeInBytes, long? requestedByUserId, string? clientId, string? deviceId)
    {
        return new FileUploadSessionEntity
        {
            FileId = fileId,
            UploadSessionCode = uploadSessionCode,
            BucketName = bucketName,
            ObjectKey = objectKey,
            ExpiresAt = expiresAt,
            Status = UploadSessionStatus.Active,
            RequestedFileName = requestedFileName,
            RequestedContentType = requestedContentType,
            RequestedSizeInBytes = requestedSizeInBytes,
            RequestedByUserId = requestedByUserId,
            ClientId = clientId,
            DeviceId = deviceId,
            IsActive = true
        };
    }

    public void Complete()
    {
        Status = UploadSessionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        IsActive = false;
    }

    public void Expire()
    {
        Status = UploadSessionStatus.Expired;
        IsActive = false;
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt || Status == UploadSessionStatus.Expired;
}
