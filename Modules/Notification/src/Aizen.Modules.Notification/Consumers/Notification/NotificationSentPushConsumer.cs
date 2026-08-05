using System.Text.Json;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Message;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Consumers.Notification;

/// <summary>
/// N-A web-push dispatch. Mirrors the BFF-hosted <c>AdminNotificationRealtimeConsumer</c>: both consume the
/// thin <see cref="NotificationSentMessage"/> the SendNotification handler publishes for InApp notifications.
/// The realtime consumer drives the in-app live badge; THIS one delivers a browser/OS web push (works with the
/// tab backgrounded/closed) to every recipient device that has an active WebPush subscription.
///
/// Gating for N-A = "any user with an active WebPush subscription" (full per-type/channel preference gating is
/// N-B). Skips cleanly when VAPID isn't configured or the recipient has no WebPush subscription — a push hiccup
/// must never affect the notification write (already committed) or the live badge. One push per recipient device,
/// sent SEQUENTIALLY (respecting the WS2 exactly-once / sequential-loop fixes). Expired subscriptions (410 Gone)
/// are pruned inside <see cref="WebPushSender"/>.
///
/// The frame carries only Title + reference; the Body is reloaded from the persisted NotificationEntity so the push
/// body matches the in-app notification. The payload carries NO sensitive content — just title/short body + the
/// deep-link refs (referenceType/referenceId); the SW/app fetches details over authorized HTTP.
/// </summary>
public sealed class NotificationSentPushConsumer
    : AizenBaseMessageConsumer<NotificationSentMessage>
{
    private readonly IUserDeviceTokenRepository        _tokenRepository;
    private readonly INotificationRepository           _notificationRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IPushSender                       _pushSender;
    private readonly VapidOptions                      _vapid;
    private readonly ILogger<NotificationSentPushConsumer> _logger;

    public NotificationSentPushConsumer(IServiceProvider sp) : base(sp)
    {
        _tokenRepository        = sp.GetRequiredService<IUserDeviceTokenRepository>();
        _notificationRepository = sp.GetRequiredService<INotificationRepository>();
        _preferenceRepository   = sp.GetRequiredService<INotificationPreferenceRepository>();
        _pushSender             = sp.GetRequiredService<IPushSender>();
        _vapid                  = sp.GetRequiredService<IOptions<VapidOptions>>().Value;
        _logger                 = sp.GetRequiredService<ILogger<NotificationSentPushConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(NotificationSentMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(NotificationSentMessage message, CancellationToken ct)
    {
        // No VAPID keys → web push is not configured; skip silently (in-app badge still works).
        if (string.IsNullOrWhiteSpace(_vapid.PublicKey) || string.IsNullOrWhiteSpace(_vapid.PrivateKey))
        {
            _logger.LogDebug("VAPID not configured; web push skipped for NotificationId={Id}.", message.NotificationId);
            return;
        }

        // N-B preference gate: skip the push if the recipient muted this category's Push channel. The in-app inbox row
        // + live badge already persisted (this consumer only sends push), so muting push never hides the notification.
        // Locked cells (InApp baseline, Account/security) always resolve enabled — see NotificationPreferencePolicy.
        var category = NotificationCategoryMap.Resolve(message.Type);
        var storedPrefs = await _preferenceRepository.GetByUserAsync(message.RecipientUserId, ct);
        var storedPush = storedPrefs
            .Where(p => p.Category == category && p.Channel == NotificationChannel.Push)
            .Select(p => (bool?)p.Enabled)
            .FirstOrDefault();
        if (!NotificationPreferencePolicy.Resolve(category, NotificationChannel.Push, storedPush))
        {
            _logger.LogDebug(
                "Push muted for UserId={UserId} category={Category}; push skipped for NotificationId={Id} (in-app unaffected).",
                message.RecipientUserId, category, message.NotificationId);
            return;
        }

        var tokens = await _tokenRepository.GetActiveByUserAsync(message.RecipientUserId, ct);
        var webPushTokens = tokens.Where(t => t.Platform == PushPlatform.WebPush).ToList();
        if (webPushTokens.Count == 0)
        {
            _logger.LogDebug(
                "No active WebPush subscription for UserId={UserId}; push skipped for NotificationId={Id}.",
                message.RecipientUserId, message.NotificationId);
            return;
        }

        // The frame carries only the Title; reload the persisted notification for the Body (and to confirm it exists).
        var notification = await _notificationRepository.GetByIdAsync(message.NotificationId, ct);
        var title = string.IsNullOrEmpty(message.Title) ? notification?.Title ?? string.Empty : message.Title;
        var body  = notification?.Body ?? string.Empty;

        // Compact deep-link payload: referenceType/referenceId only (no sensitive content). The SW maps these to a route.
        var dataJson = JsonSerializer.Serialize(new
        {
            notificationId = message.NotificationId,
            referenceType  = message.ReferenceType,
            referenceId    = message.ReferenceId,
        });

        foreach (var token in webPushTokens)
        {
            try
            {
                await _pushSender.SendAsync(token, title, body, dataJson, ct);
                _logger.LogInformation(
                    "Web push sent to UserId={UserId} for NotificationId={Id}.",
                    message.RecipientUserId, message.NotificationId);
            }
            catch (Exception ex)
            {
                // 410/404 already pruned the subscription inside the sender; log and continue so one bad
                // subscription never blocks the others (and never fails the already-committed notification).
                _logger.LogWarning(ex,
                    "Web push delivery failed for UserId={UserId}, NotificationId={Id}; continuing.",
                    message.RecipientUserId, message.NotificationId);
            }
        }
    }

    public override Task ExecuteRollbackMessage(NotificationSentMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning(
            "Rollback: NotificationSentPushConsumer NotificationId={Id}: {Error}",
            message.NotificationId, ex.Message);
        return Task.CompletedTask;
    }
}
