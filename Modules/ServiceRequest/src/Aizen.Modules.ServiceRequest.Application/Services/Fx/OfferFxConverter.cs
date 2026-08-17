using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Services.Fx;

/// <summary>BE-S3 — a resolved point-in-time rate for one source currency (1 source = <see cref="Rate"/> settlement units).</summary>
public sealed record OfferFxRatePoint(decimal Rate, DateTime RateDate);

/// <summary>BE-S3 — one FX snapshot row the offer should persist (produced by <see cref="OfferFxConverter"/>).</summary>
public sealed record OfferFxConversionRow(string SourceCurrencyCode, string SettlementCurrencyCode, decimal Rate, DateTime RateDate);

/// <summary>
/// BE-S3a — the <b>pure</b> foreign-line → settlement-currency (TRY) conversion. Given the offer and the rates resolved at
/// submit (one per distinct non-TRY source currency), it rewrites each foreign priced line's <c>UnitPrice</c> to TRY
/// (preserving the source figure in <c>SourceUnitPrice</c>) and reports the FX rows to snapshot. The economics then runs on
/// the TRY prices — the 8-equality is untouched. No I/O, no remote calls — the rates are handed in already resolved, so this
/// is unit-testable and deterministic. A TRY-only offer yields no changes and no rows (the resolver never even calls here).
///
/// <para>Rounding matches S1's money convention exactly: <c>round(source × rate) = Math.Round(x, 2, AwayFromZero)</c>. The
/// source-of-truth for each line is <c>SourceUnitPrice ?? UnitPrice</c>, so a re-submit converts from the original foreign
/// figure and never double-converts an already-TRY value.</para>
/// </summary>
public static class OfferFxConverter
{
    /// <summary>Money rounding — identical to <c>OfferCalculationService.Round</c> (§13.6 / S1).</summary>
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Converts every foreign priced line to the settlement currency and returns one row per distinct source currency
    /// actually converted. Throws if a needed rate is absent from <paramref name="ratesBySourceCurrency"/> (the resolver
    /// guarantees completeness; this is a defensive invariant, never a silent 1.0 rate).
    /// </summary>
    public static IReadOnlyList<OfferFxConversionRow> Convert(
        ServiceRequestOfferEntity offer,
        IReadOnlyDictionary<string, OfferFxRatePoint> ratesBySourceCurrency)
    {
        var settlement = OfferFxConstants.SettlementCurrency;
        var used = new Dictionary<string, OfferFxRatePoint>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in offer.Items)
        {
            if (line.ItemType == ServiceRequestOfferItemType.Discount)
                continue;   // discount lines carry an amount/percent, not a per-unit price to convert

            var source = (line.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
            if (source.Length == 0 || OfferFxConstants.IsSettlement(source))
                continue;   // settlement-native line — no conversion

            if (!ratesBySourceCurrency.TryGetValue(source, out var point))
                throw new InvalidOperationException($"No resolved FX rate for source currency '{source}'.");

            var sourceUnitPrice    = line.SourceUnitPrice ?? line.UnitPrice;   // convert from the original foreign figure
            var convertedUnitPrice = Round(sourceUnitPrice * point.Rate);
            line.ApplyFxConversion(sourceUnitPrice, convertedUnitPrice);

            used[source] = point;
        }

        return used
            .Select(kv => new OfferFxConversionRow(kv.Key, settlement, kv.Value.Rate, kv.Value.RateDate))
            .ToList();
    }
}
