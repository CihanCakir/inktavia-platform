using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.DispatchCargoDryRenewalNotification;

[DocumentationInfo("Dispatch CargoDry renewal notification BFF command handler",
    "Calls the CargoDry admin renewals/{id}/notification/dispatch POST endpoint. " +
    "Phase 11 (July 2026).")]
public sealed class DispatchCargoDryRenewalNotificationBffCommandHandler
    : AizenCommandHandler<DispatchCargoDryRenewalNotificationBffCommand, DispatchCargoDryRenewalNotificationBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public DispatchCargoDryRenewalNotificationBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<DispatchCargoDryRenewalNotificationBffCommandResponse?> Handle(
        DispatchCargoDryRenewalNotificationBffCommand request, CancellationToken ct)
    {
        var result = await _remote.DispatchRenewalNotificationAsync(
            request.RenewalPreparationId,
            new DispatchRenewalNotificationBffRequest
            {
                RecipientEmail = request.RecipientEmail,
                RecipientPhone = request.RecipientPhone,
            }, ct);

        return new DispatchCargoDryRenewalNotificationBffCommandResponse { Preparation = result };
    }
}
