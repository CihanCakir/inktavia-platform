using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>Full sell-through settlement record DTO used in detail queries.</summary>
public sealed class CargoDrySellThroughSettlementDto
{
    public long                                 Id                      { get; init; }
    public Guid?                                PublicId                { get; init; }
    public string                               SettlementCode          { get; init; } = default!;

    // ── Scope ───────────────────────────────────────────────────────────────────
    public long                                 ConsignmentAgreementId  { get; init; }
    public long                                 ProviderProfileId       { get; init; }
    public string                               ProductCode             { get; init; } = default!;
    public string?                              BatchCode               { get; init; }

    // ── Kit counts ──────────────────────────────────────────────────────────────
    public int                                  TotalKitCount           { get; init; }
    public int                                  SettledKitCount         { get; init; }

    // ── Financials ──────────────────────────────────────────────────────────────
    public decimal                              TotalSaleAmount         { get; init; }
    public decimal                              TotalCommissionAmount   { get; init; }
    public decimal                              ProviderPayoutAmount    { get; init; }
    public string                               CurrencyCode            { get; init; } = default!;

    // ── Period ──────────────────────────────────────────────────────────────────
    public DateTime                             PeriodStartUtc          { get; init; }
    public DateTime                             PeriodEndUtc            { get; init; }

    // ── Status ──────────────────────────────────────────────────────────────────
    public CargoDrySellThroughSettlementStatus  Status                  { get; init; }
    public string                               StatusName              { get; init; } = default!;

    // ── Settlement audit ────────────────────────────────────────────────────────
    public DateTime?                            ScheduledSettlementDate { get; init; }
    public DateTime?                            SettledAtUtc            { get; init; }
    public long?                                SettledByUserId         { get; init; }
    public string?                              DisputeReason           { get; init; }
    public string?                              Note                    { get; init; }

    /// <summary>Phase 4A: UTC timestamp when settlement was marked ReadyForSettlement.</summary>
    public DateTime?                            ReadyForSettlementAtUtc { get; init; }

    // ── Payment preparation (Phase 4B) ──────────────────────────────────────────
    /// <summary>Phase 4B: Cross-module ref to Payment.PayoutRecordEntity. Null until MarkPaymentPrepared() is called.</summary>
    public long?                                PayoutRecordId             { get; init; }
    public DateTime?                            PaymentPreparedAtUtc       { get; init; }
    public long?                                PaymentPreparedByUserId    { get; init; }
    public string?                              PaymentPreparationNote     { get; init; }

    // ── Invoice preparation (Phase 4C) ──────────────────────────────────────────
    /// <summary>Phase 4C: Cross-module ref to Payment.InvoiceHeaderEntity (ProviderSettlementStatement). Null until MarkInvoicePrepared() is called.</summary>
    public long?                                InvoiceId                  { get; init; }
    public DateTime?                            InvoicePreparedAtUtc       { get; init; }
    public long?                                InvoicePreparedByUserId    { get; init; }
    public string?                              InvoicePreparationNote     { get; init; }

    // ── Payout lifecycle / closure (Phase 4D) ───────────────────────────────────
    public DateTime?                            PayoutCompletedAtUtc       { get; init; }
    public long?                                PayoutCompletedByUserId    { get; init; }
    public string?                              PayoutCompletionReference  { get; init; }
    public string?                              PayoutFailureReason        { get; init; }
    public string?                              PayoutLifecycleNote        { get; init; }

    public DateTime                             CreatedAtUtc            { get; init; }
}

/// <summary>Lightweight item for paged settlement list queries.</summary>
public sealed class CargoDrySellThroughSettlementListItemDto
{
    public long                                 Id                      { get; init; }
    public string                               SettlementCode          { get; init; } = default!;
    public long                                 ConsignmentAgreementId  { get; init; }
    public long                                 ProviderProfileId       { get; init; }
    public string                               ProductCode             { get; init; } = default!;
    public string?                              BatchCode               { get; init; }
    public int                                  TotalKitCount           { get; init; }
    public int                                  SettledKitCount         { get; init; }
    public decimal                              TotalSaleAmount         { get; init; }
    public decimal                              ProviderPayoutAmount    { get; init; }
    public string                               CurrencyCode            { get; init; } = default!;
    public DateTime                             PeriodStartUtc          { get; init; }
    public DateTime                             PeriodEndUtc            { get; init; }
    public CargoDrySellThroughSettlementStatus  Status                  { get; init; }
    public string                               StatusName              { get; init; } = default!;
    public DateTime?                            ScheduledSettlementDate { get; init; }
    public DateTime?                            ReadyForSettlementAtUtc { get; init; }
    // ── Payment preparation (Phase 4B) ──────────────────────────────────────────
    public long?                                PayoutRecordId          { get; init; }
    public DateTime?                            PaymentPreparedAtUtc    { get; init; }
    // ── Invoice preparation (Phase 4C) ──────────────────────────────────────────
    public long?                                InvoiceId               { get; init; }
    public DateTime?                            InvoicePreparedAtUtc    { get; init; }
    // ── Payout lifecycle / closure (Phase 4D) ───────────────────────────────────
    public DateTime?                            PayoutCompletedAtUtc    { get; init; }
    public string?                              PayoutCompletionReference { get; init; }
    public string?                              PayoutFailureReason     { get; init; }
    public DateTime                             CreatedAtUtc            { get; init; }
}

/// <summary>Paged result wrapper for sell-through settlement list queries.</summary>
public sealed class CargoDrySellThroughSettlementPagedResultDto
{
    public List<CargoDrySellThroughSettlementListItemDto> Items    { get; init; } = [];
    public int                                            Total    { get; init; }
    public int                                            Page     { get; init; }
    public int                                            PageSize { get; init; }
}
