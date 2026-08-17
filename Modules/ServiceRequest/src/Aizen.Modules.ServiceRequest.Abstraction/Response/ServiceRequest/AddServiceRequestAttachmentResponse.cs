using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Add attachment response", "Response after attaching a file to a service request.")]
public sealed class AddServiceRequestAttachmentResponse(ServiceRequestAttachmentDto attachment)
{
    public ServiceRequestAttachmentDto Attachment { get; } = attachment;
}
