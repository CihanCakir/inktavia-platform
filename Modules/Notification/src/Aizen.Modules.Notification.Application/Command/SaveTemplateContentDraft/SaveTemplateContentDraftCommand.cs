using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Command.SaveTemplateContentDraft;

public sealed class SaveTemplateContentDraftCommand : AizenCommand<NotificationTemplateContentDto>
{
    public string              Code    { get; set; } = default!;
    public NotificationChannel Channel { get; set; }
    public string              Locale  { get; set; } = default!;

    public string? SubjectTemplate  { get; set; }
    public string? HtmlTemplate     { get; set; }
    public string? TextTemplate     { get; set; }
    public string? LayoutCode       { get; set; }
    public string? TitleTemplate    { get; set; }
    public string? BodyTemplate     { get; set; }
    public string? DeepLinkTemplate { get; set; }
    public string? SmsTextTemplate  { get; set; }
}
