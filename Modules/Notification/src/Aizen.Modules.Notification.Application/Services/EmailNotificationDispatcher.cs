using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class EmailNotificationDispatcher : INotificationDispatcher
{
    private readonly IEmailSender _emailSender;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly ILogger<EmailNotificationDispatcher> _logger;

    public EmailNotificationDispatcher(
        IEmailSender emailSender,
        INotificationRepository notificationRepository,
        INotificationIdentityRemoteCall identity,
        ILogger<EmailNotificationDispatcher> logger)
    {
        _emailSender = emailSender;
        _notificationRepository = notificationRepository;
        _identity = identity;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationEntity notification, CancellationToken ct)
    {
        // The recipient email may be supplied in MetadataJson as {"recipientEmail":"..."} (e.g. OTP / CargoDry flows
        // that already hold it). For all-string metadata that path is used; SR/offer metadata is numeric and won't
        // parse here (swallowed) → we resolve below.
        string? recipientEmail = null;
        if (!string.IsNullOrWhiteSpace(notification.MetadataJson))
        {
            try
            {
                var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(notification.MetadataJson);
                metadata?.TryGetValue("recipientEmail", out recipientEmail);
            }
            catch { /* metadata is not in expected format, skip */ }
        }

        // BE_NF2 — no email in metadata → resolve it from the recipient's profile id. Owner-facing rows are filed under
        // the participant profile id (NF1b) and provider-facing rows under the provider (organizer) profile id — both
        // are UserProfiles.Id, so one lookup addresses both. Best-effort: a resolver hiccup just skips the email (the
        // InApp row + push already fired).
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            try
            {
                var resolved = await _identity.GetProfileContactEmail(notification.RecipientUserId);
                recipientEmail = resolved?.Body?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Email recipient resolve failed for NotificationId={Id} recipient={Rid}; email skipped.",
                    notification.Id, notification.RecipientUserId);
            }
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning(
                "No recipient email found for NotificationId={Id}. Email dispatch skipped.",
                notification.Id);
            return;
        }

        // FAZ15 (#34) — DEĞİŞMEZ: Pending satırı = e-posta HİÇ gönderilmedi.
        // Transport'a gitmeden ÖNCE satırı Sending'e taşıyıp KALICI yazıyoruz. Böylece SendMailAsync başarıyla dönüp
        // aşağıdaki Sent yazımı gerçekleşemeden çökme olursa satır 'Sending' kalır (denendi, sonuç belirsiz) — asla
        // gönderilmiş bir e-postayı "hiç denenmemiş" gibi gösteren Pending değil. (Eskiden yalnız failure yazılıyordu.)
        notification.MarkAsSending();
        await _notificationRepository.UpdateAsync(notification, ct);

        try
        {
            var providerRef = await _emailSender.SendAsync(
                recipientEmail, notification.Title, notification.Body, ct);
            notification.MarkAsSent(providerRef);
            // FAZ15 (#34) — başarı ARTIK kalıcı: eskiden success dalı UpdateAsync çağırmıyordu, satır Pending kalıyordu.
            await _notificationRepository.UpdateAsync(notification, ct);
            _logger.LogInformation(
                "Email notification dispatched: Id={Id} To={To}",
                notification.Id, recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email dispatch failed for NotificationId={Id}", notification.Id);
            notification.MarkAsFailed();
            await _notificationRepository.UpdateAsync(notification, ct);
        }
    }
}
