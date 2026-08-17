using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPayoutStats;

[DocumentationInfo("Get payout stats BFF query handler",
    "Returns payout KPI summary: pending total, paid this month, and on-hold amounts with counts.")]
public sealed class GetPayoutStatsBffQueryHandler
    : AizenQueryHandler<GetPayoutStatsBffQuery, GetPayoutStatsBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetPayoutStatsBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetPayoutStatsBffResponse> Handle(
        GetPayoutStatsBffQuery request, CancellationToken ct)
    {
        var stats = await _remote.GetPayoutStatsAsync(ct);
        return new GetPayoutStatsBffResponse { Stats = stats };
    }
}
