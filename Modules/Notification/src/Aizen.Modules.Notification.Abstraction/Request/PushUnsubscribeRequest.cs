namespace Aizen.Modules.Notification.Abstraction.Request;

public sealed class PushUnsubscribeRequest
{
    public string Endpoint { get; set; } = default!;
}
