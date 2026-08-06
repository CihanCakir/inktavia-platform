using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using PayEnum = Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.Mapping;

/// <summary>
/// BE-S13b — the SR half of the dispute-outcome → refund chain: resolves a <see cref="DisputeResolutionOutcome"/> into
/// the flags Payment needs to drive the existing P10 primitives (RefundPaymentCommand / ReleasePaymentEscrowCommand).
/// The Payment half (RefundReason → RefundCause) is <c>RefundCauseMap</c>. Pure / deterministic — no side effects.
///
/// <para>All payer-favoured outcomes map to <see cref="PayEnum.RefundReason.DisputeResolvedForPayer"/> (→
/// <c>RefundCause.DisputeCustomerFavoured</c>). <see cref="DisputeResolutionOutcome.FavorProviderRelease"/> is a
/// release, not a refund, so it carries no refund reason.</para>
/// </summary>
public static class DisputeOutcomeRefundMap
{
    /// <summary>True when the outcome releases the held escrow to the provider (no refund).</summary>
    public static bool ReleasesEscrow(DisputeResolutionOutcome outcome)
        => outcome == DisputeResolutionOutcome.FavorProviderRelease;

    /// <summary>True when the outcome refunds the full refundable amount (the caller-supplied amount is ignored).</summary>
    public static bool IsFullRefund(DisputeResolutionOutcome outcome)
        => outcome == DisputeResolutionOutcome.FavorPayerFullRefund;

    /// <summary>True when the outcome requires an explicit refund amount (partial / split).</summary>
    public static bool RequiresAmount(DisputeResolutionOutcome outcome)
        => outcome is DisputeResolutionOutcome.FavorPayerPartialRefund or DisputeResolutionOutcome.Split;

    /// <summary>The Payment RefundReason a refund outcome maps to. Release carries no refund reason (returns the same
    /// payer-favoured reason as a harmless default — it is never used on the release path).</summary>
    public static PayEnum.RefundReason ToRefundReason(DisputeResolutionOutcome outcome)
        => PayEnum.RefundReason.DisputeResolvedForPayer;
}
