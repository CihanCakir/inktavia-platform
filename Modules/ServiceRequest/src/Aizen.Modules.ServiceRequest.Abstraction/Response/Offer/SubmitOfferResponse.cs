using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

public sealed class SubmitOfferResponse
{
    public ServiceRequestOfferDto Offer { get; init; } = default!;

    public SubmitOfferResponse() { }
    public SubmitOfferResponse(ServiceRequestOfferDto offer) => Offer = offer;
}
