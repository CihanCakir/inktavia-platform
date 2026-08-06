using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySellThroughSettlementDetail;

public sealed class GetCargoDrySellThroughSettlementDetailBffQuery
    : AizenQuery<GetCargoDrySellThroughSettlementDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetCargoDrySellThroughSettlementDetailBffResponse
{
    public CargoDrySellThroughSettlementBffDto? Detail { get; init; }
}
