using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.File;

[DocumentationInfo("File metadata DTO", "Extended file metadata including owner references.")]
public sealed class FileMetadataDto
{
    public Guid FileId { get; set; }
    public string FileCode { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
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
    public IReadOnlyList<FileOwnerReferenceDto> OwnerReferences { get; set; } = [];
}
