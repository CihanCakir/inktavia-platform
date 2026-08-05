using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request offer rejected message", "Published when owner rejects an offer.")]
public sealed class ServiceRequestOfferRejectedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OfferId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }
    /// <summary>Free-text note (N-E).</summary>
    public string? Reason { get; set; }
    /// <summary>N-E structured reject reason (owner).</summary>
    public OfferRejectReason? ReasonCode { get; set; }
}
