using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

/// <summary>
/// BE-MO2 — the provider offers RECEIVED on the owner's service request, cost-free. Carries the existing
/// customer-facing offer DTO (totals + line items + S3 FX) only — never provider cost / commission / funding.
/// Other providers' Drafts are excluded upstream.
/// </summary>
[DocumentationInfo("Owner offers-received response", "Provider offers on the owner's service request (cost-free).")]
public sealed class GetServiceRequestOffersForOwnerResponse
{
    public List<ServiceRequestOfferDto> Offers { get; init; } = new();
}
