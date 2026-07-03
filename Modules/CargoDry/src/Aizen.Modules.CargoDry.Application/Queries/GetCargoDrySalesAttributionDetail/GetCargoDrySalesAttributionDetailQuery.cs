using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionDetail;

public sealed class GetCargoDrySalesAttributionDetailQuery
    : AizenQuery<GetCargoDrySalesAttributionDetailResponse>
{
    public long Id { get; init; }
}

public sealed class GetCargoDrySalesAttributionDetailResponse
{
    public CargoDrySalesAttributionDto? Detail { get; init; }
}
