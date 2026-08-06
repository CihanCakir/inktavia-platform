using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetParticipantPlanById;

public sealed class GetParticipantPlanByIdBffQuery : AizenQuery<ParticipantPlanBffDto?>
{
    public long Id { get; init; }
}
