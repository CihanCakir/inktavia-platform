
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

[DocumentationInfo("Remove service request attachment request", "Input model for removing an attachment from a service request.")]
public sealed class RemoveServiceRequestAttachmentRequest
{
    public long AttachmentId { get; set; }
}
