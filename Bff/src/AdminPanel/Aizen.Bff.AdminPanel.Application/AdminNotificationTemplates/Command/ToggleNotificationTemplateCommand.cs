using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;

public sealed class ToggleNotificationTemplateCommand : AizenCommand<AdminBffCommandResultDto>
{
    public string Code      { get; init; } = default!;
    public string UserToken { get; init; } = default!;
}
