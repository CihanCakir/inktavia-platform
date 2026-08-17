using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.RunCargoDryMonthlySettlementAutomation;

[DocumentationInfo("Run CargoDry monthly settlement automation command handler",
    "Builds a unique RunCode, delegates execution to ICargoDryMonthlySettlementAutomationService, " +
    "and maps the resulting entity to a DTO. " +
    "AutoCompletePayout is always false — enforced by the service. " +
    "Phase 6 (July 2026).")]
public sealed class RunCargoDryMonthlySettlementAutomationCommandHandler
    : AizenCommandHandler<RunCargoDryMonthlySettlementAutomationCommand, RunCargoDryMonthlySettlementAutomationResponse>
{
    private readonly ICargoDryMonthlySettlementAutomationService _automationService;
    private readonly ICargoDrySettlementAutomationRunRepository  _runs;

    public RunCargoDryMonthlySettlementAutomationCommandHandler(
        ICargoDryMonthlySettlementAutomationService automationService,
        ICargoDrySettlementAutomationRunRepository  runs)
    {
        _automationService = automationService;
        _runs              = runs;
    }

    public override async Task<RunCargoDryMonthlySettlementAutomationResponse> Handle(
        RunCargoDryMonthlySettlementAutomationCommand request, CancellationToken ct)
    {
        // Build unique run code: SAR-{YYYYMM}-{seq:D4}
        var seq     = await _runs.GetNextRunSequenceAsync(request.TargetYearMonth, ct);
        var runCode = $"SAR-{request.TargetYearMonth}-{seq:D4}";

        var runDto = await _automationService.RunAsync(
            runCode:            runCode,
            targetYearMonth:    request.TargetYearMonth,
            mode:               request.Mode,
            autoPreparePayment: request.AutoPreparePayment,
            autoPrepareInvoice: request.AutoPrepareInvoice,
            triggeredByUserId:  request.TriggeredByUserId,
            note:               request.Note,
            ct:                 ct);

        return new RunCargoDryMonthlySettlementAutomationResponse
        {
            Run = runDto,
        };
    }
}
