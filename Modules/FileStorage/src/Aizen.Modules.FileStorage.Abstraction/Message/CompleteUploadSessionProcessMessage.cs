using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Enum;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Complete upload session process message", "Request/response message to complete an S3 upload session.")]
public sealed class CompleteUploadSessionProcessMessage : AizenBaseMessage
{
    public string UploadSessionCode { get; set; } = default!;
    public string? Checksum { get; set; }
}

[DocumentationInfo("Complete upload session process message result", "Response for the complete upload session request/response message.")]
public sealed class CompleteUploadSessionProcessMessageResult : AizenMessageResult
{
    public Guid FileId { get; set; }
    public FileStatus Status { get; set; }
}
