using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderMomentum;

public sealed class GetCargoDryProviderMomentumQuery : AizenQuery<CargoDryProviderMomentumDto>
{
    public long ProviderProfileId { get; init; }
    public int  LookbackMonths    { get; init; } = 24;
}
