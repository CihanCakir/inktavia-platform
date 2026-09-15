namespace Aizen.Bff.MarineProvider.Application.CargoDry.Dto;

/// <summary>
/// Provider-facing CargoDry catalog item — the module product projection with file ids resolved to presigned
/// FileStorage read URLs (thumbnailUrl + imageUrls[]). Keeps the commercial fields providers need; drops raw file ids.
/// </summary>
public sealed class CargoDryProviderCatalogItemBffDto
{
    public long     Id                     { get; init; }
    public string   ProductCode            { get; init; } = default!;
    public string   Name                   { get; init; } = default!;
    public string?  Description            { get; init; }
    public int      ValidityDays           { get; init; }
    public bool     HasSmartDevice         { get; init; }
    public string?  DeviceType             { get; init; }
    public decimal  RetailPrice            { get; init; }
    public string?  CurrencyCode           { get; init; }
    public bool     IsActive               { get; init; }
    public decimal? WholesalePrice         { get; init; }
    public decimal? ConsignmentPrice       { get; init; }
    public decimal? ProviderCommissionRate { get; init; }

    // Presigned media (BFF-resolved; null/absent when a file id has no backing object — FE renders a placeholder).
    public string?      ThumbnailUrl { get; init; }
    public List<string> ImageUrls    { get; init; } = new();
}

/// <summary>Provider stock-request picker option — code + name + optional thumbnail URL.</summary>
public sealed class CargoDryProviderProductOptionBffDto
{
    public string  ProductCode  { get; init; } = default!;
    public string? ProductName  { get; init; }
    public string? ThumbnailUrl { get; init; }
}

/// <summary>
/// Presigned media block for the CARGODRY_SUPPLY product referenced by a provider SR detail. Attached only for
/// supply requests (null otherwise).
/// </summary>
public sealed class CargoDryProviderSrProductBlockBffDto
{
    public string        ProductCode  { get; init; } = default!;
    public string?       ProductName  { get; init; }
    public decimal?      RetailPrice  { get; init; }
    public string?       CurrencyCode { get; init; }
    public string?       ThumbnailUrl { get; init; }
    public List<string>  ImageUrls    { get; init; } = new();
}
