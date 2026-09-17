using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
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
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public CompleteCargoDrySettlementPayoutBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<CompleteCargoDrySettlementPayoutBffCommandResponse> Handle(
        CompleteCargoDrySettlementPayoutBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR (same bug class as attribution resolve-financials / automation run):
        // the FE sends no actor id and the body default (0) fails module validation. Stamp the acting admin
        // server-side; the client-supplied value is ignored on purpose.
        await _resolver.ResolveAsync(ct);

        var remoteRequest = new CompleteCargoDrySettlementPayoutBffRequest
        {
            CompletedByUserId      = _holder.UserId ?? 0,
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
