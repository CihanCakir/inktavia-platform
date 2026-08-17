using Aizen.Core.Domain;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Aizen.Modules.FileStorage.Domain.Entities.Processing;
using Aizen.Modules.FileStorage.Domain.Entities.UploadSession;

namespace Aizen.Modules.FileStorage.Domain.Entities.File;

[DocumentationInfo("File entity", "Root aggregate representing a file stored in an object storage provider.")]
public sealed class FileEntity : AizenEntityWithAudit
{
    public string FileCode { get; private set; } = default!;
    public string OriginalFileName { get; private set; } = default!;
    public string StoredFileName { get; private set; } = default!;
    public string BucketName { get; private set; } = default!;
    public string ObjectKey { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string Extension { get; private set; } = default!;
    public long SizeInBytes { get; private set; }
    public string? Checksum { get; private set; }
    public StorageProviderType StorageProvider { get; private set; }
    public FileVisibility Visibility { get; private set; }
    public FileCategory Category { get; private set; }
    public FileStatus Status { get; private set; }
    public DateTime? UploadedAt { get; private set; }
    public long? UploadedByUserId { get; private set; }
    public long? DeletedByUserId { get; private set; }

    private readonly List<FileOwnerReferenceEntity> _ownerReferences = new();
    public IReadOnlyCollection<FileOwnerReferenceEntity> OwnerReferences => _ownerReferences.AsReadOnly();

    private readonly List<FileUploadSessionEntity> _uploadSessions = new();
    public IReadOnlyCollection<FileUploadSessionEntity> UploadSessions => _uploadSessions.AsReadOnly();

    private readonly List<FileProcessingJobEntity> _processingJobs = new();
    public IReadOnlyCollection<FileProcessingJobEntity> ProcessingJobs => _processingJobs.AsReadOnly();

    public FileEntity() { }

    public static FileEntity Create(
        string fileCode,
        string originalFileName,
        string storedFileName,
        string bucketName,
        string objectKey,
        string contentType,
        string extension,
        long sizeInBytes,
        StorageProviderType storageProvider,
        FileVisibility visibility,
        FileCategory category,
        long? uploadedByUserId)
    {
        return new FileEntity
        {
            FileCode = fileCode.ToUpperInvariant(),
            OriginalFileName = originalFileName.Trim(),
            StoredFileName = storedFileName,
            BucketName = bucketName,
            ObjectKey = objectKey,
            ContentType = contentType.ToLowerInvariant(),
            Extension = extension.ToLowerInvariant().TrimStart('.'),
            SizeInBytes = sizeInBytes,
            StorageProvider = storageProvider,
            Visibility = visibility,
            Category = category,
            PublicId = Guid.NewGuid(),
            Status = FileStatus.Created,
            UploadedByUserId = uploadedByUserId,
            IsActive = true
        };
    }

    public void MarkUploadUrlGenerated()
    {
        Status = FileStatus.UploadUrlGenerated;
    }

    public void MarkUploaded(string? checksum = null)
    {
        Status = FileStatus.Uploaded;
        UploadedAt = DateTime.UtcNow;
        if (checksum is not null) Checksum = checksum;
    }

    public void MarkProcessing()
    {
        Status = FileStatus.Processing;
    }

    public void MarkReady()
    {
        Status = FileStatus.Ready;
    }

    public void Reject()
    {
        Status = FileStatus.Rejected;
    }

    public void SoftDelete(long? deletedByUserId)
    {
        Status = FileStatus.Deleted;
        IsActive = false;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedByUserId = deletedByUserId;
    }

    public void UpdateChecksum(string checksum)
    {
        Checksum = checksum;
    }

    public void UpdateVisibility(FileVisibility visibility)
    {
        Visibility = visibility;
    }

    public void UpdateActualSize(long actualSizeInBytes)
    {
        SizeInBytes = actualSizeInBytes;
    }

    public void MarkRejected(string? reason = null)
    {
        Status = FileStatus.Rejected;
    }

    /// <summary>
    /// Marks the file as quarantined after a virus/malware scan detected a threat.
    /// Quarantined files are NOT readable and NOT attachable.
    /// </summary>
    public void MarkQuarantined()
    {
        Status = FileStatus.Quarantined;
    }

    /// <summary>
    /// Promotes the file to Ready after passing the virus/malware scan.
    /// Only valid from the Uploaded state.
    /// </summary>
    public void PromoteToReady()
    {
        Status = FileStatus.Ready;
    }
}
