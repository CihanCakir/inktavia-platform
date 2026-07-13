using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Notification.Application.Command.RegisterWebPushSubscription;

public sealed class RegisterWebPushSubscriptionCommand : AizenCommand<bool>
{
    public long UserId { get; set; }
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}
