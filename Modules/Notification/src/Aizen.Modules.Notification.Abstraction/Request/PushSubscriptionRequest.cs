namespace Aizen.Modules.Notification.Abstraction.Request;

public sealed class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = default!;
    public PushSubscriptionKeys Keys { get; set; } = default!;
}

public sealed class PushSubscriptionKeys
{
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}
