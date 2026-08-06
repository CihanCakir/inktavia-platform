using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.SubscribeProvider;

[DocumentationInfo("Subscribe provider BFF command handler",
    "Enrolls a provider profile in a subscription plan, activating plan benefits and setting up billing cycle.")]
public sealed class SubscribeProviderBffCommandHandler
    : AizenCommandHandler<SubscribeProviderBffCommand, SubscribeProviderBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public SubscribeProviderBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<SubscribeProviderBffCommandResponse?> Handle(
        SubscribeProviderBffCommand request, CancellationToken ct)
    {
        var result = await _remote.SubscribeProviderAsync(request.Body, ct);
        return new SubscribeProviderBffCommandResponse { Result = result };
    }
}
