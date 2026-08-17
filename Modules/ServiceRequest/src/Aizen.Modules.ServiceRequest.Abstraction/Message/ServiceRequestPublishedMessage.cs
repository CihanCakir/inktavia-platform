using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// A service request has become biddable.
///
/// This — not <see cref="ServiceRequestCreatedMessage"/> — is the event providers care about: a request is created
/// as a Draft and is invisible to them until it is published. Fanning out on "created" would advertise work that
/// the owner has not offered to anyone yet.
///
/// The MarineProvider BFF consumes this and pushes it to the providers who operate in <see cref="LocationCityCode"/>.
/// City equality, not radius — proximity search belongs to GeoDiscovery, which does not exist yet and is not needed
/// for this.
/// </summary>
[DocumentationInfo("Service request published message", "Published when a service request becomes open for offers.")]
public sealed class ServiceRequestPublishedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public long OwnerUserId { get; set; }
    public string ServiceCategoryCode { get; set; } = default!;
    public ServiceRequestPriority Priority { get; set; }
    public string? LocationCityCode { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? LocationMarinaName { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}
