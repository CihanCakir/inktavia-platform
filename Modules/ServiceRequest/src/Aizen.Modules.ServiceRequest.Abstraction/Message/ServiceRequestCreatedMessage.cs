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

    /// <summary>Human-readable, so a consumer can render a notification without calling back into this module.</summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// Where the work is. The provider BFF fans this event out to the providers who operate in that city — city
    /// equality, not a radius search. Proximity/geo discovery is a different module and deliberately not this one.
    /// </summary>
    public string? LocationCityCode { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationMarinaName { get; set; }
}
