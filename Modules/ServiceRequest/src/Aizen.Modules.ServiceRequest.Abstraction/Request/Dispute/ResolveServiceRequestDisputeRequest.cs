using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Dispute;

[DocumentationInfo("Resolve dispute request", "Admin resolves an open dispute; optionally with a monetary outcome (BE-S13b).")]
public sealed class ResolveServiceRequestDisputeRequest
{
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// BE-S13b — optional monetary outcome. When null this is a notes-only resolve (unchanged legacy behaviour: no
    /// refund, no escrow release). When set it drives the P10 refund/escrow allocation.
    /// </summary>
    public DisputeResolutionOutcome? Outcome { get; set; }

    /// <summary>
    /// BE-S13b — required for <see cref="DisputeResolutionOutcome.FavorPayerPartialRefund"/> and
    /// <see cref="DisputeResolutionOutcome.Split"/>: the amount to refund to the payer (validated ≤ refundable in
    /// Payment). Ignored for full-refund and provider-release outcomes.
    /// </summary>
    public decimal? RefundAmount { get; set; }
}
