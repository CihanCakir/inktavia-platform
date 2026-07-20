using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class UnsubscribePushCommand : AizenCommand<PushSubscriptionResponse>
{
    public string Endpoint { get; set; } = default!;
}
