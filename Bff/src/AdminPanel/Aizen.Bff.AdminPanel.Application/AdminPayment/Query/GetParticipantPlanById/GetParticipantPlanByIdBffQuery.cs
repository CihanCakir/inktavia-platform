using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetParticipantPlanById;

public sealed class GetParticipantPlanByIdBffQuery : AizenQuery<ParticipantPlanBffDto?>
{
    public long Id { get; init; }
}
