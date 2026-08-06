using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.ApproveManualPayout;

[DocumentationInfo("Approve manual payout BFF command handler",
    "Approves and manually dispatches a held payout after admin review. Records the gateway payout ID and admin note, then releases the hold.")]
public sealed class ApproveManualPayoutBffCommandHandler
    : AizenCommandHandler<ApproveManualPayoutBffCommand, ApproveManualPayoutBffCommandResponse>
{
    private readonly IPaymentRemoteCall _remote;

    public ApproveManualPayoutBffCommandHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ApproveManualPayoutBffCommandResponse?> Handle(
        ApproveManualPayoutBffCommand request, CancellationToken ct)
    {
        var result = await _remote.ApproveManualPayoutAsync(request.Id, request.Body, ct);
        return new ApproveManualPayoutBffCommandResponse { Result = result };
    }
}
