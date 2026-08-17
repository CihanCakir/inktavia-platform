namespace Aizen.Modules.Payment.Abstraction.Dto;

/// <summary>BE-P10 — a refund record row for the admin refund queue (Cause/ReleaseState/Status as enum-int).</summary>
public sealed class RefundQueueItemDto
{
    public long      Id                   { get; init; }
    public long      PaymentTransactionId { get; init; }
    public string    RefundCode           { get; init; } = default!;
    public decimal   Amount               { get; init; }
    public string    CurrencyCode         { get; init; } = "TRY";
    public int       RefundType           { get; init; }
    public int       Reason               { get; init; }
    public int       Status               { get; init; }
    public int?      Cause                { get; init; }
    public int?      ReleaseState         { get; init; }
    public long?     RefundAllocationId   { get; init; }
    public string?   GatewayRefundReference { get; init; }
    public DateTime? ProcessedAt          { get; init; }
    public DateTime? CreatedAt            { get; init; }
}

public sealed class RefundQueuePagedDto
{
    public List<RefundQueueItemDto> Items { get; init; } = new();
    public int Total { get; init; }
    public int Page  { get; init; }
    public int PageSize { get; init; }
}

/// <summary>BE-P10 §21.2 — a chargeback record row for the admin chargeback queue.</summary>
public sealed class ChargebackQueueItemDto
{
    public long     Id                        { get; init; }
    public long     PaymentTransactionId      { get; init; }
    public string   GatewayChargebackReference { get; init; } = default!;
    public decimal  Amount                    { get; init; }
    public string   CurrencyCode              { get; init; } = "TRY";
    public decimal  ChargebackExpenseAmount   { get; init; }
    public decimal  ProviderRecoveredAmount   { get; init; }
    public decimal  RemainingNegativeBalance  { get; init; }
    public DateTime ReceivedAtUtc             { get; init; }
    public string?  Notes                     { get; init; }
}

public sealed class ChargebackQueuePagedDto
{
    public List<ChargebackQueueItemDto> Items { get; init; } = new();
    public int Total { get; init; }
    public int Page  { get; init; }
    public int PageSize { get; init; }
}
