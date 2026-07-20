using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.DeactivateWebPushSubscription;

public sealed class DeactivateWebPushSubscriptionCommand : AizenCommand<PushSubscriptionResponse>
{
    public string Endpoint { get; set; } = default!;
}
