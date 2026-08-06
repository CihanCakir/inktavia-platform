using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.HoldPayout;

[DocumentationInfo("Hold payout BFF command handler",
    "Places a pending provider payout on administrative hold with a mandatory reason. Payout cannot be dispatched until approved or released.")]
public sealed class HoldPayoutBffCommandHandler
    : AizenCommandHandler<HoldPayoutBffCommand, HoldPayoutBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public HoldPayoutBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<HoldPayoutBffCommandResponse?> Handle(
        HoldPayoutBffCommand request, CancellationToken ct)
    {
        var result = await _remote.HoldPayoutAsync(request.Id, request.Body, ct);
        return new HoldPayoutBffCommandResponse { Result = result };
    }
}
