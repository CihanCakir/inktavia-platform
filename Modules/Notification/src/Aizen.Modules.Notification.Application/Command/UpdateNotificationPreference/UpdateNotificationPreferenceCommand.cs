using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.UpdateNotificationPreference;

public sealed class UpdateNotificationPreferenceCommand : AizenCommand<NotificationPreferencesResponse>
{
    public string Category { get; set; } = default!;
    public string Channel  { get; set; } = default!;
    public bool   Enabled  { get; set; }
}
