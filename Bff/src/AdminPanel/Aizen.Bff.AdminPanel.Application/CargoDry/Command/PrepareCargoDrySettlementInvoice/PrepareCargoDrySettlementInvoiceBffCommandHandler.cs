using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.PrepareCargoDrySettlementInvoice;

[DocumentationInfo("Prepare CargoDry settlement invoice BFF command handler",
    "Forwards the prepare-invoice request to the CargoDry commercial module. " +
    "The module creates a Draft ProviderSettlementStatement invoice in the Payment module " +
    "(via in-process ICargoDrySettlementInvoiceService). " +
    "Settlement status remains Scheduled after preparation (Option B lifecycle). " +
    "Idempotent — safe to retry; returns existing invoice if already prepared. " +
    "Does NOT issue the invoice, does NOT create PaymentTransaction, does NOT call Iyzico. " +
    "Does NOT mark settlement as Settled. " +
    "Phase 4C (July 2026).")]
public sealed class PrepareCargoDrySettlementInvoiceBffCommandHandler
    : AizenCommandHandler<PrepareCargoDrySettlementInvoiceBffCommand,
                          PrepareCargoDrySettlementInvoiceBffCommandResponse>
{
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public PrepareCargoDrySettlementInvoiceBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<PrepareCargoDrySettlementInvoiceBffCommandResponse> Handle(
        PrepareCargoDrySettlementInvoiceBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR (same bug class as attribution resolve-financials / automation run):
        // the FE sends no actor id and the body default (0) fails module validation. Stamp the acting admin
        // server-side; the client-supplied value is ignored on purpose.
        await _resolver.ResolveAsync(ct);

        var remoteRequest = new PrepareCargoDrySettlementInvoiceBffRequest
        {
            PreparedByUserId = _holder.UserId ?? 0,
            PreparationNote  = request.PreparationNote,
        };

        var result = await _remote.PrepareSettlementInvoiceAsync(
            request.SettlementId, remoteRequest, ct);

        return new PrepareCargoDrySettlementInvoiceBffCommandResponse
        {
            Settlement     = result.Settlement,
            InvoiceId      = result.InvoiceId,
            AlreadyExisted = result.AlreadyExisted,
        };
    }
}
