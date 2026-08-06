using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryStatsComparison;

[DocumentationInfo("Get CargoDry stats comparison BFF query handler",
    "Fetches current vs prior 30-day period KPI deltas for dashboard change badges.")]
public sealed class GetCargoDryStatsComparisonBffQueryHandler
    : AizenQueryHandler<GetCargoDryStatsComparisonBffQuery, GetCargoDryStatsComparisonBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryStatsComparisonBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryStatsComparisonBffResponse> Handle(
        GetCargoDryStatsComparisonBffQuery request, CancellationToken ct)
    {
        var upstream = await _remote.GetStatsComparisonAsync(ct);

        // Re-map to ensure BFF DTO is decoupled from module DTO shape
        var dto = new CargoDryStatsComparisonBffDto
        {
            ActiveKitsChangePercent   = upstream.ActiveKitsChangePercent,
            TotalKitsChangePercent    = upstream.TotalKitsChangePercent,
            TodayActivationsChangePct = upstream.TodayActivationsChangePct,
            RenewalRateDelta          = upstream.RenewalRateDelta,
            CurrentPeriodStart        = upstream.CurrentPeriodStart,
            PriorPeriodStart          = upstream.PriorPeriodStart,
        };

        return new GetCargoDryStatsComparisonBffResponse { Comparison = dto };
    }
}
