using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPayoutStats;

[DocumentationInfo("Get payout stats BFF query handler",
    "Returns payout KPI summary: pending total, paid this month, and on-hold amounts with counts.")]
public sealed class GetPayoutStatsBffQueryHandler
    : AizenQueryHandler<GetPayoutStatsBffQuery, GetPayoutStatsBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetPayoutStatsBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetPayoutStatsBffResponse> Handle(
        GetPayoutStatsBffQuery request, CancellationToken ct)
    {
        var stats = await _remote.GetPayoutStatsAsync(ct);
        return new GetPayoutStatsBffResponse { Stats = stats };
    }
}
