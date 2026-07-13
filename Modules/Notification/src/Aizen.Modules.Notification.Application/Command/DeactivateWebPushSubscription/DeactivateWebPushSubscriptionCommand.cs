using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Notification.Application.Command.DeactivateWebPushSubscription;

public sealed class DeactivateWebPushSubscriptionCommand : AizenCommand<bool>
{
    public string Endpoint { get; set; } = default!;
}
