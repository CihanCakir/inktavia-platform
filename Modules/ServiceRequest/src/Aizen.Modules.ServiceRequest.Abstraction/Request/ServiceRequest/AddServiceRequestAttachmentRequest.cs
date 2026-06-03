using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;

[DocumentationInfo("Add service request attachment request", "Input model for attaching a file to a service request.")]
public sealed class AddServiceRequestAttachmentRequest
{
    public Guid FileId { get; set; }
    public ServiceRequestAttachmentType AttachmentType { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}
