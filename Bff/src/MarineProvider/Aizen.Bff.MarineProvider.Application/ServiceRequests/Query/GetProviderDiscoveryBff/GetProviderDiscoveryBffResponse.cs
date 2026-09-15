namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

/// <summary>
/// BFF-enriched discovery response. Wraps the module's paged discovery result
/// and decorates each item with vessel summary data fetched in a single bulk call.
/// </summary>
public sealed class GetProviderDiscoveryBffResponse
{
    public List<ProviderDiscoveryBffItemDto> Items { get; init; } = new();
    public string? NextCursor { get; init; }
    public int PageSize { get; init; }
    public string? LocationMode { get; init; }
}

public sealed record ProviderDiscoveryBffItemDto
{
    // Service request fields
    public long Id { get; init; }
    public string RequestCode { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string? ServiceCategoryCode { get; init; }
    public string? ServiceTypeCode { get; init; }
    // CargoDry supply flow
    /// <summary>Non-null only for CARGODRY_SUPPLY items — the requested product code.</summary>
    public string? CargoDryProductCode { get; init; }
    /// <summary>Whether the calling provider may accept this request (true for non-CargoDry items; gated for CARGODRY_SUPPLY).</summary>
    public bool CanAccept { get; init; } = true;
    /// <summary>Reason the provider cannot accept (null when CanAccept). For a locked CARGODRY_SUPPLY card. Distinguishes not-in-program vs stokta yok.</summary>
    public string? CanAcceptReason { get; init; }
    /// <summary>A1 — the provider's available consignment-kit count for this product (null for non-CARGODRY_SUPPLY items).</summary>
    public int? AvailableKitCount { get; init; }
    /// <summary>Whether the owner has marked the calling provider as their preferred CargoDry supplier.</summary>
    public bool IsPreferred { get; init; }
    public string? LocationCityCode { get; init; }
    public string? LocationCountryCode { get; init; }
    public string? LocationMarinaName { get; init; }
    public decimal? SnappedLatitude { get; init; }
    public decimal? SnappedLongitude { get; init; }
    public decimal? DistanceKm { get; init; }
    public DateTime? RequestedStartDate { get; init; }
    public DateTime? RequestedEndDate { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int OfferCount { get; init; }
    public int AttachmentCount { get; init; }
    public bool HasProviderOffer { get; init; }
    public long? ProviderOfferId { get; init; }
    public string? ProviderOfferStatus { get; init; }
    public decimal? ProviderOfferTotalAmount { get; init; }
    public bool IsUpdated { get; init; }

    // Vessel fields (from module + enriched from vessel summary)
    public long VesselId { get; init; }
    public string? VesselName { get; init; }
    public string? VesselTypeCode { get; init; }
    public string? VesselBrand { get; init; }
    public string? VesselModel { get; init; }
    public decimal? VesselLengthValue { get; init; }
    public string? VesselLengthUnitCode { get; init; }
}
