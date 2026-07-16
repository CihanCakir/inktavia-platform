using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request updated message", "Published when a service request's details are updated (title, description, dates, etc.).")]
public sealed class ServiceRequestUpdatedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public string? LocationCityCode { get; set; }
}
