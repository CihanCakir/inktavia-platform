using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionDetail;

public sealed class GetCargoDrySalesAttributionDetailBffQuery
    : AizenQuery<GetCargoDrySalesAttributionDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetCargoDrySalesAttributionDetailBffResponse
{
    public CargoDrySalesAttributionBffDto? Detail { get; init; }
}
