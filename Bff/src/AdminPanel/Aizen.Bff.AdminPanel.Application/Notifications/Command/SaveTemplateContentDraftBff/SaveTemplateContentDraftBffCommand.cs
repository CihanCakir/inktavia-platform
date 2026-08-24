using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

public sealed class SaveTemplateContentDraftBffCommand : AizenCommand<NotificationTemplateContentDto>
{
    public string              Code    { get; init; } = default!;
    public NotificationChannel Channel { get; init; }
    public string              Locale  { get; init; } = default!;

    public string? SubjectTemplate  { get; init; }
    public string? HtmlTemplate     { get; init; }
    public string? TextTemplate     { get; init; }
    public string? LayoutCode       { get; init; }
    public string? TitleTemplate    { get; init; }
    public string? BodyTemplate     { get; init; }
    public string? DeepLinkTemplate { get; init; }
    public string? SmsTextTemplate  { get; init; }
}
