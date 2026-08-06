using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;

[DocumentationInfo("ServiceRequest dispute entity", "Dispute opened by owner, provider, or admin when completion is contested.")]
public sealed class ServiceRequestDisputeEntity : AizenEntityWithAudit
{
    public long ServiceRequestId { get; private set; }
    public long OpenedByUserId { get; private set; }
    public ServiceRequestActorType OpenedByActorType { get; private set; }
    public ServiceRequestDisputeStatus Status { get; private set; }
    public ServiceRequestDisputeReason Reason { get; private set; }
    public string Description { get; private set; } = default!;
    public string? ResolutionNotes { get; private set; }
    public long? ResolvedByAdminUserId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime OpenedAt { get; private set; }

    // ── BE-S13b — monetary resolution outcome (append-only; null for a notes-only resolve) ──
    /// <summary>The <see cref="DisputeResolutionOutcome"/> chosen when resolving; null = notes-only.</summary>
    public DisputeResolutionOutcome? ResolutionOutcome { get; private set; }
    /// <summary>Amount refunded to the payer as part of the outcome (0 for provider-release / notes-only).</summary>
    public decimal? ResolutionRefundAmount { get; private set; }
    /// <summary>UTC time the P10 outcome was applied. The idempotency anchor: set once, a re-resolve never re-applies.</summary>
    public DateTime? PaymentOutcomeAppliedAt { get; private set; }

    /// <summary>True once the monetary outcome has been driven into Payment — a re-resolve must not double-refund.</summary>
    public bool IsPaymentOutcomeApplied => PaymentOutcomeAppliedAt.HasValue;

    public ServiceRequestDisputeEntity() { }

    public static ServiceRequestDisputeEntity Create(
        long serviceRequestId,
        long openedByUserId,
        ServiceRequestActorType openedByActorType,
        ServiceRequestDisputeReason reason,
        string description)
    {
        return new ServiceRequestDisputeEntity
        {
            ServiceRequestId = serviceRequestId,
            OpenedByUserId = openedByUserId,
            OpenedByActorType = openedByActorType,
            Status = ServiceRequestDisputeStatus.Open,
            Reason = reason,
            Description = description.Trim(),
            OpenedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void ChangeStatus(ServiceRequestDisputeStatus newStatus) => Status = newStatus;

    public void Resolve(long adminUserId, string? resolutionNotes)
    {
        Status = ServiceRequestDisputeStatus.Resolved;
        ResolutionNotes = resolutionNotes;
        ResolvedByAdminUserId = adminUserId;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// BE-S13b — record that a monetary outcome was driven into Payment. Idempotent: only stamps once, so a re-resolve
    /// keeps the original applied outcome and never re-triggers a refund/release.
    /// </summary>
    public void MarkPaymentOutcomeApplied(DisputeResolutionOutcome outcome, decimal refundAmount)
    {
        if (IsPaymentOutcomeApplied) return;
        ResolutionOutcome = outcome;
        ResolutionRefundAmount = refundAmount;
        PaymentOutcomeAppliedAt = DateTime.UtcNow;
    }
}
