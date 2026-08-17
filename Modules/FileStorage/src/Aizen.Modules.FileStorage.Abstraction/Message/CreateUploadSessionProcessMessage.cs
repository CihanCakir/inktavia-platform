using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Create upload session process message", "Request/response message to create an S3 pre-signed upload session.")]
public sealed class CreateUploadSessionProcessMessage : AizenBaseMessage
{
    public string OriginalFileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeInBytes { get; set; }
    public FileCategory Category { get; set; }
    public FileVisibility Visibility { get; set; }
    public string? OwnerModule { get; set; }
    public string? OwnerEntityType { get; set; }
    public Guid? OwnerEntityId { get; set; }
    public long RequestedByUserId { get; set; }
}

[DocumentationInfo("Create upload session process message result", "Response for the create upload session request/response message.")]
public sealed class CreateUploadSessionProcessMessageResult : AizenMessageResult
{
    public Guid FileId { get; set; }
    public string UploadSessionCode { get; set; } = default!;
    public string UploadUrl { get; set; } = default!;
    public string ObjectKey { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
