using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Query.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQuery : AizenQuery<NotificationPreferencesResponse>
{
}
