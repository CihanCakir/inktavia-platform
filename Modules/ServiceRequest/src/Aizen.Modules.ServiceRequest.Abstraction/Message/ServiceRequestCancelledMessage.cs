using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request cancelled message", "Published when a service request is cancelled by the owner.")]
public sealed class ServiceRequestCancelledMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public string? LocationCityCode { get; set; }
    public long CancelledByUserId { get; set; }
}
