using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Responses;

[DocumentationInfo("Create upload session remote call response", "Response model for the FileStorage CreateUploadSession remote call.")]
public sealed class CreateUploadSessionRemoteCallResponse
{
    public Guid FileId { get; set; }
    public string UploadSessionCode { get; set; } = default!;
    public string UploadUrl { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
