using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

public sealed class MarkMobileNotificationReadCommandHandler
    : AizenCommandHandler<MarkMobileNotificationReadCommand, MobileMarkReadResultDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly INotificationRemoteCall _notification;

    public MarkMobileNotificationReadCommandHandler(IParticipantProfileResolver resolver, INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _notification = notification;
    }

    public override async Task<MobileMarkReadResultDto?> Handle(
        MarkMobileNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _notification.MarkAsRead(request.NotificationId);
        var body = resp?.Body
            ?? throw new AizenBusinessException("Could not mark the notification read.");

        return MobileNotificationMapper.MapMarkRead(body);
    }
}
