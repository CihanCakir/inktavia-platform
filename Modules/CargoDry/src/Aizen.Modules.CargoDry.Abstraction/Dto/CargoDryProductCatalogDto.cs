namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Owner-safe CargoDry product catalog projection (CargoDry supply flow). Deliberately carries NO commercial fields
/// (wholesale / consignment / commission / provider earning) — it is exposed to boat owners via the mobile BFF.
/// Media are FileStorage file ids only; the presigned display URLs are resolved at the BFF boundary.
/// </summary>
public sealed class CargoDryProductCatalogDto
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

    /// <summary>FileStorage file id of the thumbnail (null = none). Resolved to a read URL at the BFF.</summary>
    public Guid?        ThumbnailFileId { get; init; }
    /// <summary>Ordered gallery image file ids (by display order). Resolved to read URLs at the BFF.</summary>
    public List<Guid>   ImageFileIds    { get; init; } = new();
}
