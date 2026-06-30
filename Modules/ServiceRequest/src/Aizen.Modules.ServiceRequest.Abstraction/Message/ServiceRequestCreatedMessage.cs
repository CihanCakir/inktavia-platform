using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request created message", "Published when a new service request is created.")]
public sealed class ServiceRequestCreatedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public long OwnerUserId { get; set; }
    public long VesselId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public ServiceRequestPriority Priority { get; set; }
}
