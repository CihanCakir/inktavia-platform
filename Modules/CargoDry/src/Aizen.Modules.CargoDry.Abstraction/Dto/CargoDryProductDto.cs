namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public sealed class CargoDryProductDto
{
    public long    Id             { get; init; }
    public string  ProductCode    { get; init; } = default!;
    public string  Name           { get; init; } = default!;
    public string  Description    { get; init; } = default!;
    public int     ValidityDays   { get; init; }
    public bool    HasSmartDevice { get; init; }
    public string? DeviceType     { get; init; }
    public decimal RetailPrice    { get; init; }
    public string  CurrencyCode   { get; init; } = default!;
    public bool    IsActive       { get; init; }
    public string? CreatedAt      { get; init; }

    /// <summary>
    /// Operational kit statistics for this product.
    /// Populated only by the product detail endpoint — null in list responses.
    /// </summary>
    public CargoDryProductKitStatsDto? KitStats { get; init; }
}

/// <summary>
/// Live kit statistics for a single product, computed from the kit repository.
/// Displayed in the Product Specs drawer on the admin products page.
/// </summary>
public sealed class CargoDryProductKitStatsDto
{
    public int    TotalKitsIssued  { get; init; }
    public int    ActiveKits       { get; init; }
    public int    ExpiredKits      { get; init; }
    public int    RevokedKits      { get; init; }
    public int    RenewedKits      { get; init; }
    public double AvgEfficiencyPct { get; init; }
    public double RenewalRatePct   { get; init; }
    public int    ExpiringIn30Days { get; init; }
}
