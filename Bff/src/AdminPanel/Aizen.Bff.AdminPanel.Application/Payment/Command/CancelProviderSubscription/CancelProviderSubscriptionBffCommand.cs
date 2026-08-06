using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CancelProviderSubscription;

public sealed class CancelProviderSubscriptionBffCommand : AizenCommand<CancelProviderSubscriptionBffCommandResponse>
{
    public long    ProviderProfileId { get; init; }
    public string? Reason            { get; init; }
}

public sealed class CancelProviderSubscriptionBffCommandResponse
{
    public CancelSubscriptionResult Result { get; init; } = default!;
}
