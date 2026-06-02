using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Dto.Access;

[DocumentationInfo("File access URL DTO", "Contains the pre-signed S3 read URL for a file.")]
public sealed class FileAccessUrlDto
{
    public Guid FileId { get; set; }
    public string ReadUrl { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
