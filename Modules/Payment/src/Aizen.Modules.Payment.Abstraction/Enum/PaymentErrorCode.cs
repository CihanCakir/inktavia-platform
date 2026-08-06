namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Payment module error codes — mapped to Resource/aizen_error_messages.json.
/// Range: 5000–5999.
/// </summary>
public enum PaymentErrorCode
{
    TransactionNotFound                  = 5001,
    TransactionInvalidState              = 5002,
    RefundAmountInvalid                  = 5003,
    RefundAmountExceedsMaximum           = 5004,
    CommissionRuleNotFound               = 5005,
    PayoutRecordNotFound                 = 5006,
    GatewayInitiationFailed              = 5007,
    SubMerchantRegistrationFailed        = 5008,
    IdempotencyConflict                  = 5009,
    TransactionAlreadyCaptured           = 5010,
    TransactionAlreadyCancelled          = 5011,
    RefundRecordNotFound                 = 5012,
    EscrowReleaseInvalidState            = 5013,
    ProviderPaymentProfileNotFound       = 5014,
    ProviderPaymentProfileAlreadyExists  = 5015,
    TransactionAlreadyActive             = 5016,
    RefundReversalInvalidState           = 5017,
    CommissionRateExceedsThreshold       = 5018,
    NetPayoutBelowMinimumThreshold       = 5019,
    RefundRecordInvalidState             = 5020,
    CommissionDiscountExceedsGross       = 5021,
    CommissionRateInvalid                = 5022,
    GatewayProviderNotFound              = 5023,

    // ── Subscription ──────────────────────────────────────────────────────────
    SubscriptionAlreadyActive            = 5024,
    SubscriptionNotFound                 = 5025,
    ProviderPlanNotFound                 = 5026,
    ParticipantPlanNotFound              = 5027,

    CommissionRuleNotInactive            = 5037,   // Reactivate attempted on non-Inactive rule

    // ── Economics snapshot (BE-P1) ────────────────────────────────────────────
    PaymentEconomicsInvariantViolation   = 5038,   // A §4 zero-tolerance invariant failed in Create()
    EconomicsSnapshotAlreadyLinked       = 5039,   // LinkEconomicsSnapshot called on an already-linked transaction

    // ── Commission rule engine (BE-P2) ────────────────────────────────────────
    CommissionRuleConflict               = 5040,   // ≥2 rules tie on (specificity, priority) at resolve, or an overlapping active rule at create/update

    // ── Platform fee rule engine (BE-P3) ──────────────────────────────────────
    PlatformFeeRuleConflict              = 5041,   // ≥2 rules tie on (specificity, priority) at resolve, or an overlapping active rule at create/update
    PlatformFeeRuleNotFound              = 5042,   // No rule for the given id / no rule (incl. Global) matched the context
    PlatformFeeRuleInvalid               = 5043,   // Model-incoherent rule (e.g. Percentage without Rate, bounds with Min > Max)
    PlatformFeeRuleNotInactive           = 5047,   // Reactivate attempted on a non-Inactive platform fee rule

    // ── Provider plan price versioning (BE-P4) ────────────────────────────────
    ProviderPlanPriceConflict            = 5044,   // >1 active price at an instant, or an overlapping range at create/update
    ProviderPlanPriceGap                 = 5045,   // Create/update would leave a non-contiguous gap between consecutive price ranges
    ProviderPlanPriceNotFound            = 5046,   // No active price resolves for the (plan, currency, period, instant)

    // ── Profit protection engine (BE-P5) ──────────────────────────────────────
    ProfitProtectionPolicyConflict       = 5050,   // >1 active policy for a currency at an instant, or an overlapping range at create/update
    ProfitProtectionPolicyNotFound       = 5051,   // No active policy resolves for the (currency, instant) → ConfigurationError at the engine
    ProfitProtectionPolicyInvalid        = 5052,   // Model-incoherent policy (e.g. negative rate/amount, share outside [0,1])
    ProfitProtectionPolicyNotInactive    = 5053,   // Reactivate attempted on a non-Inactive profit-protection policy

    // ── Customer discount + benefit budget (BE-P6) ────────────────────────────
    CustomerDiscountRuleConflict         = 5060,   // ≥2 rules tie on (specificity, priority) at resolve, or an overlapping active rule at create/update
    CustomerDiscountRuleNotFound         = 5061,   // No rule resolves for the (plan, category, currency, instant)
    CustomerDiscountRuleInvalid          = 5062,   // Model-incoherent rule (missing funding source, bad Shared rates, Percent without Rate, ...)
    CustomerBenefitBudgetNotFound        = 5063,   // No budget for the id / subscription
    CustomerBenefitInsufficientRemaining = 5064,   // Reservation amount exceeds RemainingAmount
    CustomerBenefitConcurrencyConflict   = 5065,   // Optimistic-concurrency clash — another checkout mutated the budget first
    CustomerBenefitReservationNotFound   = 5066,   // No reservation for the id
    CustomerBenefitReservationInvalidState = 5067, // Consume/Release attempted from a non-Reserved state
    CustomerBenefitBudgetPolicyConflict  = 5068,   // >1 active budget policy for a (plan, currency), or overlap at create/update
    CustomerDiscountRuleNotInactive      = 5069,   // Reactivate attempted on a non-Inactive customer discount rule

    // ── Provider commission benefit (BE-P7) ───────────────────────────────────
    ProviderCommissionBenefitRuleConflict         = 5070,   // ≥2 exclusive/non-stackable benefits tie, or an overlapping active rule at create/update
    ProviderCommissionBenefitRuleNotFound         = 5071,   // No rule for the given id
    ProviderCommissionBenefitRuleInvalid          = 5072,   // Incoherent rule (positive surcharge, Exclusive+Stackable, min out of [0,1], negative caps)
    ProviderCommissionBenefitEntitlementNotFound  = 5073,   // No entitlement for the id / provider
    ProviderCommissionBenefitExhausted            = 5074,   // Usage limit or eligible-GMV limit reached
    ProviderCommissionBenefitConcurrencyConflict  = 5075,   // Optimistic-concurrency clash on the entitlement Version
    ProviderCommissionBenefitUsageNotFound        = 5076,   // No usage ledger row for the id
    ProviderCommissionBenefitUsageInvalidState    = 5077,   // Consume/Release from a non-Reserved usage state
    ProviderCommissionBelowFloor                  = 5078,   // Effective rate would fall below the plan/system floor without an admin ProviderOverride
    ProviderCommissionBenefitRuleNotInactive      = 5079,   // Reactivate attempted on a non-Inactive provider commission benefit rule

    // ── SR acceptance economics (BE-P8) ───────────────────────────────────────
    ServiceRequestEconomicsRejected               = 5080,   // Profit-protection gate rejected the acceptance economics — no snapshot/escrow, acceptance blocked
    ServiceRequestEconomicsConfigurationError     = 5081,   // No active profit-protection policy for the currency/instant → ConfigurationError, acceptance blocked
    ServiceRequestEconomicsCommissionUnresolved   = 5082,   // A commissionable line resolved to no commission rule (completeness gap — not even a Global rule matched)

    // ── Provider sub-merchant onboarding + split gate (BE-I1) ──────────────────
    ProviderSubMerchantInvalidTransition          = 5090,   // Illegal onboarding lifecycle transition on ProviderPaymentProfile
    ProviderNotSplitEligible                      = 5091,   // Recipient provider has no split-eligible sub-merchant → P8 blocks acceptance (no escrow/snapshot)
    ProviderSubMerchantOnboardingIncomplete       = 5092,   // Onboarding data not submitted / sub-merchant not created

    // ── iyzico auth-mode + pre-send split-math guard + item-level ops (BE-P9) ──
    IyzicoSplitMathMismatch                       = 5093,   // Pre-send split-math self-verification failed → NO iyzico call is made
    IyzicoItemApproveFailed                       = 5094,   // POST /payment/iyzipos/item/approve failed
    IyzicoItemDisapproveFailed                    = 5095,   // POST /payment/iyzipos/item/disapprove failed
    IyzicoUpdateShareFailed                       = 5096,   // PUT /payment/item (update sub-merchant share) failed
    PaymentAuthModeInvalid                        = 5097,   // Unresolvable / invalid payment auth-mode policy

    // ── Refund allocation + provider negative balance + chargeback (BE-P10) ────
    RefundAllocationMismatch                      = 5100,   // §7.5 total/breakdown zero-tolerance invariant failed
    RefundAllocationPolicyConflict                = 5101,   // >1 active refund-allocation policy for a currency at an instant
    RefundAllocationPolicyInvalid                 = 5102,   // Incoherent policy (bad dates, missing cause rule, negative limit)
    ProviderNegativeBalanceLimitExceeded          = 5103,   // Provider negative balance beyond the limit → block payout / acceptance
    RefundRestoreAlreadyApplied                   = 5104,   // §19.15 benefit/entitlement restore already applied for this refund
    ChargebackAlreadyProcessed                    = 5105,   // Idempotent guard on the gateway chargeback reference

    // ── Premium product / price / purchase / entitlement (BE-P11) ──────────────
    PremiumProductPriceConflict                   = 5110,   // >1 active premium price covers an instant / bad date range
    PremiumProductPriceNotFound                   = 5111,   // No active premium price resolves at the purchase instant
    PremiumPurchaseInvalidState                   = 5112,   // Illegal purchase lifecycle transition (Pending→Paid→Refunded)
    PremiumEntitlementInvalidTransition           = 5113,   // Illegal entitlement transition (Activate/Revoke/Expire guards)
    PremiumDuplicateActiveBoost                   = 5114,   // A Pending/Active boost already exists for (provider, offer)
    PremiumProductNotFound                        = 5115,   // OFFER_BOOST_7D product missing / inactive

    // ── Part commercial terms (BE-S5, §20.9) ──────────────────────────────────
    PartCommercialTermConflict           = 5120,   // ≥2 terms tie on (specificity, priority) at resolve, or an overlapping active version at create/update
    PartCommercialTermNotFound           = 5121,   // No term for the given id
    PartCommercialTermInvalid            = 5122,   // Model-incoherent term (negative money, Σfunded > maxDiscountable, maxCustomerDiscount > maxDiscountable, bad dates)
    PartCommercialTermNotInactive        = 5124,   // Reactivate attempted on a non-Inactive part commercial term

    // ── Line-level profit protection (BE-S9, §20.12) — evaluated BEFORE the §19.2 transaction gates ──
    LineProfitProtectionRejected                  = 5130,   // A line failed the line-level gate (no netting) → acceptance Rejected, no snapshot/escrow
    LineProfitProtectionProviderReceivableBelowFloor = 5131,// A line's ProviderNet < its provider-minimum-receivable floor (part: S5; else policy)
    LineProfitProtectionFundedDiscountExceedsCap  = 5132,   // A line's provider- or platform-funded discount exceeds its allowed cap
    LineProfitProtectionNegativeContribution      = 5133,   // A line's platform contribution < the min (negative-contribution ban) without an in-limit strategic-loss exception
    LineProfitProtectionConfigurationError        = 5134,   // A part line has no resolved S5 allowance (or no policy) → ConfigurationError, acceptance blocked
    // (a line below the commission floor reuses the P7 ProviderCommissionBelowFloor = 5078 contract — no duplicate code)

    // ── Payout admin operations ───────────────────────────────────────────────
    PayoutInvalidStateForHold            = 5035,
    PayoutInvalidStateForApproval        = 5036,

    // ── Invoice ───────────────────────────────────────────────────────────────
    InvoiceNotFound                      = 5028,
    InvoiceInvalidStateTransition        = 5029,
    InvoiceAlreadyIssued                 = 5030,
    InvoiceLinesRequired                 = 5031,
    InvoiceAlreadyCancelled              = 5032,
    InvoiceNumberConcurrencyExceeded     = 5033,
    InvoiceCannotBeArchived              = 5034,
}
