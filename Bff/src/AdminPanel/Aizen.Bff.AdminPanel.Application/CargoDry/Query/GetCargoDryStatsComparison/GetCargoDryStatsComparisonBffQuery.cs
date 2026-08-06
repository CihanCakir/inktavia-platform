using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryStatsComparison;

public sealed class GetCargoDryStatsComparisonBffQuery : AizenQuery<GetCargoDryStatsComparisonBffResponse>
{
}

public sealed class GetCargoDryStatsComparisonBffResponse
{
    public CargoDryStatsComparisonBffDto Comparison { get; init; } = default!;
}
