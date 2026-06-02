using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Abstraction.Message;

[DocumentationInfo("Create file read URL process message", "Request/response message to generate a pre-signed S3 read URL.")]
public sealed class CreateFileReadUrlProcessMessage : AizenBaseMessage
{
    public Guid FileId { get; set; }
    public TimeSpan ExpiresIn { get; set; } = TimeSpan.FromMinutes(15);
}

[DocumentationInfo("Create file read URL process message result", "Response for the create file read URL request/response message.")]
public sealed class CreateFileReadUrlProcessMessageResult : AizenMessageResult
{
    public Guid FileId { get; set; }
    public string ReadUrl { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
