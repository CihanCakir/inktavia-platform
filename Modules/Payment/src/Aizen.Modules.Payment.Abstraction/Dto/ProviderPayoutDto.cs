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

    // ── BE-P10 provider negative-balance transparency (null when the provider has no balance ledger row) ──
    /// <summary>The provider's current negative-balance ledger state (§7.4). A clawback offsets from future payouts;
    /// over the limit blocks payouts/acceptance. Null when no ledger row exists (never clawed back).</summary>
    public ProviderBalanceSummaryDto? NegativeBalance { get; init; }
}

/// <summary>BE-P10 §7.4 — the provider negative-balance ledger snapshot for transparency (read-only).</summary>
public sealed class ProviderBalanceSummaryDto
{
    /// <summary>Signed running balance; negative = the provider owes the platform (offset from future payouts first).</summary>
    public decimal Balance              { get; init; }
    public decimal NegativeAmount       { get; init; }
    public decimal NegativeBalanceLimit { get; init; }
    public bool    IsOverLimit          { get; init; }
    public string  CurrencyCode         { get; init; } = "TRY";
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
