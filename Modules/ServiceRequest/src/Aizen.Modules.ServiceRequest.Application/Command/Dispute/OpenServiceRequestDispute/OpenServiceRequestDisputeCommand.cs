using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;

namespace Aizen.Modules.ServiceRequest.Application.Command.Dispute;

[DocumentationInfo("Open dispute command", "Opens a dispute on a service request.")]
public sealed class OpenServiceRequestDisputeCommand : AizenCommand<OpenServiceRequestDisputeResponse>
{
    public long ServiceRequestId { get; }
    public ServiceRequestActorType ActorType { get; }
    public OpenServiceRequestDisputeRequest Request { get; }
    public OpenServiceRequestDisputeCommand(long serviceRequestId, ServiceRequestActorType actorType, OpenServiceRequestDisputeRequest request)
    {
        ServiceRequestId = serviceRequestId; ActorType = actorType; Request = request;
    }
}
