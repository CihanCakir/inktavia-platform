using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>Admin doğrudan/toplu bildirim kampanyası oluşturma isteği. Şablon YA DA custom içerik yolu (ikisi birden değil).</summary>
public sealed class CreateNotificationCampaignRequest
{
    public CampaignAudience   Audience   { get; set; }
    public CampaignTargetMode TargetMode { get; set; }

    /// <summary>TargetMode=Selected için alıcı profil id listesi.</summary>
    public List<long>?        SelectedRecipientIds { get; set; }

    /// <summary>Şablon yolu: gönderilecek template kodu.</summary>
    public string?            TemplateCode { get; set; }

    /// <summary>Custom yol: locale→{title,body}. Desteklenen tüm dilleri (tr+en) kapsamalı; kısmi kapsama reddedilir.</summary>
    public Dictionary<string, CampaignLocaleContent>? CustomContent { get; set; }

    /// <summary>Yalnız InApp ve/veya Email. Sms Faz 7'ye kadar reddedilir.</summary>
    public List<NotificationChannel> Channels { get; set; } = new();

    /// <summary>Gelecekteyse v1'de sadece saklanır (consumer store-and-refuse eder); poller sonraki faz.</summary>
    public DateTimeOffset?    ScheduledAt { get; set; }
}

/// <summary>Custom içerik yolunda bir locale için başlık + gövde.</summary>
public sealed class CampaignLocaleContent
{
    public string Title { get; set; } = default!;
    public string Body  { get; set; } = default!;
}
