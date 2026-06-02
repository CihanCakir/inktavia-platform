using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

[DocumentationInfo("Create upload session remote call request", "Request model for the FileStorage CreateUploadSession remote call.")]
public sealed class CreateUploadSessionRemoteCallRequest
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
