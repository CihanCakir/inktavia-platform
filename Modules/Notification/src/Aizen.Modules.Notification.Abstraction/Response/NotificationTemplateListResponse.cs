using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Abstraction.Response;

public sealed class NotificationTemplateListResponse
{
    public List<NotificationTemplateDto> Items { get; init; } = [];
}
