using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class PushNotificationDispatcher : INotificationDispatcher
{
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IFcmSender _fcmSender;
    private readonly IPushSender _webPushSender;
    private readonly ILogger<PushNotificationDispatcher> _logger;

    public PushNotificationDispatcher(
        IUserDeviceTokenRepository tokenRepository,
        INotificationRepository notificationRepository,
        IFcmSender fcmSender,
        IPushSender webPushSender,
        ILogger<PushNotificationDispatcher> logger)
    {
        _tokenRepository = tokenRepository;
        _notificationRepository = notificationRepository;
        _fcmSender       = fcmSender;
        _webPushSender   = webPushSender;
        _logger          = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        var tokens = await _tokenRepository.GetActiveByUserAsync(notification.RecipientUserId, ct);
        if (tokens.Count == 0)
        {
            // FAZ16 (#69) — hiç aktif cihaz token'ı yok. KARAR: bu satırı Pending BIRAKMIYORUZ. Bu fazın amacı
            // "Pending = deneme henüz tamamlanmadı/çökme" sinyalini KESKİN tutmaktır; iletilecek hedefi olmayan
            // satırlar sonsuza dek Pending kalırsa o sinyal kalıcı yanlış-pozitiflerle aşınır (bir sonraki teşhis
            // aynı şekilde yanılır). "Klasik bir gönderim hatası" değil ama terminal bir iletilememe durumudur;
            // bu yüzden Failed yazıyoruz ve nedeni ('no target') log ile ayırt ettiriyoruz.
            _logger.LogInformation(
                "No active device tokens for UserId={UserId}. Push not delivered (no target).",
                notification.RecipientUserId);
            notification.MarkAsFailed();
            await _notificationRepository.UpdateAsync(notification, ct);
            return;
        }

        // FAZ16 (#34/#69) — DEĞİŞMEZ: Pending satırı = bildirim HİÇ iletilmedi.
        // Transport'a gitmeden ÖNCE Sending yaz (Pending'ten çıkış, kalıcı); sonucu döngüden SONRA yaz.
        notification.MarkAsSending();
        await _notificationRepository.UpdateAsync(notification, ct);

        // FAZ16 (#69) — TEK bildirim, N cihaz token'ı. Sonucu döngü sırasına BIRAKMIYORUZ; açıkça topluyoruz:
        //   en az bir token başarılı → Sent · hiçbiri başarılı değil (hepsi başarısız/atlandı) → Failed.
        // DeliveryProviderRef N id tutamaz → yalnız İLK başarılı ref'i ÖRNEK/temsili olarak saklarız (tam küme değil).
        var anySucceeded = false;
        string? firstSuccessRef = null;

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

                anySucceeded = true;
                firstSuccessRef ??= messageId;   // ilk başarılı ref = ÖRNEK; tüm cihazların id kümesi değil.
                _logger.LogInformation(
                    "Push sent via {Platform} to user {UserId}: Ref={Ref}",
                    token.Platform, notification.RecipientUserId,
                    messageId[..Math.Min(30, messageId.Length)]);
            }
            catch (NotImplementedException ex)
            {
                // FAZ16 (#69) — KARAR: gerçekleştirilmemiş platform (APNs/bilinmeyen) BİZİM eksiğimiz, bu bildirimin
                // gönderim başarısızlığı DEĞİL. Eskiden rethrow ediliyordu → kalan token'lar terk ediliyor, satır hiç
                // yazılmıyordu. Artık: logla-GEÇ. Bu token ne başarı ne başarısızlık sayılır (atlanır); diğer token'lar
                // denenmeye devam eder. (Hepsi atlanırsa aşağıdaki kural gereği sonuç Failed olur — hiçbiri iletilmedi.)
                _logger.LogWarning(ex,
                    "Push skipped (platform not implemented) for {Platform} token of user {UserId}.",
                    token.Platform, notification.RecipientUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push failed for {Platform} token of user {UserId}.",
                    token.Platform, notification.RecipientUserId);

                if (ex.Message.Contains("registration-token-not-registered", StringComparison.OrdinalIgnoreCase))
                    await _tokenRepository.DeactivateAsync(token.DeviceToken, ct);
            }
        }

        // FAZ16 (#34/#69) — N cihaz için tek sonuç, döngüden sonra bir kez yazılır (döngü içinde her token'da EZİLMEZ).
        if (anySucceeded)
            notification.MarkAsSent(firstSuccessRef);
        else
            notification.MarkAsFailed();
        await _notificationRepository.UpdateAsync(notification, ct);
    }
}
