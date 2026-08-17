using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class GetJobWorkLogsBffQuery : AizenQuery<GetServiceRequestWorkLogsResponse>
{
    public long AssignmentId { get; init; }
}
