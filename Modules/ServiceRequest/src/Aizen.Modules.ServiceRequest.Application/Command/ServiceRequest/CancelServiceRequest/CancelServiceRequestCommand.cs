using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Cancel service request command", "Cancels a service request with optional reason.")]
public sealed class CancelServiceRequestCommand : AizenCommand<CancelServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public CancelServiceRequestRequest Request { get; }
    public CancelServiceRequestCommand(long serviceRequestId, CancelServiceRequestRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
