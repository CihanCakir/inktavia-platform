using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class PushNotificationDispatcher : INotificationDispatcher
{
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly IFcmSender _fcmSender;
    private readonly ILogger<PushNotificationDispatcher> _logger;

    public PushNotificationDispatcher(
        IUserDeviceTokenRepository tokenRepository,
        IFcmSender fcmSender,
        ILogger<PushNotificationDispatcher> logger)
    {
        _tokenRepository = tokenRepository;
        _fcmSender       = fcmSender;
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
                var messageId = await _fcmSender.SendAsync(
                    token.DeviceToken, notification.Title, notification.Body,
                    notification.MetadataJson, ct);
                notification.MarkAsSent(messageId);
                _logger.LogInformation(
                    "Push sent to token {Token}: MessageId={MessageId}",
                    token.DeviceToken[..Math.Min(10, token.DeviceToken.Length)] + "...", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push failed for token");
                if (ex.Message.Contains("registration-token-not-registered", StringComparison.OrdinalIgnoreCase))
                    await _tokenRepository.DeactivateAsync(token.DeviceToken, ct);
            }
        }
    }
}
