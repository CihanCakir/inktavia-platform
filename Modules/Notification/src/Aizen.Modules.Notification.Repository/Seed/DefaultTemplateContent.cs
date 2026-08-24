using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Repository.Seed;

/// <summary>
/// Mantıksal bir template'ten (Title/Body) 'en' v1 Published içerik üretmenin TEK doğru eşlemesi.
/// Aynı eşleme hem seed'de (taze DB) hem migration SQL'inde (mevcut satırlar) kullanılır — golden test bunu doğrular.
/// Kural: tüm kanallara Title/Body kopyalanır; Email kanalına ayrıca Subject=Title, Html=Body, Layout='DEFAULT'.
/// </summary>
public static class DefaultTemplateContent
{
    public const string DefaultLayoutCode = "DEFAULT";
    public const string DefaultLocale     = "en";

    public static NotificationTemplateContentEntity FromTemplate(
        long templateId, NotificationChannel channel, string titleTemplate, string bodyTemplate)
    {
        var isEmail = channel == NotificationChannel.Email;
        return NotificationTemplateContentEntity.Create(
            templateId:      templateId,
            channel:         channel,
            locale:          DefaultLocale,
            version:         1,
            status:          TemplateContentStatus.Published,
            titleTemplate:   titleTemplate,
            bodyTemplate:    bodyTemplate,
            subjectTemplate: isEmail ? titleTemplate : null,
            htmlTemplate:    isEmail ? bodyTemplate : null,
            layoutCode:      isEmail ? DefaultLayoutCode : null);
    }
}

/// <summary>DEFAULT e-posta layout'unun seed verisi. HtmlShell {{content}} içerir (EmailLayoutEntity zorunlu kılar).</summary>
public static class DefaultEmailLayoutSeed
{
    public const string Code = "DEFAULT";
    public const string Name = "Inktavia Default";

    // Minimal Inktavia kabuğu (tablo tabanlı, e-posta istemcisi uyumlu). {{content}} render edilen gövdeyle değişir.
    public const string HtmlShell =
        "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head>" +
        "<body style=\"margin:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;\">" +
        "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f4f5f7;padding:24px 0;\">" +
        "<tr><td align=\"center\">" +
        "<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:600px;background:#ffffff;border-radius:8px;overflow:hidden;\">" +
        "<tr><td style=\"background:#0b3d5c;padding:20px 24px;color:#ffffff;font-size:18px;font-weight:bold;\">Inktavia Marine</td></tr>" +
        "<tr><td style=\"padding:24px;font-size:15px;line-height:1.5;\">{{content}}</td></tr>" +
        "<tr><td style=\"padding:16px 24px;background:#f0f1f3;color:#8a8f98;font-size:12px;\">&#169; Inktavia Marine</td></tr>" +
        "</table></td></tr></table></body></html>";
}
