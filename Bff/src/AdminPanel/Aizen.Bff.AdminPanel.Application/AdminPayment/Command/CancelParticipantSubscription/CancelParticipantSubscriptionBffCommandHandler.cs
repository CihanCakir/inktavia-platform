using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelParticipantSubscription;

[DocumentationInfo("Cancel participant subscription BFF command handler",
    "Cancels the active subscription for a participant profile. Plan benefits are deactivated at the end of the billing period.")]
public sealed class CancelParticipantSubscriptionBffCommandHandler
    : AizenCommandHandler<CancelParticipantSubscriptionBffCommand, CancelParticipantSubscriptionBffCommandResponse>
{
    private readonly IAdminPaymentBffRemoteCall _remote;

    public CancelParticipantSubscriptionBffCommandHandler(IAdminPaymentBffRemoteCall remote)
        => _remote = remote;

    public override async Task<CancelParticipantSubscriptionBffCommandResponse?> Handle(
        CancelParticipantSubscriptionBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CancelParticipantSubscriptionAsync(request.ParticipantProfileId, request.Reason, ct);
        return new CancelParticipantSubscriptionBffCommandResponse { Result = result };
    }
}
