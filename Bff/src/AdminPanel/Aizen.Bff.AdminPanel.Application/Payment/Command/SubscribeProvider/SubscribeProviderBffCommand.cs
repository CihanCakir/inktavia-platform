using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.SubscribeProvider;

public sealed class SubscribeProviderBffCommand : AizenCommand<SubscribeProviderBffCommandResponse>
{
    public required SubscribeProviderPlanRequest Body { get; init; }
}

public sealed class SubscribeProviderBffCommandResponse
{
    public SubscribeProviderPlanResult Result { get; init; } = default!;
}
