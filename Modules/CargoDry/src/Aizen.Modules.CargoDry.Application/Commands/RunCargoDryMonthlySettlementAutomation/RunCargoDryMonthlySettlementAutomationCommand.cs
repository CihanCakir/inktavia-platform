using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.RunCargoDryMonthlySettlementAutomation;

/// <summary>
/// Executes the CargoDry monthly settlement automation for a given target year-month.
///
/// In DryRun mode: scans eligible settlements and records a preview run with no mutations.
/// In Live mode:  dispatches existing command handlers for each eligible settlement.
///
/// Hard invariants (Phase 6, July 2026):
///   - AutoCompletePayout is ALWAYS false; it cannot be set by the caller.
///   - AutoPreparePayment defaults to false; Live payout completion is always manual.
///   - AutoPrepareInvoice defaults to false; requires AutoPreparePayment = true to have any effect.
///   - The handler never marks any settlement as Settled.
///   - The handler never calls Approve/Processing/Complete/Fail payout commands.
/// </summary>
[DocumentationInfo("Run CargoDry monthly settlement automation command",
    "Triggers the monthly settlement automation for the given target year-month. " +
    "DryRun (default) produces a preview run with no mutations. " +
    "Live mode dispatches existing command handlers per eligible settlement. " +
    "AutoCompletePayout is always false. Phase 6 (July 2026).")]
public sealed class RunCargoDryMonthlySettlementAutomationCommand
    : AizenCommand<RunCargoDryMonthlySettlementAutomationResponse>
{
    /// <summary>
    /// Target calendar month in YYYYMM format (e.g. 202606 for June 2026).
    /// Settlements with PeriodStartUtc in this month are in scope.
    /// </summary>
    public int TargetYearMonth { get; init; }

    /// <summary>
    /// Execution mode. DryRun = preview only (default); Live = apply mutations.
    /// </summary>
    public CargoDrySettlementAutomationMode Mode { get; init; } = CargoDrySettlementAutomationMode.DryRun;

    /// <summary>
    /// When true and Mode = Live, the automation will call PrepareCargoDrySettlementPayment
    /// for settlements that become ReadyForSettlement. Default: false.
    /// </summary>
    public bool AutoPreparePayment { get; init; } = false;

    /// <summary>
    /// When true and Mode = Live and AutoPreparePayment = true, the automation will call
    /// PrepareCargoDrySettlementInvoice after payment preparation. Default: false.
    /// </summary>
    public bool AutoPrepareInvoice { get; init; } = false;

    /// <summary>Admin user triggering the run. Required for audit trail.</summary>
    public long TriggeredByUserId { get; init; }

    /// <summary>Optional admin note to attach to the run record.</summary>
    public string? Note { get; init; }
}

public sealed class RunCargoDryMonthlySettlementAutomationResponse
{
    public CargoDrySettlementAutomationRunDto Run { get; init; } = default!;
}
