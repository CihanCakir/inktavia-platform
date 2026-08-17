using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;

namespace Aizen.Modules.Payment.Application.Queries.GetParticipantPlanById;

public sealed class GetParticipantPlanByIdQuery : AizenQuery<ParticipantPlanDto>
{
    public long Id { get; init; }
}
