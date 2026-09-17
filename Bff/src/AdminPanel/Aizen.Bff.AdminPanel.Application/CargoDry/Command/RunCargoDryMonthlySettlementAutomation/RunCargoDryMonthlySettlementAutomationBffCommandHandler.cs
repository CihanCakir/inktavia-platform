using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RunCargoDryMonthlySettlementAutomation;

[DocumentationInfo("RunCargoDryMonthlySettlementAutomationBffCommandHandler",
    "Proxies the settlement automation trigger to the CargoDry module. " +
    "Mode=1 (DryRun) by default — no mutations unless Mode=2 (Live). " +
    "AutoCompletePayout is always enforced to false server-side regardless of input. " +
    "Phase 6 (July 2026).")]
public sealed class RunCargoDryMonthlySettlementAutomationBffCommandHandler
    : AizenCommandHandler<RunCargoDryMonthlySettlementAutomationBffCommand,
                          RunCargoDryMonthlySettlementAutomationBffCommandResponse>
{
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public RunCargoDryMonthlySettlementAutomationBffCommandHandler(
        ICargoDryRemoteCall remote, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _remote   = remote;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<RunCargoDryMonthlySettlementAutomationBffCommandResponse?> Handle(
        RunCargoDryMonthlySettlementAutomationBffCommand request, CancellationToken ct)
    {
        // FIX_RESOLVE_FINANCIALS_ACTOR (same class of bug as attribution resolve-financials): the module
        // validates TriggeredByUserId as a real user id, but the FE never sends one and the body default (0)
        // failed validation -> structured 400 "TriggeredByUserId must be a valid user id" (seen live, E2E E2).
        // Stamp the acting admin server-side; the client-supplied value is ignored on purpose.
        await _resolver.ResolveAsync(ct);

        var run = await _remote.RunSettlementAutomationAsync(
            new RunSettlementAutomationBffRequest
            {
                TargetYearMonth    = request.TargetYearMonth,
                Mode               = request.Mode,
                AutoPreparePayment = request.AutoPreparePayment,
                AutoPrepareInvoice = request.AutoPrepareInvoice,
                TriggeredByUserId  = _holder.UserId ?? 0,
                Note               = request.Note,
            },
            ct);

        return new RunCargoDryMonthlySettlementAutomationBffCommandResponse { Run = run };
    }
}
