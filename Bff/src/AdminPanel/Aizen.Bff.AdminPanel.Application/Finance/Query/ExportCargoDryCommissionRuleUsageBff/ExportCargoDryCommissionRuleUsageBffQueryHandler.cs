using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Finance.Query.ExportCargoDryCommissionRuleUsageBff;

[DocumentationInfo("Export CargoDry commission rule usage CSV BFF query handler",
    "Proxies to the CargoDry finance commission rule usage export endpoint and streams the full CSV. " +
    "All filters are forwarded; no pagination is applied. Phase 16G (July 2026).")]
public sealed class ExportCargoDryCommissionRuleUsageBffQueryHandler
    : AizenQueryHandler<ExportCargoDryCommissionRuleUsageBffQuery, ExportCargoDryCommissionRuleUsageBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public ExportCargoDryCommissionRuleUsageBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<ExportCargoDryCommissionRuleUsageBffResponse> Handle(
        ExportCargoDryCommissionRuleUsageBffQuery request, CancellationToken ct)
    {
        var upstream    = await _remote.ExportCommissionRuleUsageAsync(
            request.DateFrom,
            request.DateTo,
            request.RuleId,
            request.ProductCode,
            request.SalesChannel,
            request.ProviderProfileId,
            ct);

        var bytes       = await upstream.Content.ReadAsByteArrayAsync(ct);
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";

        return new ExportCargoDryCommissionRuleUsageBffResponse
        {
            Bytes       = bytes,
            ContentType = contentType,
            FileName    = $"cargodry-commission-rule-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.csv",
        };
    }
}
