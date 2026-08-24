using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Application.Query.GetTemplateContentVersions;

public sealed class GetTemplateContentVersionsQuery : AizenQuery<List<NotificationTemplateVersionDto>>
{
    public string              Code    { get; init; } = default!;
    public NotificationChannel Channel { get; init; }
    public string              Locale  { get; init; } = default!;
}
