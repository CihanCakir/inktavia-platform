namespace Aizen.Modules.Payment.Abstraction.Request;

/// <summary>BE-P11 §9.2 — provider request to purchase an OFFER_BOOST_7D for one of their offers (non-marketplace checkout).</summary>
public sealed class PurchaseOfferBoostRequest
{
    public long   OfferId      { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
}
