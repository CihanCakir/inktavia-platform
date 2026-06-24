using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Modules.ServiceRequest.Application.Query.WorkLog;

[DocumentationInfo("Get work logs query", "Returns work phases, log entries, and job health for a service request.")]
public sealed class GetWorkLogsQuery : AizenQuery<GetWorkLogsResponse>
{
    public long ServiceRequestId { get; }
    public GetWorkLogsQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}
