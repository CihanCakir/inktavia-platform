using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

public sealed class UpdateMobileNotificationPreferenceCommandHandler
    : AizenCommandHandler<UpdateMobileNotificationPreferenceCommand, MobileNotificationPreferencesDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly INotificationRemoteCall _notification;

    public UpdateMobileNotificationPreferenceCommandHandler(IParticipantProfileResolver resolver, INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _notification = notification;
    }

    public override async Task<MobileNotificationPreferencesDto?> Handle(
        UpdateMobileNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var r = request.Request;
        if (r is null || string.IsNullOrWhiteSpace(r.Category) || string.IsNullOrWhiteSpace(r.Channel))
            throw new AizenBusinessException("A category and channel are required.");

        var resp = await _notification.UpdatePreference(new UpdateNotificationPreferenceRequest
        {
            Category = r.Category!.Trim(),
            Channel  = r.Channel!.Trim(),
            Enabled  = r.Enabled,
        });

        return MobileNotificationMapper.MapPreferences(resp?.Body);
    }
}
