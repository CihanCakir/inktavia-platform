using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.FailCargoDrySettlementPayout;

[DocumentationInfo("Fail CargoDry settlement payout BFF command handler",
    "Forwards the fail-payout request to the CargoDry commercial module. " +
    "Records a payout failure on both the Payment module PayoutRecord and the settlement. " +
    "Settlement status remains Scheduled — allows the admin to retry after resolving the failure. " +
    "FailureReason is required. Cannot fail an already-Settled settlement. " +
    "No Iyzico call. Phase 4D (July 2026).")]
public sealed class FailCargoDrySettlementPayoutBffCommandHandler
    : AizenCommandHandler<FailCargoDrySettlementPayoutBffCommand,
                          FailCargoDrySettlementPayoutBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public FailCargoDrySettlementPayoutBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<FailCargoDrySettlementPayoutBffCommandResponse> Handle(
        FailCargoDrySettlementPayoutBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new FailCargoDrySettlementPayoutBffRequest
        {
            FailedByUserId    = request.FailedByUserId,
            FailureReason     = request.FailureReason,
            ExternalReference = request.ExternalReference,
            Note              = request.Note,
        };

        var result = await _remote.FailSettlementPayoutAsync(
            request.SettlementId, remoteRequest, ct);

        return new FailCargoDrySettlementPayoutBffCommandResponse
        {
            Settlement   = result.Settlement,
            PayoutResult = result.PayoutResult,
        };
    }
}
