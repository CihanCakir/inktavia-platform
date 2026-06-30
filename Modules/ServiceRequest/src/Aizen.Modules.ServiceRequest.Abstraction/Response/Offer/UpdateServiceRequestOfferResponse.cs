using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

[DocumentationInfo("Update offer response", "Response after updating a provider offer.")]
public sealed class UpdateServiceRequestOfferResponse(ServiceRequestOfferDto offer)
{
    public ServiceRequestOfferDto Offer { get; } = offer;
}
