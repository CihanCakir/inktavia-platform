using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

/// <summary>BE-MO2 — one received offer with its full cost-free item breakdown (for the owner offer-detail screen).</summary>
[DocumentationInfo("Owner offer-detail response", "One provider offer on the owner's SR with its cost-free line items.")]
public sealed class GetServiceRequestOfferForOwnerResponse
{
    public ServiceRequestOfferDto? Offer { get; init; }
}
