using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderEarnings;

public sealed class GetCargoDryProviderEarningsQuery : AizenQuery<CargoDryProviderEarningsDto>
{
    public long ProviderProfileId { get; init; }
}
