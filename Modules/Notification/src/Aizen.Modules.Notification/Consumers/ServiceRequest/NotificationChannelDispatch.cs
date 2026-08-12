using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

/// <summary>
/// BE_NF2 — sends the same event on BOTH channels: <b>InApp</b> (the canonical inbox list row, which also drives web +
/// FCM push via <c>NotificationSentPushConsumer</c>) and <b>Email</b> (delivery-only; N-B Email-gated in
/// <c>SendNotificationCommandHandler</c>). The inbox stays ONE logical row because the read query is filtered to InApp
/// (NF1 D4). The Email recipient address is resolved from <c>RecipientUserId</c> (a profile id) inside
/// <c>EmailNotificationDispatcher</c>, so callers pass the same recipient/variables for both channels.
///
/// Use only where <paramref name="recipientId"/> is a genuine profile id (owner participant profile / provider
/// organizer profile), since the email resolver keys on <c>UserProfiles.Id</c>.
/// </summary>
internal static class NotificationChannelDispatch
{
    public static async Task SendInAppAndEmailAsync(
        ISender sender, long recipientId, NotificationType type,
        Dictionary<string, string> variables, string? metadataJson,
        string referenceType, long referenceId, CancellationToken ct)
    {
        await sender.Send(new SendNotificationCommand
        {
            RecipientUserId = recipientId,
            Type            = type,
            Channel         = NotificationChannel.InApp,
            Variables       = variables,
            MetadataJson    = metadataJson,
            ReferenceType   = referenceType,
            ReferenceId     = referenceId,
        }, ct);

        await sender.Send(new SendNotificationCommand
        {
            RecipientUserId = recipientId,
            Type            = type,
            Channel         = NotificationChannel.Email,
            Variables       = variables,
            MetadataJson    = metadataJson,
            ReferenceType   = referenceType,
            ReferenceId     = referenceId,
        }, ct);
    }
}
