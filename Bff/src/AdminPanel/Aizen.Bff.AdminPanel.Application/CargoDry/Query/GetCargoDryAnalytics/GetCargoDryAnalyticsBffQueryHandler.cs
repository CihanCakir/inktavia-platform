using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryAnalytics;

[DocumentationInfo("Get CargoDry analytics BFF query handler", "Calls the CargoDry analytics endpoint and enriches status slices with HEX palette colours for Recharts.")]
public sealed class GetCargoDryAnalyticsBffQueryHandler
    : AizenQueryHandler<GetCargoDryAnalyticsBffQuery, GetCargoDryAnalyticsBffResponse>
{
    private static readonly Dictionary<string, string> StatusColors = new()
    {
        ["Available"]   = "#b9c7e4",
        ["Activated"]   = "#4ade80",
        ["Expired"]     = "#ef4444",
        ["Renewed"]     = "#e9c349",
        ["Revoked"]     = "#f97316",
        ["Lost"]        = "#a855f7",
        ["Transferred"] = "#06b6d4",
    };

    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryAnalyticsBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryAnalyticsBffResponse> Handle(
        GetCargoDryAnalyticsBffQuery request, CancellationToken ct)
    {
        var raw = await _remote.GetAnalyticsAsync(ct);

        var dto = new CargoDryAnalyticsBffDto
        {
            TotalKits          = raw.TotalKits,
            ActiveKits         = raw.ActiveKits,
            AvgEfficiencyPct   = raw.AvgEfficiencyPct,
            RenewalRatePct     = raw.RenewalRatePct,
            ExpiringNext30Days = raw.ExpiringNext30Days,
            ComputedAt         = raw.ComputedAt,
            StatusDistribution = raw.StatusDistribution
                .Select(kv => new CargoDryStatusSliceDto
                {
                    Name  = kv.Key,
                    Value = kv.Value,
                    Color = StatusColors.GetValueOrDefault(kv.Key, "#64748b"),
                })
                .ToList(),
            DailyActivations = raw.DailyActivations
                .Select(d => new CargoDryDailyActivationBffDto
                {
                    Date        = d.Date,
                    Activations = d.Activations,
                    Renewals    = d.Renewals,
                })
                .ToList(),
            EfficiencyBuckets = raw.EfficiencyBuckets
                .Select(kv => new CargoDryEfficiencyBucketDto
                {
                    Bucket = kv.Key,
                    Count  = kv.Value,
                })
                .ToList(),
            ProductMix = raw.ProductMix
                .Select(p => new CargoDryProductMixBffDto
                {
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    ActiveKits  = p.ActiveKits,
                    TotalKits   = p.TotalKits,
                })
                .ToList(),
        };

        return new GetCargoDryAnalyticsBffResponse { Analytics = dto };
    }
}
