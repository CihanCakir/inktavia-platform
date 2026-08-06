using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.MarkCargoDrySettlementPayoutProcessing;

[DocumentationInfo("Mark CargoDry settlement payout processing BFF command handler",
    "Forwards the mark-payout-processing request to the CargoDry commercial module. " +
    "The module transitions the PayoutRecord to Processing status in the Payment module. " +
    "Optional step between Approve and Complete. Settlement status remains Scheduled. " +
    "No Iyzico call. No bank transfer. Phase 4D (July 2026).")]
public sealed class MarkCargoDrySettlementPayoutProcessingBffCommandHandler
    : AizenCommandHandler<MarkCargoDrySettlementPayoutProcessingBffCommand,
                          MarkCargoDrySettlementPayoutProcessingBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public MarkCargoDrySettlementPayoutProcessingBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<MarkCargoDrySettlementPayoutProcessingBffCommandResponse> Handle(
        MarkCargoDrySettlementPayoutProcessingBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new MarkCargoDrySettlementPayoutProcessingBffRequest
        {
            ProcessedByUserId = request.ProcessedByUserId,
            ExternalReference = request.ExternalReference,
            Note              = request.Note,
        };

        var result = await _remote.MarkSettlementPayoutProcessingAsync(
            request.SettlementId, remoteRequest, ct);

        return new MarkCargoDrySettlementPayoutProcessingBffCommandResponse
        {
            Settlement   = result.Settlement,
            PayoutResult = result.PayoutResult,
        };
    }
}
