using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

/// <summary>
/// Service interface for the CargoDry monthly settlement automation.
/// Encapsulates both preview (DryRun) and live execution logic.
///
/// Hard invariants (Phase 6, July 2026):
///   - AutoCompletePayout is NEVER true. Payout lifecycle is always manual.
///   - The live run dispatches existing CQRS command handlers; never bypasses them.
///   - The live run NEVER marks any settlement as Settled.
///   - The live run NEVER calls Approve/Processing/Complete/Fail payout commands.
///   - DryRun mode performs NO mutations whatsoever.
/// </summary>
public interface ICargoDryMonthlySettlementAutomationService
{
    /// <summary>
    /// Performs a read-only analysis (DryRun) of all settlements for the given target year-month.
    /// Returns a preview of what actions would be taken in Live mode.
    /// Never mutates any data.
    /// </summary>
    Task<CargoDrySettlementAutomationPreviewDto> GetPreviewAsync(
        int                targetYearMonth,
        bool               autoPreparePayment,
        bool               autoPrepareInvoice,
        CancellationToken  ct);

    /// <summary>
    /// Executes the automation for the given target year-month in Live mode.
    /// Creates and persists a <see cref="CargoDrySettlementAutomationRunDto"/> capturing
    /// the full result. Dispatches existing command handlers for each eligible settlement.
    ///
    /// Returns the completed run DTO (with items populated) after saving.
    /// </summary>
    Task<CargoDrySettlementAutomationRunDto> RunAsync(
        string             runCode,
        int                targetYearMonth,
        CargoDrySettlementAutomationMode mode,
        bool               autoPreparePayment,
        bool               autoPrepareInvoice,
        long               triggeredByUserId,
        string?            note,
        CancellationToken  ct);
}
