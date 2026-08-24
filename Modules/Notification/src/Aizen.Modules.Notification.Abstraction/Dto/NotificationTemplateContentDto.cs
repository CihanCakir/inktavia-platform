using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>Tam bir içerik satırı (bir channel×locale×version). Taslak kaydetme/yayınlama yanıtı olarak da döner.</summary>
public sealed class NotificationTemplateContentDto
{
    public long                  Id               { get; init; }
    public long                  TemplateId       { get; init; }
    public NotificationChannel   Channel          { get; init; }
    public string                Locale           { get; init; } = default!;
    public int                   Version          { get; init; }
    public TemplateContentStatus Status           { get; init; }

    public string?               SubjectTemplate  { get; init; }
    public string?               HtmlTemplate     { get; init; }
    public string?               TextTemplate     { get; init; }
    public string?               LayoutCode       { get; init; }
    public string?               TitleTemplate    { get; init; }
    public string?               BodyTemplate     { get; init; }
    public string?               DeepLinkTemplate { get; init; }
    public string?               SmsTextTemplate  { get; init; }

    public DateTimeOffset        CreatedAt        { get; init; }
    public DateTimeOffset?       UpdatedAt        { get; init; }
}
