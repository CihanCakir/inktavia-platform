using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class SubscribePushCommand : AizenCommand<SubscribePushResponse>
{
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;
}

public sealed class SubscribePushResponse
{
    public bool Success { get; set; }
}
