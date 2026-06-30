using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.ServiceRequest;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Dispute service request command", "Admin opens a dispute on a service request.")]
public sealed class DisputeServiceRequestCommand : AizenCommand<DisputeServiceRequestResponse>
{
    public long ServiceRequestId { get; }
    public DisputeServiceRequestRequest Request { get; }
    public DisputeServiceRequestCommand(long serviceRequestId, DisputeServiceRequestRequest request)
    {
        ServiceRequestId = serviceRequestId; Request = request;
    }
}
