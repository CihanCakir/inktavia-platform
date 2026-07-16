using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

public sealed class SaveOfferDraftResponse
{
    public ServiceRequestOfferDto Offer { get; init; } = default!;
    public string ConcurrencyToken { get; init; } = default!;

    public SaveOfferDraftResponse() { }
    public SaveOfferDraftResponse(ServiceRequestOfferDto offer, string concurrencyToken)
    {
        Offer = offer;
        ConcurrencyToken = concurrencyToken;
    }
}
