namespace Aizen.Modules.Payment.Abstraction.Dto;

/// <summary>BE-P10 §7.2 — a per-cause platform-fee refund rule on a policy (Cause / Mode as enum-int).</summary>
public sealed class RefundAllocationPolicyRuleDto
{
    public int      Cause                  { get; init; }
    public int      PlatformFeeRefundMode  { get; init; }
    public decimal? FixedPlatformFeeAmount { get; init; }
}

/// <summary>BE-P10 §7.2 — a refund-allocation policy (single-active per currency) with its per-cause rules.</summary>
public sealed class RefundAllocationPolicyAdminDto
{
    public long      Id                   { get; init; }
    public string    CurrencyCode         { get; init; } = "TRY";
    public decimal   NegativeBalanceLimit { get; init; }
    public DateTime  EffectiveFrom        { get; init; }
    public DateTime? EffectiveTo          { get; init; }
    public int       Status               { get; init; }
    public string?   PolicyCode           { get; init; }
    public string?   PolicyName           { get; init; }
    public string?   Notes                { get; init; }
    public bool      IsActive             { get; init; }
    public List<RefundAllocationPolicyRuleDto> Rules { get; init; } = new();
}

public sealed record RefundAllocationPolicyMutateResultDto(long Id, string? PolicyCode);
