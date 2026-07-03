using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetSubscriptionMrrTrend;

[DocumentationInfo("GetSubscriptionMrrTrendBffQueryHandler",
    "Proxies the subscription MRR trend query to the Payment module and returns monthly revenue data. " +
    "Used for the Revenue Velocity chart on the admin subscriptions dashboard.")]
public sealed class GetSubscriptionMrrTrendBffQueryHandler
    : AizenQueryHandler<GetSubscriptionMrrTrendBffQuery, GetSubscriptionMrrTrendBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetSubscriptionMrrTrendBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetSubscriptionMrrTrendBffResponse> Handle(
        GetSubscriptionMrrTrendBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetSubscriptionMrrTrendAsync(months: request.Months, ct: ct);
        return new GetSubscriptionMrrTrendBffResponse { Result = result };
    }
}
