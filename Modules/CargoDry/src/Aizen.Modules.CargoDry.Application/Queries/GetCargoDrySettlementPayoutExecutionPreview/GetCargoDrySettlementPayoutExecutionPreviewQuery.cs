using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementPayoutExecutionPreview;

/// <summary>
/// Returns an eligibility preview for the payout execution lifecycle of a
/// CargoDry sell-through settlement. Surfaces the current payout record state,
/// which lifecycle actions (Approve, MarkProcessing, Complete, Fail) are available,
/// and any blocking reasons.
///
/// Safe to call at any time — never throws for business ineligibility.
/// Phase 4D (July 2026).
/// </summary>
[DocumentationInfo("Get CargoDry settlement payout execution preview query",
    "Returns current payout lifecycle state and eligibility flags for Approve, MarkProcessing, " +
    "Complete, and Fail actions on a CargoDry sell-through settlement payout. " +
    "Never throws for business ineligibility — returns CanXxx=false + BlockingReasons. " +
    "Phase 4D (July 2026).")]
public sealed class GetCargoDrySettlementPayoutExecutionPreviewQuery
    : AizenQuery<GetCargoDrySettlementPayoutExecutionPreviewResponse>
{
    public required long SettlementId { get; init; }
}

public sealed class GetCargoDrySettlementPayoutExecutionPreviewResponse
{
    // ── Settlement identity ────────────────────────────────────────────────────
    public long                                 SettlementId    { get; init; }
    public string                               SettlementCode  { get; init; } = default!;
    public CargoDrySellThroughSettlementStatus  Status          { get; init; }
    public string                               StatusName      { get; init; } = default!;
    public decimal                              ProviderPayoutAmount { get; init; }
    public string                               CurrencyCode    { get; init; } = default!;
    public string                               ProductCode     { get; init; } = default!;
    public DateTime                             PeriodStartUtc  { get; init; }
    public DateTime                             PeriodEndUtc    { get; init; }

    // ── Phase prerequisite checks ──────────────────────────────────────────────
    /// <summary>True if PayoutRecordId is set (Phase 4B completed).</summary>
    public bool    PaymentPrepared         { get; init; }
    public long?   PayoutRecordId          { get; init; }
    public DateTime? PaymentPreparedAtUtc  { get; init; }

    /// <summary>True if InvoiceId is set (Phase 4C completed).</summary>
    public bool    InvoicePrepared         { get; init; }
    public long?   InvoiceId               { get; init; }
    public DateTime? InvoicePreparedAtUtc  { get; init; }

    // ── Current payout state (from Payment module, null if PayoutRecordId missing) ──
    public PayoutStatus? PayoutStatus      { get; init; }
    public string?       PayoutStatusName  { get; init; }
    public string?       ExternalReference { get; init; }
    public DateTime?     PayoutApprovedAtUtc    { get; init; }
    public DateTime?     PayoutProcessingAtUtc  { get; init; }
    public DateTime?     PayoutCompletedAtUtc   { get; init; }
    public DateTime?     PayoutFailedAtUtc      { get; init; }
    public string?       PayoutFailureReason    { get; init; }

    // ── CargoDry-side payout closure state ────────────────────────────────────
    public string?   PayoutCompletionReference  { get; init; }
    public string?   PayoutLifecycleNote        { get; init; }

    // ── Lifecycle action eligibility ──────────────────────────────────────────
    /// <summary>True if ApproveCargoDrySettlementPayoutCommand can be called.</summary>
    public bool CanApprovePayout    { get; init; }

    /// <summary>True if MarkCargoDrySettlementPayoutProcessingCommand can be called.</summary>
    public bool CanMarkProcessing   { get; init; }

    /// <summary>True if CompleteCargoDrySettlementPayoutCommand can be called.</summary>
    public bool CanCompletePayout   { get; init; }

    /// <summary>True if FailCargoDrySettlementPayoutCommand can be called.</summary>
    public bool CanFailPayout       { get; init; }

    public IReadOnlyList<string> BlockingReasons    { get; init; } = [];
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];
}
