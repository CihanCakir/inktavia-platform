using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class AddJobWorkLogBffCommand : AizenCommand<AddServiceRequestWorkLogResponse>
{
    public long AssignmentId { get; init; }
    public AddServiceRequestWorkLogRequest Body { get; init; } = default!;
}
