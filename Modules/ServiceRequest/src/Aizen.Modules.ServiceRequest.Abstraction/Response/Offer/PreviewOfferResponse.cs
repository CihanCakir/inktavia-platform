using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

public sealed class PreviewOfferResponse
{
    public ServiceRequestOfferDto Offer { get; init; } = default!;

    public PreviewOfferResponse() { }
    public PreviewOfferResponse(ServiceRequestOfferDto offer) => Offer = offer;
}
