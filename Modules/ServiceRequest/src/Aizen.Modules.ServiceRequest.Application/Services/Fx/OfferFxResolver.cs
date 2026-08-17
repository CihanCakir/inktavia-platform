using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;

namespace Aizen.Modules.ServiceRequest.Application.Services.Fx;

/// <summary>
/// BE-S3a — resolves every distinct non-TRY source currency on an offer to the settlement currency at a single submit
/// instant, converts the foreign lines (via the pure <see cref="OfferFxConverter"/>), and writes the offer-level FX rate
/// snapshots (<see cref="OfferFxSnapshotEntity"/>). <b>Fail-loud</b>: a currency with no effective rate rejects the submit
/// with <c>SR_FX_RATE_UNAVAILABLE</c> — a foreign line can never be priced against a silent 0/1.0 rate. A <b>TRY-only</b>
/// offer (the common case) resolves nothing — <b>no remote call, no mutation, byte-identical to pre-S3</b>.
/// </summary>
public sealed class OfferFxResolver
{
    private readonly IExchangeRateSource _rateSource;

    public OfferFxResolver(IExchangeRateSource rateSource) => _rateSource = rateSource;

    /// <summary>
    /// Resolves + converts in place. Call at submit (and preview) BEFORE the calculation service, on a Draft offer. On an
    /// offer whose lines are all settlement-native this returns immediately without touching the remote call or the offer.
    /// </summary>
    public async Task ResolveAndConvertAsync(ServiceRequestOfferEntity offer, DateTimeOffset asOfUtc, CancellationToken ct)
    {
        var settlement = OfferFxConstants.SettlementCurrency;

        var sourceCurrencies = offer.Items
            .Where(i => i.ItemType != ServiceRequestOfferItemType.Discount)
            .Select(i => (i.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant())
            .Where(c => c.Length > 0 && !OfferFxConstants.IsSettlement(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sourceCurrencies.Count == 0)
            return;   // TRY-only offer — no resolve call, no conversion, no snapshot rows

        var rates = new Dictionary<string, OfferFxRatePoint>(StringComparer.OrdinalIgnoreCase);
        foreach (var ccy in sourceCurrencies)
        {
            var dto = await _rateSource.GetAsync(ccy, settlement, asOfUtc, ct);
            if (dto is null || !dto.HasRate || dto.Rate <= 0m)
                throw new AizenBusinessException($"SR_FX_RATE_UNAVAILABLE: '{ccy}->{settlement}'");
            rates[ccy] = new OfferFxRatePoint(dto.Rate, dto.RateDate);
        }

        var rows = OfferFxConverter.Convert(offer, rates);

        var resolvedAtUtc = asOfUtc.UtcDateTime;
        offer.ReplaceFxSnapshots(rows.Select(r => OfferFxSnapshotEntity.Create(
            offer.Id, r.SourceCurrencyCode, r.SettlementCurrencyCode, r.Rate, r.RateDate, resolvedAtUtc)));
    }
}
