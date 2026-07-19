using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetDiscoverySummaryBffQuery : AizenQuery<ProviderDiscoverySummaryResponse>
{
    public string? LocationCityCode { get; init; }
    public string? LocationCountryCode { get; init; }
    public string? ServiceCategoryCode { get; init; }
    public string? SearchTerm { get; init; }
    public decimal? CenterLatitude { get; init; }
    public decimal? CenterLongitude { get; init; }
    public decimal? RadiusKm { get; init; }
    public decimal? BoundsMinLat { get; init; }
    public decimal? BoundsMaxLat { get; init; }
    public decimal? BoundsMinLng { get; init; }
    public decimal? BoundsMaxLng { get; init; }
}
