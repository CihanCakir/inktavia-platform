using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

[DocumentationInfo("Reject offer request", "Owner rejects a provider offer.")]
public sealed class RejectServiceRequestOfferRequest
{
    public long OfferId { get; set; }

    /// <summary>N-E structured reject reason (owner).</summary>
    public OfferRejectReason? ReasonCode { get; set; }

    /// <summary>Optional free-text note (kept alongside the structured reason).</summary>
    public string? Reason { get; set; }
}
