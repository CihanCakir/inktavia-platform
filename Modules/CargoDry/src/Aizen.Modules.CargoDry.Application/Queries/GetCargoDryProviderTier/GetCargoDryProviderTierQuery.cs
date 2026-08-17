using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderTier;

public sealed class GetCargoDryProviderTierQuery : AizenQuery<CargoDryProviderTierDto>
{
    public long ProviderProfileId { get; init; }
}
