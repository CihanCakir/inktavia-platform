using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetCommissionRuleStats;

[DocumentationInfo("Get commission rule stats BFF query handler",
    "Returns KPI aggregates for the commission rules admin dashboard strip: " +
    "TotalRules, ActiveRules, EmergencyRules, GlobalBaseRate, ScheduledRules, DraftRules. " +
    "Pure proxy to the Payment module stats endpoint — no enrichment needed.")]
public sealed class GetCommissionRuleStatsBffQueryHandler
    : AizenQueryHandler<GetCommissionRuleStatsBffQuery, GetCommissionRuleStatsBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;

    public GetCommissionRuleStatsBffQueryHandler(IAdminPaymentBffRemoteCall payment)
        => _payment = payment;

    public override async Task<GetCommissionRuleStatsBffResponse?> Handle(
        GetCommissionRuleStatsBffQuery request, CancellationToken ct)
    {
        var stats = await _payment.GetCommissionRuleStatsAsync(ct);
        return new GetCommissionRuleStatsBffResponse { Stats = stats };
    }
}
