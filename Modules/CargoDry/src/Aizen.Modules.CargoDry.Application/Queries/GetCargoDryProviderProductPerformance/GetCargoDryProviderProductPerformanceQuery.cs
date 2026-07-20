using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderProductPerformance;

public sealed class GetCargoDryProviderProductPerformanceQuery : AizenQuery<List<CargoDryProductPerformanceDto>>
{
    public long ProviderProfileId { get; init; }
}
