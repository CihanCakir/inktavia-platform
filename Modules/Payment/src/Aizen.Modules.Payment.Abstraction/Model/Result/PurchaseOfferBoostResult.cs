namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>BE-P11 §9.2 — the outcome of initiating an OFFER_BOOST_7D purchase (Pending; entitlement not active yet).</summary>
public sealed record PurchaseOfferBoostResult(
    long    PremiumPurchaseId,
    string  PurchaseCode,
    long    PaymentTransactionId,
    string  GatewayReference,
    decimal UnitPrice,
    string  CurrencyCode,
    int     DurationDays,
    string? CheckoutFormContent,
    string? RedirectUrl
);
