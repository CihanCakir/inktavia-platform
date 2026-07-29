namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// BE-P11 §9.1 — the kind of entitlement a premium product grants. Extensible; MVP ships only the offer boost.
/// </summary>
public enum PremiumEntitlementType
{
    OfferBoost = 1,   // visibility/ranking/showcase for a single offer (OFFER_BOOST_7D) — §19.4: never touches commission
}

/// <summary>BE-P11 §9.1 — product / price catalogue status.</summary>
public enum PremiumProductStatus
{
    Active   = 1,
    Inactive = 2,
}

/// <summary>BE-P11 §9.2 — the premium purchase lifecycle (payment-driven).</summary>
public enum PremiumPurchaseStatus
{
    Pending  = 1,   // checkout initiated, awaiting the success webhook — NO entitlement yet
    Paid     = 2,   // webhook confirmed → entitlement created + Active
    Failed   = 3,   // failure webhook → no entitlement
    Refunded = 4,   // boost refunded → entitlement Revoked (non-marketplace refund, no ProviderNegativeBalance)
}

/// <summary>BE-P11 §9.2 — the entitlement lifecycle (guarded transitions).</summary>
public enum PremiumEntitlementStatus
{
    Inactive = 1,   // created but not yet active (transient — never before the success webhook)
    Active   = 2,   // live between StartsAt and ExpiresAt
    Revoked  = 3,   // refund/admin revoked before expiry
    Expired  = 4,   // past ExpiresAt (ExpirePremiumEntitlementsJob)
}
