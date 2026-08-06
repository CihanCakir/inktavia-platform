using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.NotificationTemplates.Command;

public sealed class CreateNotificationTemplateBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public string              TemplateCode  { get; init; } = default!;
    public string              Name          { get; init; } = default!;
    public NotificationType    Type          { get; init; }
    public NotificationChannel Channel       { get; init; }
    public string              TitleTemplate { get; init; } = default!;
    public string              BodyTemplate  { get; init; } = default!;
}
