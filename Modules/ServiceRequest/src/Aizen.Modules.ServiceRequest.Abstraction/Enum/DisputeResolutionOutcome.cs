namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// BE-S13b — the monetary outcome an admin chooses when resolving a dispute. Drives the P10 refund/escrow path.
/// A resolve with a <c>null</c> outcome is the legacy notes-only resolve (no money moves).
///
/// <para>Crosses the SR→Payment remote-call seam as an <c>int</c> code (mirror of the Payment-side interpretation;
/// values must stay in lock-step). See <c>DisputeOutcomeRefundMap</c> for the outcome → RefundReason mapping.</para>
/// </summary>
public enum DisputeResolutionOutcome
{
    /// <summary>Rule for the payer: refund the full refundable amount. Escrow (if held) is reversed.</summary>
    FavorPayerFullRefund = 1,

    /// <summary>Rule for the payer: refund a specific amount (≤ refundable). Requires <c>RefundAmount</c>.</summary>
    FavorPayerPartialRefund = 2,

    /// <summary>Rule for the provider: release the held escrow to the provider. No refund.</summary>
    FavorProviderRelease = 3,

    /// <summary>Split outcome: refund a specific amount to the payer (≤ refundable), the remainder stays for the
    /// provider (released through the normal completion-release path). Requires <c>RefundAmount</c>.</summary>
    Split = 4,
}
