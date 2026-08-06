using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.NotificationTemplates.Command;

public sealed class UpdateNotificationTemplateBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public string Code          { get; init; } = default!;
    public string Name          { get; init; } = default!;
    public string TitleTemplate { get; init; } = default!;
    public string BodyTemplate  { get; init; } = default!;
}
