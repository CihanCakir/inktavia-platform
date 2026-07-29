namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// Engine input value object (§19.2/§3, §19.14) — the computed economics + requested advantages + revenue
/// allocations. Assembled by P8 at acceptance; synthetic in tests (P6/P7 fields default to 0). The engine derives
/// the expected variable expenses itself from the policy — they are NOT passed in here.
/// </summary>
public sealed record ProfitProtectionContext(
    string  CurrencyCode,
    // ── Core economics ──────────────────────────────────────────────────────────
    decimal ServiceAmount,
    decimal CustomerPayableServiceAmount,
    decimal CustomerTotalAmount,
    decimal ProviderNetAmount,
    // ── Revenue (net) ───────────────────────────────────────────────────────────
    decimal ProviderCommissionNetRevenue,
    decimal CustomerPlatformFeeNetRevenue,
    decimal CustomerPlanRevenueAllocation      = 0m,
    decimal CustomerPremiumRevenueAllocation   = 0m,
    decimal ProviderPlanRevenueAllocation      = 0m,
    decimal ProviderAddOnRevenueAllocation     = 0m,
    // ── Requested advantages (P6/P7 — 0 in P5 tests) ────────────────────────────
    decimal RequestedPlatformFundedCustomerDiscount = 0m,
    decimal ProviderFundedCustomerDiscount          = 0m,
    decimal ProviderCommissionBenefitCost           = 0m,
    // ── Budget (P6 — 0 if none) ─────────────────────────────────────────────────
    decimal CustomerBenefitBudgetRemaining          = 0m);
