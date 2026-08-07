using Aizen.Core.Domain;
using Aizen.Modules.Payment.Domain.Money;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// BE-S4b (§20.8) — immutable, insert-only travel/mobilization snapshot; a child of the single aggregate
/// <see cref="PaymentEconomicsSnapshotEntity"/> (FK, OnDelete Restrict) — it fills the reserved <c>TravelPricingSnapshot</c>
/// slot. Written once by <see cref="PaymentEconomicsSnapshotEntity.CreateFromLines"/> for each Travel line that carried a
/// travel detail; never mutated (private setters, no methods). Captures how the travel charge was derived (method, origin/
/// destination city codes + <b>denormalized labels</b>, provider-declared distance, per-km rate, KILOMETER unit) plus the
/// <b>resolved travel amount</b> (= the Travel line total). Self-contained — no re-lookup after acceptance (§20.15).
///
/// <para><b>Descriptive:</b> the Travel line's money is already an Exempt pass-through in the 8-equality; this snapshot enters
/// no sum and none of the invariants. The <b>tamper guard</b> asserts the resolved amount matches the derivation
/// (PerKm: <c>Round(DistanceKm × PerKmRate)</c>; Flat: a positive fee with no km/rate) so a persisted row can never silently
/// disagree with its own numbers. <see cref="Method"/> is the raw SR <c>TravelPricingMethod</c> int (held opaquely).</para>
/// </summary>
[DocumentationInfo("Travel pricing snapshot entity",
    "Immutable travel/mobilization derivation child of PaymentEconomicsSnapshot (§20.8). Descriptive metadata; the resolved " +
    "amount is tamper-checked against the derivation. Not part of the money math or the 8 equalities.")]
[NoMessagebusSync] // domain-authored immutable financial snapshot — never generically writable
public sealed class TravelPricingSnapshotEntity : AizenEntityWithAudit
{
    /// <summary>The raw SR TravelPricingMethod int: 1 = FlatMobilization, 2 = PerKm.</summary>
    public const int FlatMobilization = 1;
    public const int PerKm            = 2;

    /// <summary>The R3-seeded distance unit a PerKm travel line must use.</summary>
    public const string KilometerUnitCode = "KILOMETER";

    public long     EconomicsSnapshotId  { get; private set; }
    /// <summary>The Travel line this derivation belongs to (= offer item id, as the line snapshot's LineRef).</summary>
    public string   LineRef              { get; private set; } = default!;
    public int      Method               { get; private set; }
    public string?  OriginCityCode       { get; private set; }
    public string?  OriginCityLabel      { get; private set; }
    public string?  DestinationCityCode  { get; private set; }
    public string?  DestinationCityLabel { get; private set; }
    public decimal? DistanceKm           { get; private set; }
    public decimal? PerKmRate            { get; private set; }
    public string?  UnitCode             { get; private set; }
    /// <summary>The Travel line total (the derived travel charge). Descriptive — not in any sum or invariant.</summary>
    public decimal  ResolvedTravelAmount { get; private set; }

    private TravelPricingSnapshotEntity() { }

    /// <summary>
    /// The only construction path. Normalises codes/labels, then tamper-checks the resolved amount against the derivation:
    /// PerKm requires a positive distance/rate, the KILOMETER unit, and <c>ResolvedTravelAmount == Round(DistanceKm × PerKmRate)</c>;
    /// FlatMobilization requires no km/rate and a positive fee. Throws <see cref="PaymentEconomicsInvariantException"/> on any mismatch.
    /// </summary>
    public static TravelPricingSnapshotEntity Create(
        string lineRef, int method,
        string? originCityCode, string? originCityLabel,
        string? destinationCityCode, string? destinationCityLabel,
        decimal? distanceKm, decimal? perKmRate, string? unitCode,
        decimal resolvedTravelAmount)
    {
        if (string.IsNullOrWhiteSpace(lineRef))
            throw new PaymentEconomicsInvariantException("TravelPricingSnapshot requires a LineRef.");

        var normalizedUnit = string.IsNullOrWhiteSpace(unitCode) ? null : unitCode.Trim().ToUpperInvariant();

        switch (method)
        {
            case PerKm:
                if (!distanceKm.HasValue || distanceKm.Value <= 0m)
                    throw new PaymentEconomicsInvariantException("TravelPricingSnapshot PerKm requires DistanceKm > 0.");
                if (!perKmRate.HasValue || perKmRate.Value <= 0m)
                    throw new PaymentEconomicsInvariantException("TravelPricingSnapshot PerKm requires PerKmRate > 0.");
                if (normalizedUnit != KilometerUnitCode)
                    throw new PaymentEconomicsInvariantException("TravelPricingSnapshot PerKm requires the KILOMETER unit.");
                if (resolvedTravelAmount != MoneyMath.Round(distanceKm.Value * perKmRate.Value))
                    throw new PaymentEconomicsInvariantException(
                        "TravelPricingSnapshot ResolvedTravelAmount == Round(DistanceKm * PerKmRate)");
                break;

            case FlatMobilization:
                if (distanceKm.HasValue || perKmRate.HasValue)
                    throw new PaymentEconomicsInvariantException("TravelPricingSnapshot FlatMobilization must not carry a distance/rate.");
                if (normalizedUnit is not null)
                    throw new PaymentEconomicsInvariantException("TravelPricingSnapshot FlatMobilization must not carry a unit.");
                if (resolvedTravelAmount <= 0m)
                    throw new PaymentEconomicsInvariantException("TravelPricingSnapshot FlatMobilization requires a positive fee.");
                break;

            default:
                throw new PaymentEconomicsInvariantException($"TravelPricingSnapshot has an unknown Method '{method}'.");
        }

        return new TravelPricingSnapshotEntity
        {
            LineRef              = lineRef,
            Method               = method,
            OriginCityCode       = Normalize(originCityCode),
            OriginCityLabel      = Trim(originCityLabel),
            DestinationCityCode  = Normalize(destinationCityCode),
            DestinationCityLabel = Trim(destinationCityLabel),
            DistanceKm           = distanceKm,
            PerKmRate            = perKmRate,
            UnitCode             = normalizedUnit,
            ResolvedTravelAmount = resolvedTravelAmount,
            IsActive             = true,
        };
    }

    private static string? Normalize(string? code) => string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
    private static string? Trim(string? label) => string.IsNullOrWhiteSpace(label) ? null : label.Trim();
}
