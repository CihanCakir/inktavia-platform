using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Completion;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class CompleteJobBffCommand : AizenCommand<JobSuccessResult>
{
    public long AssignmentId { get; init; }
    public SubmitServiceRequestCompletionRequest Body { get; init; } = default!;
}
