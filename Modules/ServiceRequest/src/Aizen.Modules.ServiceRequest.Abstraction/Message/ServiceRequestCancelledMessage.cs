using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request cancelled message", "Published when a service request is cancelled by the owner.")]
public sealed class ServiceRequestCancelledMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public string RequestCode { get; set; } = default!;
    public string? LocationCityCode { get; set; }
    public long CancelledByUserId { get; set; }

    /// <summary>
    /// N-E — the mapped Payment RefundReason (as its int value) for the structured cancel reason. The Payment
    /// refund consumer routes this through RefundCauseMap → RefundAllocationPolicy for deterministic P10 allocation.
    /// 0 / unset ⇒ the consumer falls back to ServiceRequestCancelled. Field name matches the Payment-side message.
    /// </summary>
    public int RefundReasonCode { get; set; }

    /// <summary>N-E — the free-text cancel note (audit / dispute). Field name matches the Payment-side message.</summary>
    public string? CancellationReason { get; set; }

    // ── Wave 4A — enrichment so the Notification side can notify owner/provider on a CARGODRY_SUPPLY cancel ──
    /// <summary>The owner's Identity user id (for the owner-facing cancel notification).</summary>
    public long OwnerUserId { get; set; }
    /// <summary>Service category (lets a consumer branch on CARGODRY_SUPPLY without an SR read-back).</summary>
    public string? ServiceCategoryCode { get; set; }
    /// <summary>CargoDry product code (non-null only for CARGODRY_SUPPLY).</summary>
    public string? CargoDryProductCode { get; set; }
    /// <summary>Assigned provider profile id at cancel time (null when unassigned / cargo path).</summary>
    public long? AssignedProviderProfileId { get; set; }
}
