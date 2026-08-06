using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RunCargoDryMonthlySettlementAutomation;

/// <summary>
/// BFF command that triggers the CargoDry monthly settlement automation run.
/// Mode defaults to DryRun (1). AutoCompletePayout is always false — enforced server-side.
/// Phase 6 (July 2026).
/// </summary>
public sealed class RunCargoDryMonthlySettlementAutomationBffCommand
    : AizenCommand<RunCargoDryMonthlySettlementAutomationBffCommandResponse>
{
    public int    TargetYearMonth    { get; init; }
    public int    Mode               { get; init; } = 1; // DryRun default
    public bool   AutoPreparePayment { get; init; } = false;
    public bool   AutoPrepareInvoice { get; init; } = false;
    public long   TriggeredByUserId  { get; init; }
    public string? Note              { get; init; }
}

public sealed class RunCargoDryMonthlySettlementAutomationBffCommandResponse
{
    public CargoDrySettlementAutomationRunBffDto Run { get; init; } = default!;
}
