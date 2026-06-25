using Aizen.Bff.AdminPanel.Application.Common.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminNotificationTemplates.Command;

public sealed class UpdateNotificationTemplateCommand : AizenCommand<AdminBffCommandResultDto>
{
    public string Code          { get; init; } = default!;
    public string Name          { get; init; } = default!;
    public string TitleTemplate { get; init; } = default!;
    public string BodyTemplate  { get; init; } = default!;
    public string UserToken     { get; init; } = default!;
}
