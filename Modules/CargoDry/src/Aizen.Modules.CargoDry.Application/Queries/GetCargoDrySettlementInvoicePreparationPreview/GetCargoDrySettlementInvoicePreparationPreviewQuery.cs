using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementInvoicePreparationPreview;

/// <summary>
/// Returns a preview of whether a CargoDry sell-through settlement is eligible
/// for invoice preparation. Used by the admin panel before calling
/// PrepareCargoDrySettlementInvoiceCommand to surface eligibility and blocking reasons.
///
/// NEVER throws for business ineligibility — returns CanPrepare=false + BlockingReasons instead.
/// Phase 4C (July 2026).
/// </summary>
[DocumentationInfo("Get CargoDry settlement invoice preparation preview query",
    "Returns eligibility status, blocking reasons, financial summary, and existing invoice link " +
    "for a settlement. Safe to call at any time — never throws for business ineligibility. " +
    "Phase 4C (July 2026).")]
public sealed class GetCargoDrySettlementInvoicePreparationPreviewQuery
    : AizenQuery<GetCargoDrySettlementInvoicePreparationPreviewResponse>
{
    public required long SettlementId { get; init; }
}

public sealed class GetCargoDrySettlementInvoicePreparationPreviewResponse
{
    // ── Settlement identity ────────────────────────────────────────────────────
    public long                                SettlementId      { get; init; }
    public string                              SettlementCode    { get; init; } = default!;
    public CargoDrySellThroughSettlementStatus Status            { get; init; }
    public string                              StatusName        { get; init; } = default!;

    // ── Eligibility ────────────────────────────────────────────────────────────
    /// <summary>True if PrepareCargoDrySettlementInvoiceCommand can be called for this settlement.</summary>
    public bool                  CanPrepare       { get; init; }

    /// <summary>Human-readable reasons why the settlement is not eligible. Empty if CanPrepare=true.</summary>
    public IReadOnlyList<string> BlockingReasons  { get; init; } = [];

    /// <summary>Recommended next actions for the admin to resolve blocking reasons.</summary>
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];

    // ── Financials summary ─────────────────────────────────────────────────────
    public decimal ProviderPayoutAmount    { get; init; }
    public decimal TotalSaleAmount         { get; init; }
    public decimal TotalCommissionAmount   { get; init; }
    public int     TotalKitCount           { get; init; }
    public string  CurrencyCode            { get; init; } = default!;
    public string  ProductCode             { get; init; } = default!;
    public DateTime PeriodStartUtc         { get; init; }
    public DateTime PeriodEndUtc           { get; init; }

    // ── Phase 4B payout link ───────────────────────────────────────────────────
    public long?     PayoutRecordId        { get; init; }
    public DateTime? PaymentPreparedAtUtc  { get; init; }

    // ── Existing invoice link (Phase 4C) ──────────────────────────────────────
    /// <summary>True if a ProviderSettlementStatement Draft already exists for this settlement.</summary>
    public bool      InvoiceExists         { get; init; }
    public long?     ExistingInvoiceId     { get; init; }
    public DateTime? InvoicePreparedAtUtc  { get; init; }
    public long?     InvoicePreparedByUserId { get; init; }
}
