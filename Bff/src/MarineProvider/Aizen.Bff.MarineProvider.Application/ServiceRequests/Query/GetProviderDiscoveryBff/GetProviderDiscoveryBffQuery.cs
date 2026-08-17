using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class GetProviderDiscoveryBffQuery : AizenQuery<GetProviderDiscoveryBffResponse>
{
    public int PageSize { get; init; } = 20;
    public string? Cursor { get; init; }
    public string? SortBy { get; init; }
    public string? LocationCityCode { get; init; }
    public string? LocationCountryCode { get; init; }
    public string? ServiceCategoryCode { get; init; }
    public ServiceRequestPriority? MinPriority { get; init; }
    public string? SearchTerm { get; init; }
    /// <summary>
    /// Raw enum name from the client ("NotOffered", "Offered"). The handler maps to the int code.
    /// </summary>
    public string? OfferState { get; init; }
    /// <summary>"New only": the client's last-seen timestamp; the module returns requests published after it.</summary>
    public DateTime? PublishedAfterUtc { get; init; }
    public decimal? CenterLatitude { get; init; }
    public decimal? CenterLongitude { get; init; }
    public decimal? RadiusKm { get; init; }
    public decimal? BoundsMinLat { get; init; }
    public decimal? BoundsMaxLat { get; init; }
    public decimal? BoundsMinLng { get; init; }
    public decimal? BoundsMaxLng { get; init; }
}
