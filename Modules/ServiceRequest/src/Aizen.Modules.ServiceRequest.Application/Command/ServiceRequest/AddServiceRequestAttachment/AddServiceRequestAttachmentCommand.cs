using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Add attachment command", "Adds a file attachment to a service request.")]
public sealed class AddServiceRequestAttachmentCommand : AizenCommand<AddServiceRequestAttachmentResponse>
{
    public long ServiceRequestId { get; }
    public AddServiceRequestAttachmentRequest Request { get; }
    public AddServiceRequestAttachmentCommand(long serviceRequestId, AddServiceRequestAttachmentRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
