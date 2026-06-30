using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetParticipantPlans;

public sealed class GetParticipantPlansBffQuery : AizenQuery<GetParticipantPlansBffResponse>
{
}

public sealed class GetParticipantPlansBffResponse
{
    public List<ParticipantPlanBffDto> Plans { get; init; } = [];
}
