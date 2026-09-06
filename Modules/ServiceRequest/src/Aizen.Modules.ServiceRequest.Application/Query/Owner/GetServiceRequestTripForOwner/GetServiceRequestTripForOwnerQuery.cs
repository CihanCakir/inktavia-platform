using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner;

[DocumentationInfo("Get service request trip for owner query", "Owner-scoped current live trip for an SR (null when none).")]
public sealed class GetServiceRequestTripForOwnerQuery : AizenQuery<GetServiceRequestTripForOwnerResponse>
{
    public GetServiceRequestTripForOwnerQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
    public long ServiceRequestId { get; }
}
