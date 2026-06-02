using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

[DocumentationInfo("Create upload session request", "Parameters required to create a new S3 pre-signed upload session.")]
public sealed class CreateUploadSessionRequest
{
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public FileCategory Category { get; set; }
    public FileVisibility Visibility { get; set; } = FileVisibility.Private;
    public string? OwnerModule { get; set; }
    public string? OwnerEntityType { get; set; }
    public Guid? OwnerEntityId { get; set; }
}
