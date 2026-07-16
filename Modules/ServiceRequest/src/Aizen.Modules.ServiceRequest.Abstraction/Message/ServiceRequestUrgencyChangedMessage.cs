using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request urgency changed message", "Published when a service request's priority is changed.")]
public sealed class ServiceRequestUrgencyChangedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public string? LocationCityCode { get; set; }
    public ServiceRequestPriority OldPriority { get; set; }
    public ServiceRequestPriority NewPriority { get; set; }
}
