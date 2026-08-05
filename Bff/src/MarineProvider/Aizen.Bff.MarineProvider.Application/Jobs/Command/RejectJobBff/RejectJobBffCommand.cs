using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Assignment;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

/// <summary>N-E — provider rejects an assigned job with a structured reason (+ optional note).</summary>
public sealed class RejectJobBffCommand : AizenCommand<JobSuccessResult>
{
    public long AssignmentId { get; init; }
    public RejectServiceRequestAssignmentRequest Body { get; init; } = default!;
}
