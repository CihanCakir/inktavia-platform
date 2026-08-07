using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetServiceRequestOfferForOwner;

/// <summary>BE-MO2 — one received offer (full cost-free breakdown) for the caller-owner's SR. Owner-scoped.</summary>
[DocumentationInfo("Get one offer for owner query", "One provider offer on the caller-owner's SR with its item breakdown.")]
public sealed class GetServiceRequestOfferForOwnerQuery : AizenQuery<GetServiceRequestOfferForOwnerResponse>
{
    public long ServiceRequestId { get; }
    public long OfferId { get; }

    public GetServiceRequestOfferForOwnerQuery(long serviceRequestId, long offerId)
    {
        ServiceRequestId = serviceRequestId;
        OfferId = offerId;
    }
}
