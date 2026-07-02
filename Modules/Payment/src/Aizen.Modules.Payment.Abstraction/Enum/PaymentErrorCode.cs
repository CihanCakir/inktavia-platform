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
