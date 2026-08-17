using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySalesAttributionDetail;

public sealed class GetCargoDrySalesAttributionDetailBffQuery
    : AizenQuery<GetCargoDrySalesAttributionDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetCargoDrySalesAttributionDetailBffResponse
{
    public CargoDrySalesAttributionBffDto? Detail { get; init; }
}
