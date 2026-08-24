using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Notifications.Query;

public sealed class GetNotificationTemplatesPagedBffQuery : AizenQuery<NotificationTemplateListResult>
{
    public NotificationChannel?   Channel  { get; init; }
    public string?                Locale   { get; init; }
    public TemplateContentStatus? Status   { get; init; }
    public bool?                  Enabled  { get; init; }
    public string?                Search   { get; init; }
    public int                    Page     { get; init; } = 1;
    public int                    PageSize { get; init; } = 20;
}
