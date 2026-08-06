using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryBatchList;

public sealed class GetCargoDryBatchListBffQuery : AizenQuery<GetCargoDryBatchListBffResponse>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class GetCargoDryBatchListBffResponse
{
    public CargoDryBatchListBffDto BatchList { get; init; } = default!;
}
