using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderParticipation;

public sealed class GetCargoDryProviderParticipationQuery : AizenQuery<CargoDryProviderParticipationDto>
{
    public long ProviderProfileId { get; init; }
}
