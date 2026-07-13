using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class PushNotificationDispatcher : INotificationDispatcher
{
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly IFcmSender _fcmSender;
    private readonly IPushSender _webPushSender;
    private readonly ILogger<PushNotificationDispatcher> _logger;

    public PushNotificationDispatcher(
        IUserDeviceTokenRepository tokenRepository,
        IFcmSender fcmSender,
        IPushSender webPushSender,
        ILogger<PushNotificationDispatcher> logger)
    {
        _tokenRepository = tokenRepository;
        _fcmSender       = fcmSender;
        _webPushSender   = webPushSender;
        _logger          = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        var tokens = await _tokenRepository.GetActiveByUserAsync(notification.RecipientUserId, ct);
        if (tokens.Count == 0)
        {
            _logger.LogInformation(
                "No active device tokens for UserId={UserId}. Push skipped.",
                notification.RecipientUserId);
            return;
        }

        foreach (var token in tokens)
        {
            try
            {
                var messageId = token.Platform switch
                {
                    PushPlatform.WebPush => await _webPushSender.SendAsync(
                        token, notification.Title, notification.Body,
                        notification.MetadataJson, ct),

                    PushPlatform.Fcm => await _fcmSender.SendAsync(
                        token.DeviceToken, notification.Title, notification.Body,
                        notification.MetadataJson, ct),

                    PushPlatform.Apns => throw new NotImplementedException(
                        $"APNs push sender is not implemented. Platform={token.Platform}, UserId={notification.RecipientUserId}"),

                    _ => throw new NotImplementedException(
                        $"Unknown push platform: {token.Platform}"),
                };

                notification.MarkAsSent(messageId);
                _logger.LogInformation(
                    "Push sent via {Platform} to user {UserId}: Ref={Ref}",
                    token.Platform, notification.RecipientUserId,
                    messageId[..Math.Min(30, messageId.Length)]);
            }
            catch (NotImplementedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push failed for {Platform} token of user {UserId}.",
                    token.Platform, notification.RecipientUserId);

                if (ex.Message.Contains("registration-token-not-registered", StringComparison.OrdinalIgnoreCase))
                    await _tokenRepository.DeactivateAsync(token.DeviceToken, ct);
            }
        }
    }
}
