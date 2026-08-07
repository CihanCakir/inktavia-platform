using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetServiceRequestOffersForOwner;

/// <summary>
/// BE-MO2 — the offers received on the owner's SR. Owner-scoped: the handler verifies the caller owns the SR
/// (OwnerUserId from the trusted context), never from parameters.
/// </summary>
[DocumentationInfo("Get offers for owner query", "Provider offers received on the caller-owner's service request.")]
public sealed class GetServiceRequestOffersForOwnerQuery : AizenQuery<GetServiceRequestOffersForOwnerResponse>
{
    public long ServiceRequestId { get; }

    public GetServiceRequestOffersForOwnerQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
