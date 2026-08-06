using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.ResolveCommissionRate;

[DocumentationInfo("Resolve commission rate BFF query handler",
    "Resolves the effective commission rate for a given provider profile, plan, and service category combination.")]
public sealed class ResolveCommissionRateBffQueryHandler
    : AizenQueryHandler<ResolveCommissionRateBffQuery, ResolveCommissionRateBffResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public ResolveCommissionRateBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ResolveCommissionRateBffResponse> Handle(
        ResolveCommissionRateBffQuery request, CancellationToken ct)
    {
        var rate = await _remote.ResolveCommissionRateAsync(
            request.ProviderProfileId, request.ProviderPlanId, request.CategoryCode, ct);

        return new ResolveCommissionRateBffResponse { Rate = rate };
    }
}
