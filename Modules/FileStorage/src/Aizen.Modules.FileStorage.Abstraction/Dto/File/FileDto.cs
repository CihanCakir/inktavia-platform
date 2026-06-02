using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.File;

[DocumentationInfo("File DTO", "Core file metadata returned from FileStorage operations.")]
public sealed class FileDto
{
    public Guid FileId { get; set; }
    public string FileCode { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
    public string StoredFileName { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string Extension { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public string? Checksum { get; set; }
    public StorageProviderType StorageProvider { get; set; }
    public FileVisibility Visibility { get; set; }
    public FileCategory Category { get; set; }
    public FileStatus Status { get; set; }
    public DateTime? UploadedAt { get; set; }
    public long? UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
