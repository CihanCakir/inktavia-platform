namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

public sealed class ProviderDiscoveryResponse
{
    public List<ProviderDiscoveryItemDto> Items { get; init; } = new();
    public string? NextCursor { get; init; }
    public int PageSize { get; init; }
    /// <summary>
    /// "Geo" when centre/bounds filtering is active, "City" otherwise.
    /// </summary>
    public string? LocationMode { get; set; }
}

public sealed record ProviderDiscoveryItemDto
{
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
    // Vessel
    public long VesselId { get; init; }
    public string? VesselName { get; init; }
    // Counts (subqueries, no Include)
    public int OfferCount { get; init; }
    public int AttachmentCount { get; init; }
    // Caller's own offer state
    public bool HasProviderOffer { get; init; }
    public long? ProviderOfferId { get; init; }
    public string? ProviderOfferStatus { get; init; }
    public decimal? ProviderOfferTotalAmount { get; init; }
    // Approximate: ModifyDate moves on any audit write, not only owner content edits.
    // A precise ContentUpdatedAt (set solely by the owner-edit command) is post-MVP.
    public bool IsUpdated { get; init; }
}
