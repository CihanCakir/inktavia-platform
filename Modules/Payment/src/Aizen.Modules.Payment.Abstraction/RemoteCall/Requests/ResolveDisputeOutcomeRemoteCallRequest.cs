namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// BE-S13b — SR → Payment: drive the P10 refund/escrow path for a resolved dispute. Idempotent on the dispute
/// context ref <c>DISPUTE-{DisputeId}</c> so a re-resolve never double-refunds. The SR module resolves the outcome
/// into these flags (via <c>DisputeOutcomeRefundMap</c>) so Payment only executes the existing primitives
/// (RefundPaymentCommand / ReleasePaymentEscrowCommand) — no bespoke refund math.
/// </summary>
public sealed class ResolveDisputeOutcomeRemoteCallRequest
{
    public required long ServiceRequestId { get; init; }
    public required long DisputeId        { get; init; }

    /// <summary>The <c>DisputeResolutionOutcome</c> int code (audit / event only; behaviour is driven by the flags below).</summary>
    public required int OutcomeCode { get; init; }

    /// <summary>True for FavorProviderRelease — release the held escrow to the provider, no refund.</summary>
    public bool ReleaseToProvider { get; init; }

    /// <summary>True for FavorPayerFullRefund — refund the full refundable amount (<see cref="RefundAmount"/> ignored).</summary>
    public bool FullRefund { get; init; }

    /// <summary>Refund amount for a partial/split payer-favoured outcome. Validated ≤ refundable in Payment.</summary>
    public decimal? RefundAmount { get; init; }

    /// <summary>The Payment <c>RefundReason</c> int code (e.g. DisputeResolvedForPayer) — mapped to a RefundCause via RefundCauseMap.</summary>
    public int RefundReasonCode { get; init; }

    public long AdminUserId { get; init; }
    public string? Notes { get; init; }
}
