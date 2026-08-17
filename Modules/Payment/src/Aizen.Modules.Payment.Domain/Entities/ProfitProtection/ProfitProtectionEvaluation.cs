using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// Engine output (§19.11). Carries the final decision, the applied advantages (after any adjustment), the three
/// computed contributions + their required minimums, the derived expected expenses, and the safe-max discount —
/// enough for the caller (P8) to show the final amounts before checkout and to audit via the evaluation log.
/// </summary>
public sealed record ProfitProtectionEvaluation(
    ProfitProtectionDecisionState State,
    long?   PolicyId,
    string? AdjustmentReason,

    // ── Applied advantages (final, pre-checkout) ────────────────────────────────
    decimal AppliedPlatformFundedDiscount,
    decimal AppliedCommissionBenefit,

    // ── Contributions (computed at the applied advantages) ──────────────────────
    decimal CustomerSideContributionExpected,
    decimal ProviderSideContributionExpected,
    decimal TotalTransactionContributionExpected,

    // ── Required minimums (§19.3) ───────────────────────────────────────────────
    decimal RequiredCustomerSideContribution,
    decimal RequiredProviderSideContribution,
    decimal RequiredTransactionContribution,

    // ── Derived expected variable expenses ──────────────────────────────────────
    decimal ExpectedPaymentProcessingExpense,
    decimal ExpectedRefundRiskReserve,
    decimal ExpectedOtherVariableExpenses,

    // ── Safe-max platform-funded discount (§19.10) ──────────────────────────────
    decimal MaximumSafePlatformFundedDiscount)
{
    /// <summary>True for Approved / ApprovedWithAdjustment — the transaction may proceed to checkout.</summary>
    public bool CanProceed => State is ProfitProtectionDecisionState.Approved
                                    or ProfitProtectionDecisionState.ApprovedWithAdjustment;
}
