using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

public sealed class GetCurrentParticipantProfileDetailQuery : AizenQuery<ParticipantProfileDetailDto>
{
}
