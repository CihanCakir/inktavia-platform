using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetSubscriptionChurnRisk;

[DocumentationInfo("GetSubscriptionChurnRiskBffQueryHandler",
    "Proxies the subscription churn risk query to the Payment module and returns PastDue + expiry signal counts. " +
    "Used for the Churn Risk panel on the admin subscriptions dashboard.")]
public sealed class GetSubscriptionChurnRiskBffQueryHandler
    : AizenQueryHandler<GetSubscriptionChurnRiskBffQuery, GetSubscriptionChurnRiskBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetSubscriptionChurnRiskBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetSubscriptionChurnRiskBffResponse> Handle(
        GetSubscriptionChurnRiskBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetSubscriptionChurnRiskAsync(ct: ct);
        return new GetSubscriptionChurnRiskBffResponse { Result = result };
    }
}
