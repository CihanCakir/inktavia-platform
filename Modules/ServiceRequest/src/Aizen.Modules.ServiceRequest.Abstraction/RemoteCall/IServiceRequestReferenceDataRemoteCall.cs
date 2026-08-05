using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;

public interface IServiceRequestReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities/{cityCode}")]
    Task<AizenApiResponse<SrCityValidationDto>> GetCity(string countryCode, string cityCode);

    [AizenRemoteCallGet("/api/v1/reference-data/measurement-units?onlyActive=true")]
    Task<AizenApiResponse<List<SrMeasurementUnitDto>>> GetActiveMeasurementUnits();

    // R1 — point-in-time FX resolve for the S3 offer-creation snapshot (no re-valuation after acceptance).
    [AizenRemoteCallGet("/api/v1/reference-data/exchange-rates/resolve?fromCurrencyCode={fromCurrencyCode}&toCurrencyCode={toCurrencyCode}&asOfUtc={asOfUtc}")]
    Task<AizenApiResponse<SrExchangeRateResolveDto>> ResolveExchangeRate(string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc);
}

public sealed class SrCityValidationDto
{
    public string CityCode { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public bool IsActive { get; set; }
}

public sealed class SrMeasurementUnitDto
{
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; }
}

/// <summary>R1 — minimal projection of the ReferenceData point-in-time FX resolve result.</summary>
public sealed class SrExchangeRateResolveDto
{
    /// <summary>False when no rate was effective at the requested instant (empty result).</summary>
    public bool HasRate { get; set; }
    public string FromCurrencyCode { get; set; } = default!;
    public string ToCurrencyCode { get; set; } = default!;
    public decimal Rate { get; set; }
    public DateTime RateDate { get; set; }
    public DateTimeOffset AsOfUtc { get; set; }
}
