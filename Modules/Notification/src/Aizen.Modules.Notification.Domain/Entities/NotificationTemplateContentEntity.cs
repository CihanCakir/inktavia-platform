using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// Bir mantıksal template'in (NotificationTemplateEntity) belirli bir (Channel, Locale, Version) için SOMUT içeriği.
///
/// TASARIM KARARI — tek tablo + az sayıda nullable kanal kolonu: Her kanalın kendi içerik alanları var
/// (Email: Subject/Html/Text/Layout, InApp/Push: Title/Body/DeepLink, Sms: SmsText). Bunları ayrı tablolara bölmek
/// yerine tek tabloda tutup ilgisiz kolonları nullable bırakıyoruz. Gerekçe: kanal sayısı küçük ve sabit; render
/// tek satırdan okunuyor (join yok); admin/preview tek satırı düzenliyor. Alan-başına-tablo şeması burada karmaşıklık
/// katardı, esneklik katmazdı. Hangi kolonun hangi kanalda dolu olması gerektiği renderer'da doğrulanır.
/// </summary>
public sealed class NotificationTemplateContentEntity : AizenEntity
{
    /// <summary>FK → NotificationTemplateEntity.Id (mantıksal template).</summary>
    public long                  TemplateId        { get; private set; }
    public NotificationChannel   Channel           { get; private set; }
    /// <summary>Bölge-siz locale kodu ("tr"/"en").</summary>
    public string                Locale            { get; private set; } = default!;
    public int                   Version           { get; private set; }
    public TemplateContentStatus Status            { get; private set; }

    // Email kanalı alanları
    public string?               SubjectTemplate   { get; private set; }
    public string?               HtmlTemplate      { get; private set; }
    public string?               TextTemplate      { get; private set; }
    /// <summary>Email için sarmalayıcı layout kodu (EmailLayoutEntity.Code). Null ise "DEFAULT" varsayılır.</summary>
    public string?               LayoutCode        { get; private set; }

    // InApp / Push kanalı alanları
    public string?               TitleTemplate     { get; private set; }
    public string?               BodyTemplate      { get; private set; }
    public string?               DeepLinkTemplate  { get; private set; }

    // Sms kanalı alanı
    public string?               SmsTextTemplate   { get; private set; }

    public DateTimeOffset        CreatedAt         { get; private set; }
    public DateTimeOffset?       UpdatedAt         { get; private set; }

    private NotificationTemplateContentEntity() { }

    public static NotificationTemplateContentEntity Create(
        long templateId,
        NotificationChannel channel,
        string locale,
        int version,
        TemplateContentStatus status,
        string? titleTemplate = null,
        string? bodyTemplate = null,
        string? subjectTemplate = null,
        string? htmlTemplate = null,
        string? textTemplate = null,
        string? deepLinkTemplate = null,
        string? smsTextTemplate = null,
        string? layoutCode = null)
    {
        return new NotificationTemplateContentEntity
        {
            TemplateId       = templateId,
            Channel          = channel,
            Locale           = locale.ToLowerInvariant(),
            Version          = version,
            Status           = status,
            TitleTemplate    = titleTemplate,
            BodyTemplate     = bodyTemplate,
            SubjectTemplate  = subjectTemplate,
            HtmlTemplate     = htmlTemplate,
            TextTemplate     = textTemplate,
            DeepLinkTemplate = deepLinkTemplate,
            SmsTextTemplate  = smsTextTemplate,
            LayoutCode       = layoutCode,
            CreatedAt        = DateTimeOffset.UtcNow,
        };
    }

    public void Publish()
    {
        Status    = TemplateContentStatus.Published;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status    = TemplateContentStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
