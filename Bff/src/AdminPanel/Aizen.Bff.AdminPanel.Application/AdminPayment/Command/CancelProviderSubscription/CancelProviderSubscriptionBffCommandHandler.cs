using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.CancelProviderSubscription;

[DocumentationInfo("Cancel provider subscription BFF command handler",
    "Cancels the active subscription for a provider profile. Deactivates plan benefits at the end of the current billing period.")]
public sealed class CancelProviderSubscriptionBffCommandHandler
    : AizenCommandHandler<CancelProviderSubscriptionBffCommand, CancelProviderSubscriptionBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public CancelProviderSubscriptionBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<CancelProviderSubscriptionBffCommandResponse?> Handle(
        CancelProviderSubscriptionBffCommand request, CancellationToken ct)
    {
        var result = await _remote.CancelProviderSubscriptionAsync(request.ProviderProfileId, request.Reason, ct);
        return new CancelProviderSubscriptionBffCommandResponse { Result = result };
    }
}
