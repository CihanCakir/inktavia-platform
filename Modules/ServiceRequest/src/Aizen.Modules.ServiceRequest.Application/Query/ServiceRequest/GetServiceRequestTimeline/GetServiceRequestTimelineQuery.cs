using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;

[DocumentationInfo("Get service request timeline query", "Returns ordered timeline events for a service request.")]
public sealed class GetServiceRequestTimelineQuery : AizenQuery<GetServiceRequestTimelineResponse>
{
    public long ServiceRequestId { get; }
    public GetServiceRequestTimelineQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
