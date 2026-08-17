using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class UpdateNotificationPreferenceBffCommand : AizenCommand<NotificationPreferencesResponse>
{
    public string Category { get; set; } = default!;
    public string Channel  { get; set; } = default!;
    public bool   Enabled  { get; set; }
}
