namespace Aizen.Modules.Payment.Abstraction.Dto;

public sealed class ProviderTransactionDto
{
    public string  TransactionCode  { get; init; } = default!;
    public int     TransactionType  { get; init; }
    public int     ContextType      { get; init; }
    public long    ContextId        { get; init; }
    public decimal GrossAmount      { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal VatOnCommission  { get; init; }
    public decimal NetPayoutAmount  { get; init; }
    public decimal TotalRefundedAmount { get; init; }
    public string  CurrencyCode     { get; init; } = "TRY";
    public int     Status           { get; init; }
    public string? GatewayReference { get; init; }
    public DateTime  CreatedAt   { get; init; }
    public DateTime? CapturedAt  { get; init; }
    public DateTime? ReleasedAt  { get; init; }

    // ── BE-P10 refund / dispute status (additive; existing fields unchanged) ──
    /// <summary>Set when the transaction is in dispute/chargeback (BE-P10 §21.2). Null otherwise.</summary>
    public DateTime? DisputedAt  { get; init; }

    // ── BE-P8/S8 immutable economics-snapshot breakdown (null for legacy transactions with no snapshot) ──
    public long?                                    EconomicsSnapshotId { get; init; }
    public ProviderTransactionEconomicsBreakdownDto? EconomicsBreakdown { get; init; }

    // ── BE-P10 refund allocation summary (null unless the transaction has a refund with a P10 allocation) ──
    public ProviderTransactionRefundSummaryDto?      RefundSummary       { get; init; }
}

/// <summary>
/// BE-P10 — the latest refund allocation on a transaction, read from the immutable <c>RefundAllocation</c> (never recomputed).
/// <c>PlatformAdvancedRefundAmount</c>/<c>ProviderRecoveryAmount</c>/<c>RemainingProviderNegativeBalance</c> give the provider
/// clawback transparency for a release-after refund (§7.3). <c>Cause</c>/<c>ReleaseState</c> are the enum names (string).
/// </summary>
public sealed class ProviderTransactionRefundSummaryDto
{
    public string?  Cause                           { get; init; }
    public string?  ReleaseState                    { get; init; }
    public decimal  ServiceRefundAmount             { get; init; }
    public decimal  ProviderNetReversalAmount       { get; init; }
    public decimal  CommissionRevenueReversalAmount { get; init; }
    public decimal  PlatformAdvancedRefundAmount    { get; init; }
    public decimal  ProviderRecoveryAmount          { get; init; }
    public decimal  RemainingProviderNegativeBalance{ get; init; }
}

/// <summary>
/// BE-P8/S8 — the immutable economics breakdown for a transaction, read straight from its linked
/// <c>PaymentEconomicsSnapshot</c> (never recomputed). All fields are the snapshot's own figures; null container when the
/// transaction predates the snapshot (legacy). Commission-benefit is not persisted on the snapshot (BE-S8 defers it) so it is
/// not surfaced here; the discount funding lines (provider-/platform-funded) are.
/// </summary>
public sealed class ProviderTransactionEconomicsBreakdownDto
{
    public decimal ServiceAmount               { get; init; }
    public decimal ServiceVatAmount            { get; init; }
    public decimal CommissionBaseAmount        { get; init; }
    public decimal CommissionRate              { get; init; }
    public decimal CommissionAmount            { get; init; }
    public decimal ProviderNetAmount           { get; init; }
    public decimal PlatformFeeNetAmount        { get; init; }
    public decimal PlatformFeeVatAmount        { get; init; }
    public decimal PlatformFeeGrossAmount      { get; init; }
    public decimal CustomerTotalAmount         { get; init; }
    public decimal PlatformGrossShare          { get; init; }
    public decimal TotalCustomerDiscount       { get; init; }
    public decimal TotalProviderFundedDiscount { get; init; }
    public decimal TotalPlatformFundedDiscount { get; init; }
}

public sealed class ProviderTransactionPagedResultDto
{
    public List<ProviderTransactionDto> Items { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}
