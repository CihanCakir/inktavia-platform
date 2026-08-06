using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetSubscriptionStats;

[DocumentationInfo("Get subscription stats BFF query handler",
    "Returns subscription management KPI summary: active provider and participant counts, MRR, failed renewals, and expiring-soon alerts.")]
public sealed class GetSubscriptionStatsBffQueryHandler
    : AizenQueryHandler<GetSubscriptionStatsBffQuery, GetSubscriptionStatsBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetSubscriptionStatsBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetSubscriptionStatsBffResponse> Handle(
        GetSubscriptionStatsBffQuery request, CancellationToken ct)
    {
        var stats = await _remote.GetSubscriptionStatsAsync(ct);
        return new GetSubscriptionStatsBffResponse { Stats = stats };
    }
}
