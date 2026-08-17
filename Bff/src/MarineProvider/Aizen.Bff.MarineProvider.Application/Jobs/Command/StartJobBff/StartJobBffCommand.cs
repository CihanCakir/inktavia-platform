using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Jobs;

public sealed class StartJobBffCommand : AizenCommand<JobSuccessResult>
{
    public long AssignmentId { get; init; }
}
