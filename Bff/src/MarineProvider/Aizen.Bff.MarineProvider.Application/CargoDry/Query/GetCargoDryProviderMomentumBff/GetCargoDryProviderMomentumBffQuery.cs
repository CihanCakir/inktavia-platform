using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDryProviderMomentumBffQuery : AizenQuery<CargoDryProviderMomentumDto>
{
}
