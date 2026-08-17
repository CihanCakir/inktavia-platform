namespace Aizen.Modules.Payment.Abstraction.Dto;

/// <summary>BE-P11 admin — a premium product row (EntitlementType/Status as enum-int, mirroring other admin DTOs).</summary>
public sealed class PremiumProductAdminDto
{
    public long    Id              { get; init; }
    public string  Code            { get; init; } = default!;
    public string  Name            { get; init; } = default!;
    public int     EntitlementType { get; init; }
    public int     DurationDays    { get; init; }
    public int     Status          { get; init; }
    public string? Description     { get; init; }
    public bool    IsActive        { get; init; }
}

/// <summary>BE-P11 admin — a versioned premium price row.</summary>
public sealed class PremiumProductPriceAdminDto
{
    public long      Id               { get; init; }
    public long      PremiumProductId { get; init; }
    public decimal   PriceAmount      { get; init; }
    public string    CurrencyCode     { get; init; } = "TRY";
    public DateTime  EffectiveFrom    { get; init; }
    public DateTime? EffectiveTo      { get; init; }
    public int       Status           { get; init; }
    public string?   PriceCode        { get; init; }
    public string?   Notes            { get; init; }
    public bool      IsActive         { get; init; }
}

/// <summary>BE-P11 admin — mutation result (Id + generated/echoed code).</summary>
public sealed record PremiumMutateResultDto(long Id, string? Code);
