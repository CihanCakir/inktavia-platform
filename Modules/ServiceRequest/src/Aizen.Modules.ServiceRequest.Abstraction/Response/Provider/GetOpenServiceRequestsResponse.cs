namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

[DocumentationInfo("Get open service requests response", "Paged list of biddable service requests for a provider.")]
public sealed class GetOpenServiceRequestsResponse
{
    public List<OpenServiceRequestItemDto> Items { get; init; } = new();
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed record OpenServiceRequestItemDto
{
    public long Id { get; init; }
    public string RequestCode { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? ServiceCategoryCode { get; init; }
    public string? LocationCityCode { get; init; }
    public string? LocationCountryCode { get; init; }
    public string? LocationMarinaName { get; init; }
    public string Priority { get; init; } = string.Empty;
    public DateTime? RequestedStartDate { get; init; }
    public DateTime? RequestedEndDate { get; init; }
    public int AttachmentCount { get; init; }
    public int OfferCount { get; init; }
    public DateTime? CreatedAt { get; init; }
}
