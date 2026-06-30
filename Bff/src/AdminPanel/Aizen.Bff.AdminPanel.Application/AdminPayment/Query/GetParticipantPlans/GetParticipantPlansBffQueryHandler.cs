using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetParticipantPlans;

[DocumentationInfo("Get participant plans BFF query handler",
    "Returns all active participant subscription plans ordered by sort order, used for plan listings and admin management.")]
public sealed class GetParticipantPlansBffQueryHandler
    : AizenQueryHandler<GetParticipantPlansBffQuery, GetParticipantPlansBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public GetParticipantPlansBffQueryHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetParticipantPlansBffResponse> Handle(
        GetParticipantPlansBffQuery request, CancellationToken ct)
    {
        var plans = await _remote.GetParticipantPlansAsync(ct);
        return new GetParticipantPlansBffResponse { Plans = plans };
    }
}
