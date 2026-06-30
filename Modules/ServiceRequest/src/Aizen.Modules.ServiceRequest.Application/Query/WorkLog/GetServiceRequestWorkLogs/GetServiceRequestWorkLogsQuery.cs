using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Modules.ServiceRequest.Application.Query.WorkLog;

[DocumentationInfo("Get work logs query", "Returns all work log entries for an assignment.")]
public sealed class GetServiceRequestWorkLogsQuery : AizenQuery<GetServiceRequestWorkLogsResponse>
{
    public long AssignmentId { get; }
    public GetServiceRequestWorkLogsQuery(long assignmentId) => AssignmentId = assignmentId;
}
