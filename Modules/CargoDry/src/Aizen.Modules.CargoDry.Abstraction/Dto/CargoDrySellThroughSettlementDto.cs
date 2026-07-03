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
