using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryBatchList;

public sealed class GetCargoDryBatchListQuery : AizenQuery<CargoDryBatchListDto>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
