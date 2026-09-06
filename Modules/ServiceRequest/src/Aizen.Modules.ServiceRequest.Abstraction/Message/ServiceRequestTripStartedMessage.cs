using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// Phase-2 — published when a provider marks "en route" (trip start). Drives the owner notification
/// ("Sağlayıcı yola çıktı") through the Notification module. Mirrors <see cref="ServiceRequestAssignmentStartedMessage"/>:
/// carries <c>OwnerUserId</c> so the Notification module can resolve the recipient.
/// </summary>
[DocumentationInfo("Service request trip started message", "Published on trip start — drives the owner 'provider en route' notification.")]
public sealed class ServiceRequestTripStartedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public long ProviderProfileId { get; set; }
    public long OwnerUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
