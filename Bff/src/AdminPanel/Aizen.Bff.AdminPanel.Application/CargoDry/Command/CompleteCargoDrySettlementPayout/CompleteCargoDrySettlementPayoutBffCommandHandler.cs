using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.CompleteCargoDrySettlementPayout;

[DocumentationInfo("Complete CargoDry settlement payout BFF command handler",
    "Forwards the complete-payout request to the CargoDry commercial module. " +
    "The module records the manual payout completion and closes the settlement as Settled. " +
    "THIS IS THE ONLY HANDLER that causes settlement status to advance to Settled. " +
    "ManualPaymentReference is required. Idempotent if already Settled. " +
    "No Iyzico call. No automatic transfer. Phase 4D (July 2026).")]
public sealed class CompleteCargoDrySettlementPayoutBffCommandHandler
    : AizenCommandHandler<CompleteCargoDrySettlementPayoutBffCommand,
                          CompleteCargoDrySettlementPayoutBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public CompleteCargoDrySettlementPayoutBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<CompleteCargoDrySettlementPayoutBffCommandResponse> Handle(
        CompleteCargoDrySettlementPayoutBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new CompleteCargoDrySettlementPayoutBffRequest
        {
            CompletedByUserId      = request.CompletedByUserId,
            ManualPaymentReference = request.ManualPaymentReference,
            Note                   = request.Note,
        };

        var result = await _remote.CompleteSettlementPayoutAsync(
            request.SettlementId, remoteRequest, ct);

        return new CompleteCargoDrySettlementPayoutBffCommandResponse
        {
            Settlement       = result.Settlement,
            PayoutResult     = result.PayoutResult,
            AlreadyCompleted = result.AlreadyCompleted,
        };
    }
}
