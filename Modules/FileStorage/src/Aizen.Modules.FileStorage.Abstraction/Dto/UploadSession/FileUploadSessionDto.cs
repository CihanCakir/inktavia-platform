using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;

[DocumentationInfo("File upload session DTO", "Represents an active S3 pre-signed upload session.")]
public sealed class FileUploadSessionDto
{
    public Guid FileId { get; set; }
    public string UploadSessionCode { get; set; } = default!;
    public string UploadUrl { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public UploadSessionStatus Status { get; set; }
    public string RequestedFileName { get; set; } = default!;
    public string RequestedContentType { get; set; } = default!;
    public long RequestedSizeInBytes { get; set; }
}
