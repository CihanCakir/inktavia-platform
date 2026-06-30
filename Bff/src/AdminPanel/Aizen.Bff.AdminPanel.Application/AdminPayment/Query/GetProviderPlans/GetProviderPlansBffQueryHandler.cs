using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetProviderPlans;

[DocumentationInfo("Get provider plans BFF query handler",
    "Returns all active provider subscription plans ordered by sort order, used for subscription plan listings and admin management.")]
public sealed class GetProviderPlansBffQueryHandler
    : AizenQueryHandler<GetProviderPlansBffQuery, GetProviderPlansBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetProviderPlansBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProviderPlansBffResponse> Handle(
        GetProviderPlansBffQuery request, CancellationToken ct)
    {
        var plans = await _remote.GetProviderPlansAsync(ct);
        return new GetProviderPlansBffResponse { Plans = plans };
    }
}
