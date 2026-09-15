using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.CargoDry;

/// <summary>
/// Wave 4A — routes an InApp notification to EVERY admin user (the admin panel in-app feed / bell). Resolves admin
/// ids from Identity, then sends one InApp notification per admin, sequentially (single scoped DbContext — mirrors
/// SupportRequestOpenedConsumer). Best-effort: Identity failure logs + returns (the primary owner/provider
/// notification must never be rolled back over the admin-feed copy).
/// </summary>
internal static class AdminFeedDispatch
{
    public static async Task SendToAllAdminsInAppAsync(
        ISender sender, INotificationIdentityRemoteCall identity, ILogger logger,
        NotificationType type, Dictionary<string, string> variables,
        string referenceType, long referenceId, CancellationToken ct)
    {
        List<long> adminIds;
        try
        {
            var response = await identity.GetAdminUserIds();
            adminIds = response?.Body ?? new List<long>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Admin-feed fan-out: could not resolve admin user ids ({RefType} {RefId}).", referenceType, referenceId);
            return;
        }

        foreach (var adminId in adminIds)
        {
            await sender.Send(new SendNotificationCommand
            {
                RecipientUserId = adminId,
                Type            = type,
                Channel         = NotificationChannel.InApp,
                Variables       = variables,
                ReferenceType   = referenceType,
                ReferenceId     = referenceId,
            }, ct);
        }
    }
}
