using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request status changed message", "Published when a service request status transitions.")]
public sealed class ServiceRequestStatusChangedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public ServiceRequestStatus FromStatus { get; set; }
    public ServiceRequestStatus ToStatus { get; set; }
    public long? ActorUserId { get; set; }
    public ServiceRequestActorType ActorType { get; set; }
}
