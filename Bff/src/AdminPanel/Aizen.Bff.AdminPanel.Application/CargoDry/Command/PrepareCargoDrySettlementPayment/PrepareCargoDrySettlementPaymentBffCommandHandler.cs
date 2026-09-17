using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.PrepareCargoDrySettlementPayment;

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
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public PrepareCargoDrySettlementPaymentBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<PrepareCargoDrySettlementPaymentBffCommandResponse> Handle(
        PrepareCargoDrySettlementPaymentBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR (same bug class as attribution resolve-financials / automation run):
        // the FE sends no actor id and the body default (0) fails module validation. Stamp the acting admin
        // server-side; the client-supplied value is ignored on purpose.
        await _resolver.ResolveAsync(ct);

        var remoteRequest = new PrepareCargoDrySettlementPaymentBffRequest
        {
            PreparedByUserId = _holder.UserId ?? 0,
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
