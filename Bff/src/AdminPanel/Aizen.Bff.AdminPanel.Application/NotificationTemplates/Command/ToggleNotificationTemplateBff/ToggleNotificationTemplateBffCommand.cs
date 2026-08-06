using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.NotificationTemplates.Command;

public sealed class ToggleNotificationTemplateBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public string Code      { get; init; } = default!;
}
