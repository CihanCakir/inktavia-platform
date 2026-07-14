namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

[DocumentationInfo("Get my offers response", "Paged list of the provider's own offers across all service requests.")]
public sealed class GetMyOffersResponse
{
    public List<MyOfferItemDto> Items { get; init; } = new();
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public int TotalReturned => Items.Count;
}

public sealed record MyOfferItemDto
{
    public long OfferId { get; init; }
    public long ServiceRequestId { get; init; }
    public string ServiceRequestTitle { get; init; } = string.Empty;
    public string ServiceRequestCode { get; init; } = string.Empty;
    public string OfferStatus { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string? CurrencyCode { get; init; }
    public DateTime? EstimatedStartDate { get; init; }
    public DateTime? EstimatedEndDate { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? WithdrawnAt { get; init; }
}
