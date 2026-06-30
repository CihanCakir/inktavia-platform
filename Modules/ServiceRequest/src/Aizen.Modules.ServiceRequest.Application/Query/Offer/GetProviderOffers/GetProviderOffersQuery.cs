using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Query.Offer;

[DocumentationInfo("Get provider offers query", "Returns all provider offers for a service request.")]
public sealed class GetProviderOffersQuery : AizenQuery<GetProviderOffersResponse>
{
    public long ServiceRequestId { get; }
    public GetProviderOffersQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
