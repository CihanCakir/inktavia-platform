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
