namespace Aizen.Modules.Payment.Abstraction.Dto;

/// <summary>BE-P10 §7.4 — a movement on the provider negative-balance ledger (MovementType as enum-int).</summary>
public sealed class ProviderBalanceMovementDto
{
    public long     Id                 { get; init; }
    public int      MovementType       { get; init; }
    public decimal  Amount             { get; init; }
    public decimal  BalanceAfter       { get; init; }
    public long?    RefundRecordId     { get; init; }
    public long?    ChargebackRecordId { get; init; }
    public long?    AdminUserId        { get; init; }
    public string?  Note               { get; init; }
    public DateTime OccurredAtUtc      { get; init; }
}

/// <summary>BE-P10 §7.4 — a provider's negative-balance ledger (with movements when fetched by provider).</summary>
public sealed class ProviderBalanceAdminDto
{
    public long     Id                   { get; init; }
    public long     ProviderProfileId    { get; init; }
    public string   CurrencyCode         { get; init; } = "TRY";
    public decimal  Balance              { get; init; }
    public decimal  NegativeAmount       { get; init; }
    public decimal  NegativeBalanceLimit { get; init; }
    public bool     IsOverLimit          { get; init; }
    public List<ProviderBalanceMovementDto> Movements { get; init; } = new();
}

public sealed class ProviderBalancePagedDto
{
    public List<ProviderBalanceAdminDto> Items { get; init; } = new();
    public int Total { get; init; }
    public int Page  { get; init; }
    public int PageSize { get; init; }
}

/// <summary>BE-P10 §7.4 — result of an audited manual balance adjustment.</summary>
public sealed record ProviderBalanceAdjustResultDto(long ProviderProfileId, decimal Balance, decimal NegativeAmount);
