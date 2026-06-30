
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

[DocumentationInfo("Get provider offers response", "Provider offers and agreement timeline for a service request.")]
public sealed class GetProviderOffersResponse(
    string serviceRequestId,
    string serviceRequestTitle,
    List<ProviderOfferItemDto> offers,
    List<AgreementEventDto> agreementTimeline)
{
    public string ServiceRequestId { get; } = serviceRequestId;
    public string ServiceRequestTitle { get; } = serviceRequestTitle;
    public List<ProviderOfferItemDto> Offers { get; } = offers;
    public List<AgreementEventDto> AgreementTimeline { get; } = agreementTimeline;
    public int TotalOffers => Offers.Count;
}

public sealed record ProviderOfferItemDto
{
    public string Id { get; init; } = string.Empty;
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public double Rating { get; init; }
    public int ReviewCount { get; init; }
    public decimal QuoteAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string EstimatedDuration { get; init; } = string.Empty;
    public string? ProposalUrl { get; init; }
    public string Status { get; init; } = "Pending";
    public DateTimeOffset SubmittedAt { get; init; }
}

public sealed record AgreementEventDto
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? Actor { get; init; }
    public string Icon { get; init; } = "event";
}
