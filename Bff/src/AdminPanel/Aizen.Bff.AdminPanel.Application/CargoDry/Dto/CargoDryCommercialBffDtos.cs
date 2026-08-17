namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

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
    // Phase 5: Commercial rule trace
    public long?     ResolvedRuleId           { get; init; }
    public string?   ResolvedRuleSource       { get; init; }
    public string?   ResolvedRuleName         { get; init; }
    public decimal?  ResolvedRate             { get; init; }
    public DateTime? RateResolvedAtUtc        { get; init; }
    public long?     RateResolvedByUserId     { get; init; }
    public string?   RuleResolutionNote       { get; init; }
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
    // Phase 4D
    public DateTime? PayoutCompletedAtUtc      { get; init; }
    public long?     PayoutCompletedByUserId   { get; init; }
    public string?   PayoutCompletionReference { get; init; }
    public string?   PayoutFailureReason       { get; init; }
    public string?   PayoutLifecycleNote       { get; init; }
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
    // Phase 4D
    public DateTime? PayoutCompletedAtUtc      { get; init; }
    public string?   PayoutCompletionReference { get; init; }
    public string?   PayoutFailureReason       { get; init; }
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

// ── Phase 4D: Payout Lifecycle Preview & Lifecycle Response ──────────────────

/// <summary>
/// BFF DTO for GET .../payout-execution-preview.
/// Mirrors GetCargoDrySettlementPayoutExecutionPreviewResponse from the CargoDry module.
/// Shows all payout lifecycle eligibility flags (CanApprovePayout, CanCompletePayout, etc.)
/// and the live PayoutStatus from the Payment module.
/// Phase 4D (July 2026).
/// </summary>
public sealed class CargoDrySettlementPayoutExecutionPreviewBffDto
{
    // ── Settlement identity ──────────────────────────────────────────────────
    public long   SettlementId          { get; init; }
    public string SettlementCode        { get; init; } = default!;
    public int    Status                { get; init; }
    public string StatusName            { get; init; } = default!;

    // ── Financials ───────────────────────────────────────────────────────────
    public decimal  ProviderPayoutAmount { get; init; }
    public string   CurrencyCode         { get; init; } = default!;
    public string   ProductCode          { get; init; } = default!;
    public DateTime PeriodStartUtc       { get; init; }
    public DateTime PeriodEndUtc         { get; init; }

    // ── Phase prerequisites ──────────────────────────────────────────────────
    public bool      PaymentPrepared       { get; init; }
    public long?     PayoutRecordId        { get; init; }
    public DateTime? PaymentPreparedAtUtc  { get; init; }
    public bool      InvoicePrepared       { get; init; }
    public long?     InvoiceId             { get; init; }
    public DateTime? InvoicePreparedAtUtc  { get; init; }

    // ── Live payout state from Payment module ────────────────────────────────
    public int?      PayoutStatus          { get; init; }
    public string?   PayoutStatusName      { get; init; }
    public string?   ExternalReference     { get; init; }
    public DateTime? PayoutApprovedAtUtc   { get; init; }
    public DateTime? PayoutProcessingAtUtc { get; init; }
    public DateTime? PayoutCompletedAtUtc  { get; init; }
    public DateTime? PayoutFailedAtUtc     { get; init; }
    public string?   PayoutFailureReason   { get; init; }

    // ── Settlement-side closure fields ───────────────────────────────────────
    public string? PayoutCompletionReference { get; init; }
    public string? PayoutLifecycleNote       { get; init; }

    // ── Eligibility flags ────────────────────────────────────────────────────
    public bool                  CanApprovePayout    { get; init; }
    public bool                  CanMarkProcessing   { get; init; }
    public bool                  CanCompletePayout   { get; init; }
    public bool                  CanFailPayout       { get; init; }
    public IReadOnlyList<string> BlockingReasons     { get; init; } = [];
    public IReadOnlyList<string> RecommendedActions  { get; init; } = [];
}

/// <summary>
/// Flat BFF DTO for the PayoutRecord state snapshot returned by each Phase 4D lifecycle command.
/// Mirrors CargoDryPayoutLifecycleResultDto from Payment.Abstraction.
/// Phase 4D (July 2026).
/// </summary>
public sealed class CargoDryPayoutLifecycleResultBffDto
{
    public long      PayoutRecordId    { get; init; }
    public int       PayoutStatus      { get; init; }
    public long      ProviderProfileId { get; init; }
    public decimal   Amount            { get; init; }
    public string    CurrencyCode      { get; init; } = default!;
    public string?   ExternalReference { get; init; }
    public DateTime? ApprovedAtUtc     { get; init; }
    public long?     ApprovedByUserId  { get; init; }
    public DateTime? ProcessingAtUtc   { get; init; }
    public DateTime? CompletedAtUtc    { get; init; }
    public long?     CompletedByUserId { get; init; }
    public DateTime? FailedAtUtc       { get; init; }
    public long?     FailedByUserId    { get; init; }
    public string?   FailureReason     { get; init; }
    public string?   AdminNote         { get; init; }
    public bool      AlreadyCompleted  { get; init; }
    public string    Message           { get; init; } = default!;
}

/// <summary>
/// BFF DTO returned by POST .../approve-payout, .../mark-payout-processing,
/// .../complete-payout, and .../fail-payout.
/// Wraps the updated settlement DTO, the payout result snapshot, and the idempotency flag.
/// Phase 4D (July 2026).
/// </summary>
public sealed class CargoDrySettlementPayoutLifecycleResponseBffDto
{
    public CargoDrySellThroughSettlementBffDto? Settlement       { get; init; }
    public CargoDryPayoutLifecycleResultBffDto? PayoutResult     { get; init; }
    /// <summary>True only for the complete-payout call if the settlement was already Settled.</summary>
    public bool                                 AlreadyCompleted { get; init; }
}

// ── Phase 5: Commercial Rule Resolution Preview ───────────────────────────────

/// <summary>
/// BFF DTO mirroring CargoDryCommercialRuleResolutionResult from the CargoDry module.
/// Returned by GET .../commercial/rules/resolve-preview and embedded in
/// CargoDrySalesAttributionRuleResolutionPreviewBffDto.
/// Phase 5 (July 2026).
/// </summary>
public sealed class CargoDryCommercialRuleResolutionBffDto
{
    /// <summary>True when a rate was successfully resolved; false when CommercialReviewRequired.</summary>
    public bool      CanResolve          { get; init; }

    /// <summary>Resolved commission rate (0.00–1.00). Null when CanResolve is false.</summary>
    public decimal?  ResolvedRate        { get; init; }

    public string?   CurrencyCode        { get; init; }

    /// <summary>
    /// One of: AdminOverride, CommissionRuleProviderSpecific, CommissionRuleProductChannel,
    /// ConsignmentAgreement, CargoDryProductDefault, DirectSaleNoProviderShare, Unresolved.
    /// </summary>
    public string?   RuleSource          { get; init; }

    /// <summary>Id of the CommissionRule or ConsignmentAgreement that supplied the rate. Null for AdminOverride/default tiers.</summary>
    public long?     RuleId              { get; init; }

    public string?   RuleName            { get; init; }

    /// <summary>Sale price passed into the resolver (echo-back for preview tooling).</summary>
    public decimal?  SalePrice           { get; init; }

    /// <summary>Calculated provider share amount (SalePrice × ResolvedRate). Null when CanResolve is false or SalePrice not provided.</summary>
    public decimal?  ProviderShareAmount { get; init; }

    /// <summary>Calculated platform share amount (SalePrice − ProviderShareAmount). Null when CanResolve is false or SalePrice not provided.</summary>
    public decimal?  PlatformShareAmount { get; init; }

    /// <summary>Non-empty only when CanResolve is false. Lists all reasons the resolver could not find a rate.</summary>
    public IReadOnlyList<string> BlockingReasons { get; init; } = [];

    /// <summary>Advisory warnings that do not block resolution (e.g. "Rate resolved from product default, consider creating an explicit rule").</summary>
    public IReadOnlyList<string> Warnings        { get; init; } = [];
}

/// <summary>
/// BFF DTO returned by GET .../commercial/sales-attributions/{id}/rule-resolution-preview.
/// Contains the current attribution state (including any previously applied rule trace)
/// plus the full resolution result for the given preview inputs.
/// Phase 5 (July 2026).
/// </summary>
public sealed class CargoDrySalesAttributionRuleResolutionPreviewBffDto
{
    /// <summary>Current state of the attribution record (includes Phase 5 rule trace fields if already resolved).</summary>
    public CargoDrySalesAttributionBffDto?       Attribution { get; init; }

    /// <summary>Rule resolution result computed from the attribution context + optional preview overrides.</summary>
    public CargoDryCommercialRuleResolutionBffDto? Resolution  { get; init; }
}

// ── Phase 6: Settlement Automation BFF DTOs ────────────────────────────────────

public sealed class CargoDrySettlementAutomationRunBffDto
{
    public long     Id                        { get; init; }
    public string   RunCode                   { get; init; } = default!;
    public int      TargetYearMonth           { get; init; }
    public int      Mode                      { get; init; }
    public string   ModeName                  { get; init; } = default!;
    public int      Status                    { get; init; }
    public string   StatusName                { get; init; } = default!;
    public bool     AutoCompletePayout        { get; init; }
    public bool     AutoPreparePayment        { get; init; }
    public bool     AutoPrepareInvoice        { get; init; }
    public long     TriggeredByUserId         { get; init; }
    public DateTime TriggeredAtUtc            { get; init; }
    public DateTime? CompletedAtUtc           { get; init; }
    public long?    DurationMs                { get; init; }
    public int      TotalSettlementsFound     { get; init; }
    public int      TotalSettlementsEligible  { get; init; }
    public int      TotalSettlementsProcessed { get; init; }
    public int      TotalSettlementsSkipped   { get; init; }
    public int      TotalSettlementsErrored   { get; init; }
    public string?  Note                      { get; init; }
    public string?  ErrorSummary              { get; init; }
    public DateTime CreatedAtUtc              { get; init; }
    public IReadOnlyList<CargoDrySettlementAutomationRunItemBffDto> RunItems { get; init; } = [];
}

public sealed class CargoDrySettlementAutomationRunItemBffDto
{
    public long     Id                   { get; init; }
    public long     RunId                { get; init; }
    public long     SettlementId         { get; init; }
    public string   SettlementCode       { get; init; } = default!;
    public long     ProviderProfileId    { get; init; }
    public string   ProductCode          { get; init; } = default!;
    public string   CurrencyCode         { get; init; } = default!;
    public int      PeriodYearMonth      { get; init; }
    public int      StatusBefore         { get; init; }
    public string   StatusBeforeName     { get; init; } = default!;
    public string   Action               { get; init; } = default!;
    public bool     Success              { get; init; }
    public string?  ErrorMessage         { get; init; }
    public int      AttributionsResolved { get; init; }
    public int      AttributionsSkipped  { get; init; }
    public int      AttributionsErrored  { get; init; }
    public DateTime ProcessedAtUtc       { get; init; }
}

public sealed class CargoDrySettlementAutomationRunsPagedBffDto
{
    public IReadOnlyList<CargoDrySettlementAutomationRunBffDto> Items    { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

public sealed class CargoDrySettlementAutomationPreviewItemBffDto
{
    public long     SettlementId               { get; init; }
    public string   SettlementCode             { get; init; } = default!;
    public long     ProviderProfileId          { get; init; }
    public string   ProductCode                { get; init; } = default!;
    public string   CurrencyCode               { get; init; } = default!;
    public int      PeriodYearMonth            { get; init; }
    public int      CurrentStatus              { get; init; }
    public string   CurrentStatusName          { get; init; } = default!;
    public int      UnresolvedAttributionCount { get; init; }
    public int      TotalAttributionCount      { get; init; }
    public string   PredictedAction            { get; init; } = default!;
    public bool     IsEligible                 { get; init; }
    public IReadOnlyList<string> IneligibilityReasons { get; init; } = [];
}

public sealed class CargoDrySettlementAutomationPreviewBffDto
{
    public int      TargetYearMonth           { get; init; }
    public int      TotalSettlementsFound     { get; init; }
    public int      TotalEligible             { get; init; }
    public int      TotalIneligible           { get; init; }
    public int      TotalWouldMarkReady       { get; init; }
    public int      TotalWouldPreparePayment  { get; init; }
    public int      TotalWouldPrepareInvoice  { get; init; }
    public bool     AutoPreparePayment        { get; init; }
    public bool     AutoPrepareInvoice        { get; init; }
    public IReadOnlyList<CargoDrySettlementAutomationPreviewItemBffDto> Items { get; init; } = [];
}

public sealed class RunSettlementAutomationBffRequest
{
    public int      TargetYearMonth    { get; init; }
    public int      Mode               { get; init; } = 1; // DryRun = 1
    public bool     AutoPreparePayment { get; init; } = false;
    public bool     AutoPrepareInvoice { get; init; } = false;
    public long     TriggeredByUserId  { get; init; }
    public string?  Note               { get; init; }
}
