using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementPaymentPreparationPreview;

/// <summary>
/// Returns a preview of whether a CargoDry sell-through settlement is eligible
/// for payment preparation. Used by the admin panel before calling
/// PrepareCargoDrySettlementPaymentCommand to surface eligibility and blocking reasons.
///
/// NEVER throws for business ineligibility — returns CanPrepare=false + BlockingReasons instead.
/// Phase 4B (July 2026).
/// </summary>
[DocumentationInfo("Get CargoDry settlement payment preparation preview query",
    "Returns eligibility status, blocking reasons, attribution counts, and existing payout links " +
    "for a settlement. Safe to call at any time — never throws for business ineligibility. " +
    "Phase 4B (July 2026).")]
public sealed class GetCargoDrySettlementPaymentPreparationPreviewQuery
    : AizenQuery<GetCargoDrySettlementPaymentPreparationPreviewResponse>
{
    public required long SettlementId { get; init; }
}

public sealed class GetCargoDrySettlementPaymentPreparationPreviewResponse
{
    // ── Settlement identity ────────────────────────────────────────────────────
    public long                                SettlementId      { get; init; }
    public string                              SettlementCode    { get; init; } = default!;
    public CargoDrySellThroughSettlementStatus Status            { get; init; }
    public string                              StatusName        { get; init; } = default!;

    // ── Eligibility ────────────────────────────────────────────────────────────
    /// <summary>True if PrepareCargoDrySettlementPaymentCommand can be called for this settlement.</summary>
    public bool                  CanPrepare       { get; init; }

    /// <summary>Human-readable reasons why the settlement is not eligible. Empty if CanPrepare=true.</summary>
    public IReadOnlyList<string> BlockingReasons  { get; init; } = [];

    /// <summary>Recommended next actions for the admin to resolve blocking reasons.</summary>
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];

    // ── Attribution counts ─────────────────────────────────────────────────────
    public int  TotalAttributionCount      { get; init; }
    public int  ResolvedAttributionCount   { get; init; }
    public int  UnresolvedAttributionCount { get; init; }

    // ── Financials summary ─────────────────────────────────────────────────────
    public decimal ProviderPayoutAmount    { get; init; }
    public string  CurrencyCode            { get; init; } = default!;

    // ── Existing payout link ───────────────────────────────────────────────────
    /// <summary>True if a PayoutRecord has already been created for this settlement.</summary>
    public bool   PayoutRecordExists    { get; init; }
    public long?  ExistingPayoutRecordId { get; init; }
    public DateTime? PaymentPreparedAtUtc { get; init; }
    public long?  PaymentPreparedByUserId { get; init; }
}
