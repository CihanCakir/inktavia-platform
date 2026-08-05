using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Queries;

/// <summary>
/// R1 — resolves the effective exchange rate between two currencies at a point in time
/// (the latest history row with RateDate &lt;= asOf). Used by ServiceRequest S3 to snapshot
/// the FX rate at offer creation (no re-valuation after acceptance).
/// </summary>
public sealed class ResolveExchangeRateQuery : AizenQuery<ExchangeRateResolveDto>
{
    // Public properties feed the auto-generated distributed cache key (see AizenQueryHandlerDecorator).
    // Order matters — it must match ReferenceDataCacheInvalidationService: From | To | AsOfDay.
    public string FromCurrencyCode { get; }
    public string ToCurrencyCode { get; }

    /// <summary>
    /// UTC day component ("yyyy-MM-dd") of the as-of instant. This — not the full instant — is what
    /// participates in the cache key, so the query is cached per from/to/as-of-day as required.
    /// </summary>
    public string AsOfDay { get; }

    // The exact instant used for resolution. Kept internal so it does NOT widen the cache key to
    // per-instant granularity, while the handler (same assembly) still resolves against it.
    internal DateTimeOffset AsOfUtc { get; }

    public ResolveExchangeRateQuery(string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc)
    {
        FromCurrencyCode = fromCurrencyCode;
        ToCurrencyCode = toCurrencyCode;
        AsOfUtc = asOfUtc;
        AsOfDay = asOfUtc.UtcDateTime.ToString("yyyy-MM-dd");
    }
}
