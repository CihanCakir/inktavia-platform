using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.MarkCargoDrySettlementPayoutProcessing;

[DocumentationInfo("Mark CargoDry settlement payout processing BFF command handler",
    "Forwards the mark-payout-processing request to the CargoDry commercial module. " +
    "The module transitions the PayoutRecord to Processing status in the Payment module. " +
    "Optional step between Approve and Complete. Settlement status remains Scheduled. " +
    "No Iyzico call. No bank transfer. Phase 4D (July 2026).")]
public sealed class MarkCargoDrySettlementPayoutProcessingBffCommandHandler
    : AizenCommandHandler<MarkCargoDrySettlementPayoutProcessingBffCommand,
                          MarkCargoDrySettlementPayoutProcessingBffCommandResponse>
{
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public MarkCargoDrySettlementPayoutProcessingBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<MarkCargoDrySettlementPayoutProcessingBffCommandResponse> Handle(
        MarkCargoDrySettlementPayoutProcessingBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR (same bug class as attribution resolve-financials / automation run):
        // the FE sends no actor id and the body default (0) fails module validation. Stamp the acting admin
        // server-side; the client-supplied value is ignored on purpose.
        await _resolver.ResolveAsync(ct);

        var remoteRequest = new MarkCargoDrySettlementPayoutProcessingBffRequest
        {
            ProcessedByUserId = _holder.UserId ?? 0,
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
