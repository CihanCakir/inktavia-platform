using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>PATCH /api/v1/mobile/notifications/{id}/read — mark one of the caller's notifications read.</summary>
public sealed class MarkMobileNotificationReadCommand : AizenCommand<MobileMarkReadResultDto>
{
    public MarkMobileNotificationReadCommand(long notificationId) => NotificationId = notificationId;
    public long NotificationId { get; }
}
