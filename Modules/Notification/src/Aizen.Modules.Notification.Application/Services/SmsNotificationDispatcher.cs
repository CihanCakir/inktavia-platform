using System.Text.Json;
using System.Text.Json.Nodes;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// SMS kanalı dispatcher'ı (EmailNotificationDispatcher'ın SMS aynası). Alıcı telefonunu MetadataJson'daki
/// "recipientPhone"tan ya da profil id'den (Identity remote) çözer, E.164'e normalize eder; telefon yoksa satırı
/// sebepli Failed yapar (asla sessizce atlama). FAZ15 durum akışı: Pending → Sending (kalıcı) → Sent/Failed.
/// </summary>
public sealed class SmsNotificationDispatcher : INotificationDispatcher
{
    private readonly ISmsSender _smsSender;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly SmsOptions _options;
    private readonly ILogger<SmsNotificationDispatcher> _logger;

    public SmsNotificationDispatcher(
        ISmsSender smsSender,
        INotificationRepository notificationRepository,
        INotificationIdentityRemoteCall identity,
        IOptions<SmsOptions> options,
        ILogger<SmsNotificationDispatcher> logger)
    {
        _smsSender = smsSender;
        _notificationRepository = notificationRepository;
        _identity = identity;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        // Telefon MetadataJson'da {"recipientPhone":"..."} olarak gelebilir (OTP/CargoDry akışları zaten yazar);
        // yoksa profil id'den çözülür (owner/provider satırları UserProfiles.Id'ye dosyalanır — e-posta ile aynı sınır).
        string? rawPhone = null;
        if (!string.IsNullOrWhiteSpace(notification.MetadataJson))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(notification.MetadataJson);
                metadata?.TryGetValue("recipientPhone", out rawPhone);
            }
            catch { /* metadata beklenen biçimde değil (ör. sayısal) — çözümlemeye düş */ }
        }

        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            try
            {
                var resolved = await _identity.GetProfilePhoneNumber(notification.RecipientUserId);
                rawPhone = resolved?.Body?.PhoneNumber;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SMS phone resolve failed for NotificationId={Id} recipient={Rid}.",
                    notification.Id, notification.RecipientUserId);
            }
        }

        var e164 = PhoneNumberNormalizer.ToE164(rawPhone, _options.DefaultCountryCode);
        if (string.IsNullOrWhiteSpace(e164))
        {
            // Telefon yok → satırı SEBEPLİ Failed yap (missing-locale dürüstlük kuralı). Asla sessizce atlama.
            _logger.LogWarning("No phone for NotificationId={Id}; SMS row marked Failed.", notification.Id);
            notification.MarkAsFailed(BuildFailureMetadata(notification.MetadataJson, "telefon yok"));
            await _notificationRepository.UpdateAsync(notification, ct);
            return;
        }

        // FAZ15 DEĞİŞMEZ: Pending = SMS HİÇ gönderilmedi. Transport'a gitmeden ÖNCE Sending'e taşıyıp kalıcı yaz.
        notification.MarkAsSending();
        await _notificationRepository.UpdateAsync(notification, ct);

        try
        {
            // SMS metni gövdededir (renderer Sms kanalında Title=boş, Body=render edilmiş SMS metni).
            var result = await _smsSender.SendAsync(e164, notification.Body, ct);
            if (result.Success)
            {
                notification.MarkAsSent(result.ProviderRef);
                await _notificationRepository.UpdateAsync(notification, ct);
                _logger.LogInformation("SMS notification dispatched: Id={Id} To={To}", notification.Id, PhoneMasker.Mask(e164));
            }
            else
            {
                _logger.LogWarning("SMS send failed for NotificationId={Id}: {Error}", notification.Id, result.Error);
                notification.MarkAsFailed(BuildFailureMetadata(notification.MetadataJson, result.Error ?? "sms gönderilemedi"));
                await _notificationRepository.UpdateAsync(notification, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS dispatch failed for NotificationId={Id}", notification.Id);
            notification.MarkAsFailed(BuildFailureMetadata(notification.MetadataJson, ex.Message));
            await _notificationRepository.UpdateAsync(notification, ct);
        }
    }

    // Sebebi mevcut MetadataJson'a (varsa alanları koruyarak) "failureReason" olarak ekler; parse edilemezse yeni nesne.
    private static string BuildFailureMetadata(string? existingJson, string reason)
    {
        JsonObject obj;
        try
        {
            obj = string.IsNullOrWhiteSpace(existingJson)
                ? new JsonObject()
                : JsonNode.Parse(existingJson) as JsonObject ?? new JsonObject();
        }
        catch { obj = new JsonObject(); }

        obj["failureReason"] = reason;
        return obj.ToJsonString();
    }
}
