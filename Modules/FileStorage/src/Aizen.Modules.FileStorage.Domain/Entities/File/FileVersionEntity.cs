using Aizen.Core.Domain;

namespace Aizen.Modules.FileStorage.Domain.Entities.File;

[DocumentationInfo("File version entity", "Tracks historical versions of a file object in storage.")]
public sealed class FileVersionEntity : AizenEntityWithAudit
{
    public long FileId { get; private set; }
    public int VersionNo { get; private set; }
    public string BucketName { get; private set; } = default!;
    public string ObjectKey { get; private set; } = default!;
    public long SizeInBytes { get; private set; }
    public string? Checksum { get; private set; }
    public long? CreatedByUserId { get; private set; }

    public FileEntity? File { get; private set; }

    public FileVersionEntity() { }

    public static FileVersionEntity Create(
        long fileId, int versionNo, string bucketName, string objectKey,
        long sizeInBytes, string? checksum, long? createdByUserId)
    {
        return new FileVersionEntity
        {
            FileId = fileId,
            VersionNo = versionNo,
            BucketName = bucketName,
            ObjectKey = objectKey,
            SizeInBytes = sizeInBytes,
            Checksum = checksum,
            CreatedByUserId = createdByUserId,
            IsActive = true
        };
    }
}
