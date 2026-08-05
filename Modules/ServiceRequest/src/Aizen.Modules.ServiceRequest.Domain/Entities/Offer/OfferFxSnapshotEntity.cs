using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

/// <summary>
/// BE-S3b — the point-in-time exchange-rate snapshot captured on an offer at <b>submit</b>, one row per distinct non-TRY
/// source currency used by the offer's lines. Records the R1 rate that converted the foreign line prices to the settlement
/// currency (TRY) so the conversion is auditable and, once the offer is accepted, <b>frozen</b> — no code path re-resolves
/// FX after acceptance (§20.7 "TL kabulde sabit"). A re-submit before acceptance re-resolves (rows are replaced); after
/// acceptance the offer can no longer transition to Draft, so these rows never change.
///
/// <para>UTC-safe: <see cref="RateDate"/> and <see cref="ResolvedAtUtc"/> are stored as UTC (timestamptz). Validating
/// factory (<see cref="Create"/>); no free mutators — every property has a <c>private set</c>.</para>
/// </summary>
[DocumentationInfo("Offer FX snapshot entity",
    "Immutable-once-accepted offer-level exchange-rate snapshot: one row per non-TRY source currency, capturing the R1 " +
    "rate + rate date used to convert the offer's foreign lines to TRY at submit.")]
public sealed class OfferFxSnapshotEntity : AizenEntityWithAudit
{
    public long     ServiceRequestOfferId  { get; private set; }
    /// <summary>The foreign source currency the provider quoted in (e.g. "EUR"). Never equal to <see cref="SettlementCurrencyCode"/>.</summary>
    public string   SourceCurrencyCode     { get; private set; } = default!;
    /// <summary>Always the platform settlement currency ("TRY").</summary>
    public string   SettlementCurrencyCode { get; private set; } = default!;
    /// <summary>R1 rate: 1 unit of <see cref="SourceCurrencyCode"/> = <c>Rate</c> units of <see cref="SettlementCurrencyCode"/>.</summary>
    public decimal  Rate                   { get; private set; }
    /// <summary>Effective date of the resolved rate (from R1). UTC.</summary>
    public DateTime RateDate               { get; private set; }
    /// <summary>The submit instant at which the rate was resolved. UTC.</summary>
    public DateTime ResolvedAtUtc          { get; private set; }

    public ServiceRequestOfferEntity Offer { get; private set; } = default!;

    private OfferFxSnapshotEntity() { }

    /// <summary>
    /// Builds a validated FX snapshot row. Fails loud on a settlement-equals-source mismatch or a non-positive rate — the
    /// resolver must never persist a 0/1.0 placeholder for a currency it could not price. Timestamps are coerced to UTC.
    /// </summary>
    public static OfferFxSnapshotEntity Create(
        long serviceRequestOfferId,
        string sourceCurrencyCode,
        string settlementCurrencyCode,
        decimal rate,
        DateTime rateDate,
        DateTime resolvedAtUtc)
    {
        var source     = (sourceCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        var settlement = (settlementCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();

        if (source.Length == 0)
            throw new InvalidOperationException("OfferFxSnapshot requires a source currency code.");
        if (settlement.Length == 0)
            throw new InvalidOperationException("OfferFxSnapshot requires a settlement currency code.");
        if (source == settlement)
            throw new InvalidOperationException("OfferFxSnapshot is only for a non-settlement source currency.");
        if (rate <= 0m)
            throw new InvalidOperationException($"OfferFxSnapshot requires a positive rate for {source}->{settlement}.");

        return new OfferFxSnapshotEntity
        {
            ServiceRequestOfferId  = serviceRequestOfferId,
            SourceCurrencyCode     = source,
            SettlementCurrencyCode = settlement,
            Rate                   = rate,
            RateDate               = AsUtc(rateDate),
            ResolvedAtUtc          = AsUtc(resolvedAtUtc),
            IsActive               = true,
        };
    }

    private static DateTime AsUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value
         : value.Kind == DateTimeKind.Local ? value.ToUniversalTime()
         : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
