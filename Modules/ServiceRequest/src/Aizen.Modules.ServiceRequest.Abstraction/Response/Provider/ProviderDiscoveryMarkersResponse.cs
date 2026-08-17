namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

public sealed class ProviderDiscoveryMarkersResponse
{
    public List<DiscoveryMarkerDto> Markers { get; init; } = new();
    public bool Truncated { get; init; }
}

public sealed record DiscoveryMarkerDto
{
    public long Id { get; init; }
    public decimal? ApproxLatitude { get; init; }
    public decimal? ApproxLongitude { get; init; }
    public string Priority { get; init; } = string.Empty;
    public bool IsNew { get; init; }
    public bool HasProviderOffer { get; init; }
    public string? ProviderOfferStatus { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? LocationMarinaName { get; init; }
}
