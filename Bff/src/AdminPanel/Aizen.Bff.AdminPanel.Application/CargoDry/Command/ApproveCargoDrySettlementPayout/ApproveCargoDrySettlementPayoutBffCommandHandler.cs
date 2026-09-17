using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
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
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public ApproveCargoDrySettlementPayoutBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<ApproveCargoDrySettlementPayoutBffCommandResponse> Handle(
        ApproveCargoDrySettlementPayoutBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR (same bug class as attribution resolve-financials / automation run):
        // the FE sends no actor id and the body default (0) fails module validation. Stamp the acting admin
        // server-side; the client-supplied value is ignored on purpose.
        await _resolver.ResolveAsync(ct);

        var remoteRequest = new ApproveCargoDrySettlementPayoutBffRequest
        {
            ApprovedByUserId = _holder.UserId ?? 0,
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
