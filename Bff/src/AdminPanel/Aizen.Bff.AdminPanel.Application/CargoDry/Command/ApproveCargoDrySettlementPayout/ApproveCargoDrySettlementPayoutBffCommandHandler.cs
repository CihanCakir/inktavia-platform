using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ApproveCargoDrySettlementPayout;

[DocumentationInfo("Approve CargoDry settlement payout BFF command handler",
    "Forwards the approve-payout request to the CargoDry commercial module. " +
    "The module transitions the PayoutRecord from Pending → Approved in the Payment module. " +
    "Settlement status remains Scheduled after this call. " +
    "No Iyzico call. No bank transfer. Phase 4D (July 2026).")]
public sealed class ApproveCargoDrySettlementPayoutBffCommandHandler
    : AizenCommandHandler<ApproveCargoDrySettlementPayoutBffCommand,
                          ApproveCargoDrySettlementPayoutBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public ApproveCargoDrySettlementPayoutBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<ApproveCargoDrySettlementPayoutBffCommandResponse> Handle(
        ApproveCargoDrySettlementPayoutBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new ApproveCargoDrySettlementPayoutBffRequest
        {
            ApprovedByUserId = request.ApprovedByUserId,
            Note             = request.Note,
        };

        var result = await _remote.ApproveSettlementPayoutAsync(
            request.SettlementId, remoteRequest, ct);

        return new ApproveCargoDrySettlementPayoutBffCommandResponse
        {
            Settlement   = result.Settlement,
            PayoutResult = result.PayoutResult,
        };
    }
}
