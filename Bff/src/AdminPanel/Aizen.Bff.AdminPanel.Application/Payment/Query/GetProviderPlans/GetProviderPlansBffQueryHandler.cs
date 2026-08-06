using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlans;

[DocumentationInfo("Get provider plans BFF query handler",
    "Returns all active provider subscription plans ordered by sort order, used for subscription plan listings and admin management.")]
public sealed class GetProviderPlansBffQueryHandler
    : AizenQueryHandler<GetProviderPlansBffQuery, GetProviderPlansBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public GetProviderPlansBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<GetProviderPlansBffResponse> Handle(
        GetProviderPlansBffQuery request, CancellationToken ct)
    {
        // Admin management surface must see inactive plans too (public marketing GET stays active-only).
        var plans = await _remote.GetProviderPlansAsync(includeInactive: true, ct: ct);
        return new GetProviderPlansBffResponse { Plans = plans };
    }
}
