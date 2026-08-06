using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDrySettlementPayment;

[DocumentationInfo("Prepare CargoDry settlement payment BFF command handler",
    "Forwards the prepare-payment request to the CargoDry commercial module. " +
    "The module creates a PayoutRecord in the Payment module (via in-process ICargoDrySettlementPayoutService) " +
    "and transitions the settlement from ReadyForSettlement → Scheduled. " +
    "Idempotent — safe to retry; returns existing payout record if already prepared. " +
    "Does NOT create PaymentTransaction, Invoice, or trigger any Iyzico call. " +
    "Phase 4B (July 2026).")]
public sealed class PrepareCargoDrySettlementPaymentBffCommandHandler
    : AizenCommandHandler<PrepareCargoDrySettlementPaymentBffCommand,
                          PrepareCargoDrySettlementPaymentBffCommandResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public PrepareCargoDrySettlementPaymentBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<PrepareCargoDrySettlementPaymentBffCommandResponse> Handle(
        PrepareCargoDrySettlementPaymentBffCommand request, CancellationToken ct)
    {
        var remoteRequest = new PrepareCargoDrySettlementPaymentBffRequest
        {
            PreparedByUserId = request.PreparedByUserId,
            PreparationNote  = request.PreparationNote,
        };

        var result = await _remote.PrepareSettlementPaymentAsync(
            request.SettlementId, remoteRequest, ct);

        return new PrepareCargoDrySettlementPaymentBffCommandResponse
        {
            Settlement     = result.Settlement,
            PayoutRecordId = result.PayoutRecordId,
            AlreadyExisted = result.AlreadyExisted,
        };
    }
}
