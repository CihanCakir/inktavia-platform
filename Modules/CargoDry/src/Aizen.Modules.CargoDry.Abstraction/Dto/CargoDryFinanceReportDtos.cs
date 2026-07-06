namespace Aizen.Modules.CargoDry.Abstraction.Dto;

// ── Settlement / Payout Reconciliation Report ─────────────────────────────────

/// <summary>
/// A single row in the settlement-payout reconciliation report.
/// All mismatch flags are computed server-side from entity state.
/// Phase 15 (July 2026).
/// </summary>
public sealed class CargoDrySettlementReconciliationRowDto
{
    public long      SettlementId            { get; init; }
    public string    SettlementCode          { get; init; } = default!;
    public long      ProviderProfileId       { get; init; }
    public string    ProductCode             { get; init; } = default!;
    public string    CurrencyCode            { get; init; } = default!;
    public string    SettlementStatus        { get; init; } = default!;
    public int       AttributionCount        { get; init; }
    public decimal   TotalSaleAmount         { get; init; }
    public decimal   ProviderPayoutAmount    { get; init; }
    public decimal   PlatformShareAmount     { get; init; }
    public long?     PayoutRecordId          { get; init; }
    public string?   PayoutStatus            { get; init; }
    public long?     InvoiceId               { get; init; }
    public string?   InvoiceStatus           { get; init; }
    public DateTime? PaymentPreparedAtUtc    { get; init; }
    public DateTime? InvoicePreparedAtUtc    { get; init; }
    public DateTime? PayoutCompletedAtUtc    { get; init; }
    public DateTime  CreatedAtUtc            { get; init; }
    public List<string> MismatchFlags        { get; init; } = new();
    public List<string> Warnings             { get; init; } = new();
}

/// <summary>Paged reconciliation report result for settlement-payout rows.</summary>
public sealed class CargoDrySettlementReconciliationReportDto
{
    public List<CargoDrySettlementReconciliationRowDto> Items    { get; init; } = new();
    public int                                          Total    { get; init; }
    public int                                          Page     { get; init; }
    public int                                          PageSize { get; init; }
    public int                                          MismatchCount { get; init; }
}

// ── Renewal Billing Reconciliation Report ─────────────────────────────────────

/// <summary>
/// A single row in the renewal billing reconciliation report.
/// Phase 15 (July 2026).
/// </summary>
public sealed class CargoDryRenewalReconciliationRowDto
{
    public long             RenewalPreparationId        { get; init; }
    public string           RenewalCode                 { get; init; } = default!;
    public long             KitId                       { get; init; }
    public string           KitCode                     { get; init; } = default!;
    public long?            OwnerUserId                 { get; init; }
    public long?            VesselId                    { get; init; }
    public string           ProductCode                 { get; init; } = default!;
    public string           Status                      { get; init; } = default!;
    public decimal          RenewalPrice                { get; init; }
    public string           CurrencyCode                { get; init; } = default!;
    public long?            InvoiceId                   { get; init; }
    public string?          ManualPaymentReference      { get; init; }
    public string           NotificationStatus          { get; init; } = default!;
    public DateTimeOffset?  CompletedAtUtc              { get; init; }
    public DateTimeOffset?  NewExpiresAtUtc             { get; init; }
    public DateTimeOffset   PreparedAtUtc               { get; init; }
    public List<string>     MismatchFlags               { get; init; } = new();
    public List<string>     Warnings                    { get; init; } = new();
}

/// <summary>Paged reconciliation report result for renewal billing rows.</summary>
public sealed class CargoDryRenewalReconciliationReportDto
{
    public List<CargoDryRenewalReconciliationRowDto> Items    { get; init; } = new();
    public int                                       Total    { get; init; }
    public int                                       Page     { get; init; }
    public int                                       PageSize { get; init; }
    public int                                       MismatchCount { get; init; }
}

// ── Commission Rule Usage Report ──────────────────────────────────────────────

/// <summary>
/// Aggregated usage row for a single commission rule, derived from sales attribution records.
/// Phase 15 (July 2026).
/// </summary>
public sealed class CargoDryCommissionRuleUsageRowDto
{
    public long?    RuleId                 { get; init; }
    public string?  RuleName               { get; init; }
    public string?  ResolvedRuleSource     { get; init; }
    public string?  ProductCode            { get; init; }
    public string?  SalesChannel           { get; init; }
    public long?    ProviderProfileId      { get; init; }
    public int      UsageCount             { get; init; }
    public decimal  TotalSaleAmount        { get; init; }
    public decimal  TotalProviderShareAmount  { get; init; }
    public decimal  TotalPlatformShareAmount  { get; init; }
    public DateTime? FirstUsedAtUtc        { get; init; }
    public DateTime? LastUsedAtUtc         { get; init; }
}

/// <summary>Paged commission rule usage report result.</summary>
public sealed class CargoDryCommissionRuleUsageReportDto
{
    public List<CargoDryCommissionRuleUsageRowDto> Items    { get; init; } = new();
    public int                                     Total    { get; init; }
    public int                                     Page     { get; init; }
    public int                                     PageSize { get; init; }
}
