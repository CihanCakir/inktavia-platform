using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Notifications;

public sealed class UnsubscribePushCommand : AizenCommand<UnsubscribePushResponse>
{
    public string Endpoint { get; set; } = default!;
}

public sealed class UnsubscribePushResponse
{
    public bool Success { get; set; }
}
