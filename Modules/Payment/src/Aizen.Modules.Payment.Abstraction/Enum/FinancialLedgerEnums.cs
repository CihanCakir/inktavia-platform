namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// BE-P12 §15/§19.17 — the economic nature of a ledger line. <b>Amount is always stored positive</b>; the nature (and the
/// <c>IsReversal</c> contra flag) carry the sign when a report aggregates. Revenue/Receivable add to the platform's take;
/// Expense/Liability subtract from it; Memo lines are informational (never in the Inktavia P&amp;L).
/// </summary>
public enum LedgerEntryNature
{
    Revenue    = 1,
    Expense    = 2,
    Liability  = 3,   // e.g. platform-fee VAT — collected, owed onward, NOT revenue (§19.17)
    Receivable = 4,   // e.g. provider recovery / platform-advanced refund
    Memo       = 5,   // informational only (provider-funded discount, contributions, gross share)
}

/// <summary>BE-P12 §15 — the immutable source a ledger line is derived from (with <c>SourceRef</c> = that source's id).</summary>
public enum LedgerSourceType
{
    AcceptanceSnapshot = 1,   // PaymentEconomicsSnapshot (P8)
    Refund             = 2,   // RefundAllocation / TransactionRefundRecord (P10)
    Chargeback         = 3,   // ChargebackRecord (P10)
    PremiumPurchase    = 4,   // PremiumPurchase (P11)
    Subscription       = 5,   // provider/participant subscription
}

/// <summary>
/// BE-P12 §15 + §19.17 — every reporting account line, kept <b>separate (no merging)</b>. Discounts are never collapsed into
/// one <c>DiscountAmount</c>; provider-funded customer discount is a MEMO (not an Inktavia expense); platform-funded discount
/// IS a campaign expense; platform-fee VAT is a Liability, not revenue.
/// </summary>
public enum LedgerAccountLine
{
    // ── Revenue (§15) ──────────────────────────────────────────────────────────
    ProviderCommissionRevenue     = 1,
    CustomerPlatformFeeNetRevenue = 2,
    SubscriptionRevenue           = 3,
    ProviderPlanRevenue           = 4,
    CustomerPlanRevenue           = 5,
    ProviderAddOnRevenue          = 6,
    PremiumProductRevenue         = 7,

    // ── Liability (NOT revenue) ────────────────────────────────────────────────
    CustomerPlatformFeeVatLiability = 10,

    // ── Expense (§15) ──────────────────────────────────────────────────────────
    PaymentProcessingExpense              = 20,
    GatewayOtherExpense                   = 21,
    RefundProcessingExpense               = 22,
    ChargebackExpense                     = 23,
    PlatformFundedCustomerDiscountExpense = 24,   // §19.17 — Inktavia campaign cost (IS an expense)

    // ── Discount / benefit (kept SEPARATE — §19.17) ────────────────────────────
    ProviderFundedCustomerDiscount = 30,   // MEMO — NOT an Inktavia expense (funded by the provider)
    ProviderCommissionBenefitCost  = 31,

    // ── Recovery / advance ─────────────────────────────────────────────────────
    ProviderRecoveryReceivable   = 40,
    PlatformAdvancedRefundAmount  = 41,

    // ── Contribution / settlement (computed / memo — §13.10) ───────────────────
    PlatformGrossShare           = 50,
    PlatformSettlementNetAmount  = 51,
    CustomerSideContribution     = 52,
    ProviderSideContribution     = 53,
    TotalTransactionContribution = 54,
    NetMarketplaceContribution   = 55,

    // ── Expected / actual expense (§19.17) ─────────────────────────────────────
    ExpectedPaymentProcessingExpense = 60,
    ActualPaymentProcessingExpense   = 61,
    RefundRiskReserve                = 62,
    ActualRefundExpense              = 63,
}
