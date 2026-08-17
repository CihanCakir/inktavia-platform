using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>Full detail DTO for a single provider inventory row (one batch per provider/product).</summary>
public sealed class CargoDryProviderInventoryDto
{
    public long    Id                { get; init; }
    public long    ProviderProfileId { get; init; }
    public string  ProductCode       { get; init; } = default!;
    public string? BatchCode         { get; init; }

    public CargoDryCommercialModel CommercialModel   { get; init; }
    public string                  CommercialModelName { get; init; } = default!;
    public SalesChannel            SalesChannel      { get; init; }
    public string                  SalesChannelName  { get; init; } = default!;
    public StockLocationType       StockLocationType { get; init; }
    public string                  StockLocationName { get; init; } = default!;

    public int TotalAllocated  { get; init; }
    public int TotalActivated  { get; init; }
    public int TotalRevoked    { get; init; }
    public int TotalReturned   { get; init; }
    public int TotalAdjusted   { get; init; }
    public int AvailableStock  { get; init; }

    public DateTime? LastMovementAtUtc { get; init; }
    public DateTime  CreatedAtUtc      { get; init; }
    public DateTime? UpdatedAtUtc      { get; init; }
}

/// <summary>Lightweight list-item DTO for paged provider inventory list.</summary>
public sealed class CargoDryProviderInventoryListItemDto
{
    public long    Id                { get; init; }
    public long    ProviderProfileId { get; init; }
    public string  ProductCode       { get; init; } = default!;
    public string? BatchCode         { get; init; }

    public CargoDryCommercialModel CommercialModel   { get; init; }
    public string                  CommercialModelName { get; init; } = default!;
    public SalesChannel            SalesChannel      { get; init; }
    public string                  SalesChannelName  { get; init; } = default!;

    public int TotalAllocated  { get; init; }
    public int TotalActivated  { get; init; }
    public int AvailableStock  { get; init; }

    public decimal EarnedCommission    { get; init; }
    public decimal PotentialCommission { get; init; }
    public decimal SellThroughPct      { get; init; }
    public string  CurrencyCode        { get; init; } = "TRY";

    public DateTime? LastMovementAtUtc { get; init; }
    public DateTime  CreatedAtUtc      { get; init; }
}

/// <summary>Paged result wrapper for provider inventory list queries.</summary>
public sealed class CargoDryProviderInventoryPagedResultDto
{
    public List<CargoDryProviderInventoryListItemDto> Items    { get; init; } = [];
    public int                                        Total    { get; init; }
    public int                                        Page     { get; init; }
    public int                                        PageSize { get; init; }
}

/// <summary>
/// Aggregate detail view for a single provider across all their inventory rows.
/// Used by GetProviderInventoryDetailQuery.
/// </summary>
public sealed class CargoDryProviderInventoryDetailDto
{
    public long ProviderProfileId { get; init; }

    public List<CargoDryProviderInventoryDto> InventoryRows { get; init; } = [];

    public int TotalAllocated  { get; init; }
    public int TotalActivated  { get; init; }
    public int TotalRevoked    { get; init; }
    public int TotalReturned   { get; init; }
    public int TotalAdjusted   { get; init; }
    public int TotalAvailable  { get; init; }

    public DateTime? LastMovementAtUtc { get; init; }
}
