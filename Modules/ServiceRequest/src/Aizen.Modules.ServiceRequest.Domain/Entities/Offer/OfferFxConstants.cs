namespace Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

/// <summary>
/// BE-S3 — the platform's single <b>settlement currency</b>. The marketplace settles in TRY (§20.7 "TL kabulde sabit"):
/// a provider may quote a line in a foreign source currency, but at offer-submit the line is converted to TRY and the
/// 8-equality economics (S1/S6/S7/S8) always runs in this one currency. Kept as ONE constant so it is never scattered as
/// a literal across the FX conversion / snapshot code.
/// </summary>
public static class OfferFxConstants
{
    /// <summary>The currency every offer settles in. The economics math never sees any other currency.</summary>
    public const string SettlementCurrency = "TRY";

    /// <summary>True when a line's source currency is already the settlement currency (no conversion, no resolve call).</summary>
    public static bool IsSettlement(string? currencyCode)
        => string.Equals((currencyCode ?? string.Empty).Trim(), SettlementCurrency, System.StringComparison.OrdinalIgnoreCase);
}
