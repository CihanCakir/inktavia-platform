using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentDashboardKpis;

[DocumentationInfo("Get payment dashboard KPIs BFF query handler",
    "Returns aggregated payment dashboard KPI data: gross volume, platform commission, pending payouts, and escrow balances with period-over-period change indicators.")]
public sealed class GetPaymentDashboardKpisBffQueryHandler
    : AizenQueryHandler<GetPaymentDashboardKpisBffQuery, GetPaymentDashboardKpisBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetPaymentDashboardKpisBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetPaymentDashboardKpisBffResponse> Handle(
        GetPaymentDashboardKpisBffQuery request, CancellationToken ct)
    {
        var kpis = await _remote.GetDashboardKpisAsync(ct);
        return new GetPaymentDashboardKpisBffResponse { Kpis = kpis };
    }
}
