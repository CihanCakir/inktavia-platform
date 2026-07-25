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
}

public sealed class ProviderTransactionPagedResultDto
{
    public List<ProviderTransactionDto> Items { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}
