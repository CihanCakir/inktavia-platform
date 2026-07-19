using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderStockRequests;

public sealed class GetProviderStockRequestsQuery : AizenQuery<CargoDryStockRequestPagedResultDto>
{
    public long ProviderProfileId { get; init; }
    public CargoDryStockRequestStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
