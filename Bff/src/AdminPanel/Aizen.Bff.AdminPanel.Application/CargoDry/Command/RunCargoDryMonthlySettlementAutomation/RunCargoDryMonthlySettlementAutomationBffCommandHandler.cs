using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
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
    private readonly ICargoDryRemoteCall _remote;

    public RunCargoDryMonthlySettlementAutomationBffCommandHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<RunCargoDryMonthlySettlementAutomationBffCommandResponse?> Handle(
        RunCargoDryMonthlySettlementAutomationBffCommand request, CancellationToken ct)
    {
        var run = await _remote.RunSettlementAutomationAsync(
            new RunSettlementAutomationBffRequest
            {
                TargetYearMonth    = request.TargetYearMonth,
                Mode               = request.Mode,
                AutoPreparePayment = request.AutoPreparePayment,
                AutoPrepareInvoice = request.AutoPrepareInvoice,
                TriggeredByUserId  = request.TriggeredByUserId,
                Note               = request.Note,
            },
            ct);

        return new RunCargoDryMonthlySettlementAutomationBffCommandResponse { Run = run };
    }
}
