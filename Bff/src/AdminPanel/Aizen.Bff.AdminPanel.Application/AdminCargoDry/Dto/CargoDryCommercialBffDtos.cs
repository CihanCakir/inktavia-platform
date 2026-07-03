namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;

// ── Sales Attribution BFF DTOs ────────────────────────────────────────────────

public sealed class CargoDrySalesAttributionBffDto
{
    public long     Id                      { get; init; }
    public Guid?    PublicId                { get; init; }
    public long     KitId                   { get; init; }
    public string   SerialNumber            { get; init; } = default!;
    public string   KitCode                 { get; init; } = default!;
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public long?    ProviderProfileId       { get; init; }
    public int      SalesChannel            { get; init; }
    public string   SalesChannelName        { get; init; } = default!;
    public int      CommercialModel         { get; init; }
    public string   CommercialModelName     { get; init; } = default!;
    public long?    ConsignmentAgreementId  { get; init; }
    public long?    InventoryId             { get; init; }
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public decimal? SalePrice                 { get; init; }
    public decimal? CommissionRate            { get; init; }
    public decimal? CommissionAmount          { get; init; }
    public string?  CurrencyCode              { get; init; }
    // Phase 4A
    public decimal? ProviderShareAmount       { get; init; }
    public decimal? PlatformShareAmount       { get; init; }
    public bool     IsFinanciallyResolved     { get; init; }
    public DateTime? FinancialResolvedAtUtc   { get; init; }
    public long?    FinancialResolvedByUserId { get; init; }
    public string?  ResolutionNote            { get; init; }
    public long?    SellThroughSettlementId   { get; init; }
    public DateTime? AttributedAt             { get; init; }
    public long?    AttributedByUserId        { get; init; }
    public string?  ReviewNote                { get; init; }
    public long?    ReviewedByUserId          { get; init; }
    public DateTime? ReviewedAt               { get; init; }
    public DateTime CreatedAtUtc              { get; init; }
}

public sealed class CargoDrySalesAttributionListItemBffDto
{
    public long     Id                      { get; init; }
    public long     KitId                   { get; init; }
    public string   SerialNumber            { get; init; } = default!;
    public string   KitCode                 { get; init; } = default!;
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public long?    ProviderProfileId       { get; init; }
    public int      SalesChannel            { get; init; }
    public string   SalesChannelName        { get; init; } = default!;
    public int      CommercialModel         { get; init; }
    public string   CommercialModelName     { get; init; } = default!;
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public decimal? SalePrice               { get; init; }
    public decimal? CommissionAmount        { get; init; }
    public string?  CurrencyCode            { get; init; }
    public long?    SellThroughSettlementId { get; init; }
    public DateTime CreatedAtUtc            { get; init; }
}

public sealed class CargoDrySalesAttributionPagedBffDto
{
    public List<CargoDrySalesAttributionListItemBffDto> Items    { get; init; } = [];
    public int                                          Total    { get; init; }
    public int                                          Page     { get; init; }
    public int                                          PageSize { get; init; }
}

// ── Sell-Through Settlement BFF DTOs ─────────────────────────────────────────

public sealed class CargoDrySellThroughSettlementBffDto
{
    public long     Id                      { get; init; }
    public Guid?    PublicId                { get; init; }
    public string   SettlementCode          { get; init; } = default!;
    public long     ConsignmentAgreementId  { get; init; }
    public long     ProviderProfileId       { get; init; }
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public int      TotalKitCount           { get; init; }
    public int      SettledKitCount         { get; init; }
    public decimal  TotalSaleAmount         { get; init; }
    public decimal  TotalCommissionAmount   { get; init; }
    public decimal  ProviderPayoutAmount    { get; init; }
    public string   CurrencyCode            { get; init; } = default!;
    public DateTime PeriodStartUtc          { get; init; }
    public DateTime PeriodEndUtc            { get; init; }
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public DateTime? ScheduledSettlementDate { get; init; }
    public DateTime? SettledAtUtc            { get; init; }
    public long?     SettledByUserId         { get; init; }
    public string?   DisputeReason           { get; init; }
    public string?   Note                    { get; init; }
    public DateTime? ReadyForSettlementAtUtc { get; init; }
    // Phase 4B
    public long?     PayoutRecordId          { get; init; }
    public DateTime? PaymentPreparedAtUtc    { get; init; }
    public long?     PaymentPreparedByUserId { get; init; }
    public string?   PaymentPreparationNote  { get; init; }
    // Phase 4C
    public long?     InvoiceId               { get; init; }
    public DateTime? InvoicePreparedAtUtc    { get; init; }
    public long?     InvoicePreparedByUserId { get; init; }
    public string?   InvoicePreparationNote  { get; init; }
    public DateTime  CreatedAtUtc            { get; init; }
}

public sealed class CargoDrySellThroughSettlementListItemBffDto
{
    public long     Id                      { get; init; }
    public string   SettlementCode          { get; init; } = default!;
    public long     ConsignmentAgreementId  { get; init; }
    public long     ProviderProfileId       { get; init; }
    public string   ProductCode             { get; init; } = default!;
    public string?  BatchCode               { get; init; }
    public int      TotalKitCount           { get; init; }
    public int      SettledKitCount         { get; init; }
    public decimal  TotalSaleAmount         { get; init; }
    public decimal  ProviderPayoutAmount    { get; init; }
    public string   CurrencyCode            { get; init; } = default!;
    public DateTime PeriodStartUtc          { get; init; }
    public DateTime PeriodEndUtc            { get; init; }
    public int      Status                  { get; init; }
    public string   StatusName              { get; init; } = default!;
    public DateTime? ScheduledSettlementDate { get; init; }
    public DateTime? ReadyForSettlementAtUtc { get; init; }
    // Phase 4B
    public long?     PayoutRecordId          { get; init; }
    public DateTime? PaymentPreparedAtUtc    { get; init; }
    // Phase 4C
    public long?     InvoiceId               { get; init; }
    public DateTime? InvoicePreparedAtUtc    { get; init; }
    public DateTime  CreatedAtUtc            { get; init; }
}

public sealed class CargoDrySellThroughSettlementPagedBffDto
{
    public List<CargoDrySellThroughSettlementListItemBffDto> Items    { get; init; } = [];
    public int                                               Total    { get; init; }
    public int                                               Page     { get; init; }
    public int                                               PageSize { get; init; }
}

// ── Phase 4B: Settlement Payment Preparation Preview ──────────────────────────

/// <summary>
/// BFF DTO for the payment preparation eligibility preview of a sell-through settlement.
/// Mirrors the module-layer GetCargoDrySettlementPaymentPreparationPreviewResponse.
/// Phase 4B (July 2026).
/// </summary>
public sealed class CargoDrySettlementPaymentPreparationPreviewBffDto
{
    // ── Settlement identity ────────────────────────────────────────────────────
    public long   SettlementId    { get; init; }
    public string SettlementCode  { get; init; } = default!;
    public int    Status          { get; init; }
    public string StatusName      { get; init; } = default!;

    // ── Eligibility ────────────────────────────────────────────────────────────
    public bool                  CanPrepare         { get; init; }
    public IReadOnlyList<string> BlockingReasons    { get; init; } = [];
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];

    // ── Attribution counts ─────────────────────────────────────────────────────
    public int TotalAttributionCount      { get; init; }
    public int ResolvedAttributionCount   { get; init; }
    public int UnresolvedAttributionCount { get; init; }

    // ── Financials summary ─────────────────────────────────────────────────────
    public decimal ProviderPayoutAmount { get; init; }
    public string  CurrencyCode         { get; init; } = default!;

    // ── Existing payout link ───────────────────────────────────────────────────
    public bool      PayoutRecordExists     { get; init; }
    public long?     ExistingPayoutRecordId { get; init; }
    public DateTime? PaymentPreparedAtUtc   { get; init; }
    public long?     PaymentPreparedByUserId { get; init; }
}

/// <summary>
/// BFF DTO returned by POST .../prepare-payment.
/// Wraps the settlement DTO, the PayoutRecordId, and an idempotency flag.
/// Phase 4B (July 2026).
/// </summary>
public sealed class PrepareCargoDrySettlementPaymentBffResponseDto
{
    public CargoDrySellThroughSettlementBffDto? Settlement     { get; init; }
    public long                                 PayoutRecordId { get; init; }
    public bool                                 AlreadyExisted { get; init; }
}

// ── Phase 4C: Settlement Invoice Preparation Preview ──────────────────────────

/// <summary>
/// BFF DTO for the invoice preparation eligibility preview of a Scheduled sell-through settlement.
/// Mirrors the module-layer GetCargoDrySettlementInvoicePreparationPreviewResponse.
/// Phase 4C (July 2026).
/// </summary>
public sealed class CargoDrySettlementInvoicePreparationPreviewBffDto
{
    // ── Settlement identity ────────────────────────────────────────────────────
    public long   SettlementId   { get; init; }
    public string SettlementCode { get; init; } = default!;
    public int    Status         { get; init; }
    public string StatusName     { get; init; } = default!;

    // ── Eligibility ────────────────────────────────────────────────────────────
    public bool                  CanPrepare         { get; init; }
    public IReadOnlyList<string> BlockingReasons    { get; init; } = [];
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];

    // ── Financials summary ─────────────────────────────────────────────────────
    public decimal  ProviderPayoutAmount  { get; init; }
    public decimal  TotalSaleAmount       { get; init; }
    public decimal  TotalCommissionAmount { get; init; }
    public int      TotalKitCount         { get; init; }
    public string   CurrencyCode          { get; init; } = default!;
    public string   ProductCode           { get; init; } = default!;
    public DateTime PeriodStartUtc        { get; init; }
    public DateTime PeriodEndUtc          { get; init; }

    // ── Phase 4B payout link ───────────────────────────────────────────────────
    public long?     PayoutRecordId       { get; init; }
    public DateTime? PaymentPreparedAtUtc { get; init; }

    // ── Existing invoice link (Phase 4C) ──────────────────────────────────────
    public bool      InvoiceExists           { get; init; }
    public long?     ExistingInvoiceId       { get; init; }
    public DateTime? InvoicePreparedAtUtc    { get; init; }
    public long?     InvoicePreparedByUserId { get; init; }
}

/// <summary>
/// BFF DTO returned by POST .../prepare-invoice.
/// Wraps the settlement DTO, the InvoiceId, and an idempotency flag.
/// Phase 4C (July 2026).
/// </summary>
public sealed class PrepareCargoDrySettlementInvoiceBffResponseDto
{
    public CargoDrySellThroughSettlementBffDto? Settlement     { get; init; }
    public long                                 InvoiceId      { get; init; }
    public bool                                 AlreadyExisted { get; init; }
}
