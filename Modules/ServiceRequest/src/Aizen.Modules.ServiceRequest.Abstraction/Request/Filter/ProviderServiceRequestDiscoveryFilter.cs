using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;

public sealed class ProviderServiceRequestDiscoveryFilter
{
    // NO ProviderProfileId field — comes from assertion
    public int PageSize { get; set; } = 20;
    public string? Cursor { get; set; }
    public string SortBy { get; set; } = "PublishedAtDesc"; // PublishedAtDesc | PriorityDesc | DistanceAsc
    public string? LocationCityCode { get; set; }
    public string? LocationCountryCode { get; set; }
    public string? ServiceCategoryCode { get; set; }
    public ServiceRequestPriority? MinPriority { get; set; }
    public string? SearchTerm { get; set; }
    public OfferStateFilter? OfferState { get; set; } // null = Any

    // "New only" — server-side predicate on the indexed PublishedAt column. The client sends the timestamp it
    // last looked at the list; the server returns requests published after it. Keeping this server-side (rather
    // than filtering a page in the browser) is what keeps pagination correct — a client filter would drop rows
    // from a page and leave the cursor pointing at the wrong place. Same semantics as the NEW badge, so the
    // filter and the badge always agree.
    public DateTime? PublishedAfterUtc { get; set; }

    // Geo fields — see doc 09b
    public decimal? CenterLatitude { get; set; }
    public decimal? CenterLongitude { get; set; }
    public decimal? RadiusKm { get; set; }
    public decimal? BoundsMinLat { get; set; }
    public decimal? BoundsMaxLat { get; set; }
    public decimal? BoundsMinLng { get; set; }
    public decimal? BoundsMaxLng { get; set; }
}

public enum OfferStateFilter
{
    Any = 0,
    NotOffered = 1,
    Offered = 2,
}
