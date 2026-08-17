using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryDetail;

public sealed class GetProviderInventoryDetailQuery : AizenQuery<CargoDryProviderInventoryDetailDto>
{
    public long ProviderProfileId { get; init; }
}
