
namespace Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;

[DocumentationInfo("File upload URL DTO", "Contains the pre-signed S3 upload URL and expiry information.")]
public sealed class FileUploadUrlDto
{
    public string UploadUrl { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
