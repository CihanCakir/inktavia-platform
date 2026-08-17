using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;

[DocumentationInfo("Get service request detail query", "Retrieves full detail of a service request including offers, assignment, completion and dispute.")]
public sealed class GetServiceRequestDetailQuery : AizenQuery<GetServiceRequestDetailResponse>
{
    public long ServiceRequestId { get; }
    public GetServiceRequestDetailQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
