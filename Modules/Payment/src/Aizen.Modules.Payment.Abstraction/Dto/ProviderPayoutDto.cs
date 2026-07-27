namespace Aizen.Modules.Payment.Abstraction.Dto;

public sealed class ProviderPayoutDto
{
    public long    Id               { get; init; }
    public decimal Amount            { get; init; }
    public string  CurrencyCode      { get; init; } = "TRY";
    public int     Status            { get; init; }
    public string  GatewayProvider   { get; init; } = default!;
    public string? GatewayPayoutId   { get; init; }
    public string? SourceType        { get; init; }
    public long?   SourceId          { get; init; }
    public string? Description        { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public string? HoldReason        { get; init; }
    public string? FailureReason     { get; init; }
}

public sealed class ProviderPayoutPagedResultDto
{
    public List<ProviderPayoutDto> Items { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

public sealed class ProviderPayoutSummaryDto
{
    public decimal PendingAmount    { get; init; }
    public decimal ProcessingAmount { get; init; }
    public decimal CompletedAmount  { get; init; }
    public decimal OnHoldAmount     { get; init; }
    public string  CurrencyCode     { get; init; } = "TRY";
    public DateTimeOffset ComputedAtUtc { get; init; }
}
