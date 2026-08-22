using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class InAppNotificationDispatcher : INotificationDispatcher
{
    private readonly IInAppNotificationPusher _pusher;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<InAppNotificationDispatcher> _logger;

    public InAppNotificationDispatcher(
        IInAppNotificationPusher pusher,
        INotificationRepository notificationRepository,
        ILogger<InAppNotificationDispatcher> logger)
    {
        _pusher = pusher;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        // FAZ16 (#34/#69) — DEĞİŞMEZ: Pending satırı = bildirim HİÇ iletilmedi.
        // Eskiden bu dispatcher ne başarıyı ne başarısızlığı yazıyordu; her InApp satırı sonsuza dek Pending
        // kalıyordu. E-posta ile aynı desen: transport'a gitmeden ÖNCE satırı Sending'e taşıyıp KALICI yazıyoruz,
        // sonucu (Sent/Failed) transport'tan SONRA yazıyoruz. Böylece push dönüp sonuç yazılamadan çökme olursa
        // satır 'Sending' kalır (denendi, sonuç belirsiz) — asla yanıltıcı Pending.
        notification.MarkAsSending();
        await _notificationRepository.UpdateAsync(notification, ct);

        try
        {
            await _pusher.PushToUserAsync(notification.RecipientUserId, new InAppNotificationPayload
            {
                NotificationId = notification.Id,
                Type           = notification.Type.ToString(),
                Title          = notification.Title,
                Body           = notification.Body,
                MetadataJson   = notification.MetadataJson,
                CreatedAt      = notification.CreatedAt,
            }, ct);

            notification.MarkAsSent();
            await _notificationRepository.UpdateAsync(notification, ct);
            _logger.LogInformation(
                "InApp notification dispatched: Id={Id} UserId={UserId}",
                notification.Id, notification.RecipientUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InApp dispatch failed for NotificationId={Id}", notification.Id);
            notification.MarkAsFailed();
            await _notificationRepository.UpdateAsync(notification, ct);
        }
    }
}
