namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProviderSettlementDto
{
    public string  SettlementCode        { get; init; } = default!;
    public string  ProductCode           { get; init; } = default!;
    public DateTime PeriodStartUtc        { get; init; }
    public DateTime PeriodEndUtc          { get; init; }
    public decimal TotalCommissionAmount  { get; init; }
    public decimal ProviderPayoutAmount   { get; init; }
    public string  CurrencyCode           { get; init; } = "TRY";
    public int     Status                 { get; init; }
    public DateTimeOffset? ScheduledSettlementDate { get; init; }
    public DateTimeOffset? SettledAtUtc            { get; init; }
    public DateTimeOffset? PayoutCompletedAtUtc    { get; init; }
}

public sealed class CargoDryProviderSettlementPagedResultDto
{
    public List<CargoDryProviderSettlementDto> Items { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

public sealed class CargoDryProviderPayoutSummaryDto
{
    public decimal PendingPayout      { get; init; }
    public decimal ReadyPayout        { get; init; }
    public decimal ScheduledPayout    { get; init; }
    public decimal PaidPayout         { get; init; }
    public decimal DisputedPayout     { get; init; }
    public string  CurrencyCode       { get; init; } = "TRY";
    public DateTimeOffset ComputedAtUtc { get; init; }
}
