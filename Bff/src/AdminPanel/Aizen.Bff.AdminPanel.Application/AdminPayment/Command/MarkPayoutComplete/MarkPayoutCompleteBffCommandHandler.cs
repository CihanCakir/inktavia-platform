using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Command.MarkPayoutComplete;

[DocumentationInfo("Mark payout complete BFF command handler",
    "Marks a pending provider payout as completed after manual fund transfer or gateway payout confirmation.")]
public sealed class MarkPayoutCompleteBffCommandHandler
    : AizenCommandHandler<MarkPayoutCompleteBffCommand, MarkPayoutCompleteBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public MarkPayoutCompleteBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<MarkPayoutCompleteBffCommandResponse?> Handle(
        MarkPayoutCompleteBffCommand request, CancellationToken ct)
    {
        var result = await _remote.MarkPayoutCompleteAsync(request.Id, request.Body, ct);
        return new MarkPayoutCompleteBffCommandResponse { Result = result };
    }
}
