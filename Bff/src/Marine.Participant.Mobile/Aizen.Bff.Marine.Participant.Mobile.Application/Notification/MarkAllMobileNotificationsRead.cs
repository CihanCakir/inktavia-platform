using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>POST /api/v1/mobile/notifications/mark-all-read — mark all of the caller's notifications read.</summary>
public sealed class MarkAllMobileNotificationsReadCommand : AizenCommand<MobileMarkAllReadResultDto> { }

public sealed class MarkAllMobileNotificationsReadCommandHandler
    : AizenCommandHandler<MarkAllMobileNotificationsReadCommand, MobileMarkAllReadResultDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly INotificationRemoteCall _notification;

    public MarkAllMobileNotificationsReadCommandHandler(IParticipantProfileResolver resolver, INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _notification = notification;
    }

    public override async Task<MobileMarkAllReadResultDto?> Handle(
        MarkAllMobileNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _notification.MarkAllAsRead();
        var body = resp?.Body
            ?? throw new AizenBusinessException("Could not mark notifications read.");

        return MobileNotificationMapper.MapMarkAll(body);
    }
}
