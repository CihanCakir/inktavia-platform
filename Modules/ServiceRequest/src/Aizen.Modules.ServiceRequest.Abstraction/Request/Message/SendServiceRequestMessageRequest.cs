
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Message;

[DocumentationInfo("Send message request", "Send a message scoped to a service request.")]
public sealed class SendServiceRequestMessageRequest
{
    public string Content { get; set; } = default!;
    public Guid? AttachmentFileId { get; set; }
}
