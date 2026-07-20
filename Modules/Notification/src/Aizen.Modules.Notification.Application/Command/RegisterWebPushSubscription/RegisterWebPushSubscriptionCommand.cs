using Aizen.Core.CQRS.Message;
using Aizen.Modules.Notification.Abstraction.Response;

namespace Aizen.Modules.Notification.Application.Command.RegisterWebPushSubscription;

public sealed class RegisterWebPushSubscriptionCommand : AizenCommand<PushSubscriptionResponse>
{
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}
