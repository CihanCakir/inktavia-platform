using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

[DocumentationInfo("Update offer response", "Response after updating a provider offer.")]
public sealed class UpdateServiceRequestOfferResponse(ServiceRequestOfferDto offer)
{
    public ServiceRequestOfferDto Offer { get; } = offer;
}
