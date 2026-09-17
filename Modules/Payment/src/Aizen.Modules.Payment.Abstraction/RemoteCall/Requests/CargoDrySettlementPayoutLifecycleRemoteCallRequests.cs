namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

// Split-host bridge requests for the CargoDry sell-through settlement payout lifecycle. The payoutRecordId travels in the
// route; the body carries the actor + operation details. Mirror the ICargoDrySettlementPayoutLifecycleService method
// signatures 1:1 so the seam is unchanged. None of these advance the SETTLEMENT to Settled — that stays in the CargoDry
// CompleteCargoDrySettlementPayout handler (these only move the PayoutRecord).

/// <summary>Body for the payout-state read (validates the payout belongs to the given settlement).</summary>
public sealed class GetCargoDrySettlementPayoutStateRemoteCallRequest
{
    public required long SourceSettlementId { get; init; }
}

/// <summary>Body for approving a Pending payout (Pending → Approved).</summary>
public sealed class ApproveCargoDrySettlementPayoutRemoteCallRequest
{
    public required long    ApprovedByUserId { get; init; }
    public          string? Note             { get; init; }
}

/// <summary>Body for marking a payout as actively processing (Approved/Pending → Processing).</summary>
public sealed class MarkProcessingCargoDrySettlementPayoutRemoteCallRequest
{
    public required long    ProcessedByUserId { get; init; }
    public          string? ExternalReference { get; init; }
    public          string? Note              { get; init; }
}

/// <summary>Body for confirming a manual disbursement (→ Completed). Idempotent on the Payment side.</summary>
public sealed class CompleteCargoDrySettlementPayoutRemoteCallRequest
{
    public required long   CompletedByUserId      { get; init; }
    public required string ManualPaymentReference { get; init; }
    public          string? Note                  { get; init; }
}

/// <summary>Body for recording a payout failure (→ Failed).</summary>
public sealed class FailCargoDrySettlementPayoutRemoteCallRequest
{
    public required long    FailedByUserId    { get; init; }
    public required string  FailureReason     { get; init; }
    public          string? ExternalReference { get; init; }
    public          string? Note              { get; init; }
}
