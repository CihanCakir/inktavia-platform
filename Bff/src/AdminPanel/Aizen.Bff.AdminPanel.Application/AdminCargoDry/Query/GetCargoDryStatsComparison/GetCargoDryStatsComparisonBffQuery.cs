using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryStatsComparison;

public sealed class GetCargoDryStatsComparisonBffQuery : AizenQuery<GetCargoDryStatsComparisonBffResponse>
{
}

public sealed class GetCargoDryStatsComparisonBffResponse
{
    public CargoDryStatsComparisonBffDto Comparison { get; init; } = default!;
}
