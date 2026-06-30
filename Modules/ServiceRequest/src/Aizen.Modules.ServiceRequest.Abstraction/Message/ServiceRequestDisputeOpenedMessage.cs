using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request dispute opened message", "Published when a dispute is opened.")]
public sealed class ServiceRequestDisputeOpenedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long DisputeId { get; set; }
    public long OpenedByUserId { get; set; }
    public ServiceRequestActorType OpenedByActorType { get; set; }
    public ServiceRequestDisputeReason Reason { get; set; }
}
