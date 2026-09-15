using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class EmailNotificationDispatcher : INotificationDispatcher
{
    private readonly IEmailSender _emailSender;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly NotificationDeepLinkOptions _deepLinkOptions;
    private readonly ILogger<EmailNotificationDispatcher> _logger;

    public EmailNotificationDispatcher(
        IEmailSender emailSender,
        INotificationRepository notificationRepository,
        INotificationIdentityRemoteCall identity,
        IOptions<NotificationDeepLinkOptions> deepLinkOptions,
        ILogger<EmailNotificationDispatcher> logger)
    {
        _emailSender = emailSender;
        _notificationRepository = notificationRepository;
        _identity = identity;
        _deepLinkOptions = deepLinkOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Wave 4A + owner-bridge — ensure the email BODY contains a clickable link to the notification's deep link (the
    /// original complaint was "no mail ever contained a link"). Custom schemes don't work in webmail, so the link is
    /// resolved to a WEB url per audience, inferred from the deep-link SHAPE:
    ///  • mobile scheme (inktavia-marine://path) → OWNER → the mobile-BFF bridge (OwnerLinkBaseUrl/path) which
    ///    redirects into the app; the raw scheme stays on the InApp channel untouched.
    ///  • absolute http/https → used as-is (already an audience-correct URL from the deep-link builder).
    ///  • root-relative "/app/…" → PROVIDER → ProviderWebBaseUrl.
    ///  • other root-relative "/…" → OWNER → the bridge (OwnerLinkBaseUrl).
    ///  • WebBaseUrl is the generic fallback when the audience base is unset.
    /// No embedding when no base resolves; deduped against a link the template already inlined; inserted before the
    /// closing body tag of the layout-wrapped document.
    /// </summary>
    private string ApplyEmailDeepLink(NotificationEntity notification)
    {
        var body = notification.Body;
        var link = notification.DeepLink;
        if (string.IsNullOrWhiteSpace(link)) return body;

        var o = _deepLinkOptions;
        var mobilePrefix = $"{(string.IsNullOrWhiteSpace(o.MobileScheme) ? "inktavia-marine" : o.MobileScheme.Trim())}://";
        string? url;

        if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = link; // provider/admin builders already produced an audience-correct absolute URL
        }
        else if (link.StartsWith(mobilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            // OWNER (Wave 4A supply): scheme://service-requests/57 → {ownerBridge}/service-requests/57
            var path = link[mobilePrefix.Length..].TrimStart('/');
            var ownerBase = o.OwnerLinkBaseUrl ?? o.WebBaseUrl;
            url = string.IsNullOrWhiteSpace(ownerBase) ? null : $"{ownerBase!.TrimEnd('/')}/{path}";
        }
        else if (link.StartsWith("/app/", StringComparison.OrdinalIgnoreCase))
        {
            var providerBase = o.ProviderWebBaseUrl ?? o.WebBaseUrl;
            url = string.IsNullOrWhiteSpace(providerBase) ? null : $"{providerBase!.TrimEnd('/')}{link}";
        }
        else if (link.StartsWith('/'))
        {
            // OWNER (retrofitted SR/kit relative paths) → mobile bridge
            var ownerBase = o.OwnerLinkBaseUrl ?? o.WebBaseUrl;
            url = string.IsNullOrWhiteSpace(ownerBase) ? null : $"{ownerBase!.TrimEnd('/')}{link}";
        }
        else
        {
            url = null; // unknown shape → nothing clickable to embed
        }

        if (url is null) return body;
        if (body.Contains(url, StringComparison.OrdinalIgnoreCase)) return body; // template already embedded it

        var snippet = $"<p style=\"margin-top:16px\"><a href=\"{url}\">Görüntüle / View</a></p>";

        // Insert inside the (layout-wrapped) document before the closing body tag; fall back to append when unwrapped.
        var closeBody = body.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        return closeBody >= 0 ? body.Insert(closeBody, snippet) : body + snippet;
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
            var htmlBody = ApplyEmailDeepLink(notification);
            var providerRef = await _emailSender.SendAsync(
                recipientEmail, notification.Title, htmlBody, ct);
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
