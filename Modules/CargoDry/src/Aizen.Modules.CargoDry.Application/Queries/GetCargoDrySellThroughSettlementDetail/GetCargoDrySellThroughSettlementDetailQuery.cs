using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySellThroughSettlementDetail;

public sealed class GetCargoDrySellThroughSettlementDetailQuery
    : AizenQuery<GetCargoDrySellThroughSettlementDetailResponse>
{
    public long Id { get; init; }
}

public sealed class GetCargoDrySellThroughSettlementDetailResponse
{
    public CargoDrySellThroughSettlementDto? Detail { get; init; }
}
