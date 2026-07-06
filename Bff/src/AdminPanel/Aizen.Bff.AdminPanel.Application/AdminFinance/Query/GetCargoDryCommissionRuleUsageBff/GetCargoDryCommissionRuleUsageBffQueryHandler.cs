using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDryCommissionRuleUsageBff;

[DocumentationInfo("Get CargoDry commission rule usage report BFF query handler",
    "Proxies the admin commission rule usage request to the CargoDry module finance endpoint. " +
    "Returns grouped commission rule usage statistics for admin finance auditing. " +
    "Phase 15 (July 2026).")]
public sealed class GetCargoDryCommissionRuleUsageBffQueryHandler
    : AizenQueryHandler<GetCargoDryCommissionRuleUsageBffQuery, GetCargoDryCommissionRuleUsageBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GetCargoDryCommissionRuleUsageBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryCommissionRuleUsageBffResponse> Handle(
        GetCargoDryCommissionRuleUsageBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetCommissionRuleUsageAsync(
            request.DateFrom,
            request.DateTo,
            request.RuleId,
            request.ProductCode,
            request.SalesChannel,
            request.ProviderProfileId,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDryCommissionRuleUsageBffResponse { Report = result };
    }
}
