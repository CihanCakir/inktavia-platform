using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDryRenewalNotification;

[DocumentationInfo("Prepare CargoDry renewal notification BFF command handler",
    "Calls the CargoDry admin renewals/{id}/notification/prepare POST endpoint. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalNotificationBffCommandHandler
    : AizenCommandHandler<PrepareCargoDryRenewalNotificationBffCommand, PrepareCargoDryRenewalNotificationBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public PrepareCargoDryRenewalNotificationBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<PrepareCargoDryRenewalNotificationBffCommandResponse?> Handle(
        PrepareCargoDryRenewalNotificationBffCommand request, CancellationToken ct)
    {
        var result = await _remote.PrepareRenewalNotificationAsync(
            request.RenewalPreparationId,
            new PrepareRenewalNotificationBffRequest
            {
                TemplateCode = request.TemplateCode,
                LanguageCode = request.LanguageCode,
                ChannelsJson = request.ChannelsJson,
            }, ct);

        return new PrepareCargoDryRenewalNotificationBffCommandResponse { Preparation = result };
    }
}
