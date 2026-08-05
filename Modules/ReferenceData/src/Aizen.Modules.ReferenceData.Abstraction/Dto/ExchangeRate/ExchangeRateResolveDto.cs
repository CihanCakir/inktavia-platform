using Aizen.Modules.ReferenceData.Abstraction.Enum;

namespace Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;

/// <summary>
/// Result of a point-in-time exchange-rate resolve (R1). Always non-null so callers get a
/// clear empty result (<see cref="HasRate"/> = false) when no rate is effective at the instant.
/// </summary>
public sealed class ExchangeRateResolveDto
{
    /// <summary>True when an effective rate was found at or before <see cref="AsOfUtc"/>.</summary>
    public bool HasRate { get; set; }
    public string FromCurrencyCode { get; set; } = default!;
    public string ToCurrencyCode { get; set; } = default!;
    /// <summary>The effective rate; 0 when <see cref="HasRate"/> is false.</summary>
    public decimal Rate { get; set; }
    public CurrencyRateProviderType ProviderType { get; set; }
    /// <summary>Effective-from instant of the resolved rate (history RateDate); default when none.</summary>
    public DateTime RateDate { get; set; }
    /// <summary>The instant the caller asked about (offer-creation time for the S3 FX snapshot).</summary>
    public DateTimeOffset AsOfUtc { get; set; }
}
