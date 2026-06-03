using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

[DocumentationInfo("Create offer response", "Response after a provider creates an offer.")]
public sealed class CreateServiceRequestOfferResponse(ServiceRequestOfferDto offer)
{
    public ServiceRequestOfferDto Offer { get; } = offer;
}
