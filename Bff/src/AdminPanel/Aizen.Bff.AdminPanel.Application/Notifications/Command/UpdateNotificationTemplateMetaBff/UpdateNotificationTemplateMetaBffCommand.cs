using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Command;

public sealed class UpdateNotificationTemplateMetaBffCommand : AizenCommand<AdminBffCommandResultDto>
{
    public string  Code        { get; init; } = default!;
    public string  Name        { get; init; } = default!;
    public string? Description  { get; init; }
    public bool    IsActive    { get; init; }
}
