namespace Aizen.Modules.Notification.Abstraction.Request;

/// <summary>
/// Bir (channel, locale) için taslak içeriği kaydeder. Kanal-özel alanlar opsiyonel (ilgisiz kanallar null).
/// Channel &amp; locale route'tan gelir. Kaydetme MEVCUT taslağı günceller ya da yoksa YENİ bir Draft sürüm oluşturur —
/// Published/Archived ASLA mutasyona uğramaz (handler garanti eder).
/// </summary>
public sealed class SaveNotificationTemplateContentRequest
{
    // Email
    public string? SubjectTemplate  { get; set; }
    public string? HtmlTemplate     { get; set; }
    public string? TextTemplate     { get; set; }
    public string? LayoutCode       { get; set; }

    // InApp / Push
    public string? TitleTemplate    { get; set; }
    public string? BodyTemplate     { get; set; }
    public string? DeepLinkTemplate { get; set; }

    // Sms
    public string? SmsTextTemplate  { get; set; }
}
