namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Execution mode for a CargoDry monthly settlement automation run.
/// Phase 6 (July 2026).
/// </summary>
public enum CargoDrySettlementAutomationMode
{
    /// <summary>
    /// Preview only. Scans eligible settlements and reports what actions would be taken,
    /// but performs no mutations. Safe to call at any time.
    /// This is the default and recommended mode.
    /// </summary>
    DryRun = 1,

    /// <summary>
    /// Live execution. Applies automated transitions to eligible settlements
    /// by dispatching existing CQRS command handlers.
    /// Requires explicit admin confirmation via RunCargoDryMonthlySettlementAutomationCommand.
    /// </summary>
    Live = 2,
}
