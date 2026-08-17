using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderCommissionTrend;

public sealed class GetCargoDryProviderCommissionTrendQuery : AizenQuery<List<CargoDryEarningsTrendPointDto>>
{
    public long ProviderProfileId { get; init; }
    public int  Months            { get; init; } = 6;
}
