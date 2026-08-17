using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;

public interface IServiceRequestReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities/{cityCode}")]
    Task<AizenApiResponse<SrCityValidationDto>> GetCity(string countryCode, string cityCode);

    [AizenRemoteCallGet("/api/v1/reference-data/measurement-units?onlyActive=true")]
    Task<AizenApiResponse<List<SrMeasurementUnitDto>>> GetActiveMeasurementUnits();

    // S4 — R3 by-code measurement-unit resolve (validates a Travel PerKm line's unitCode against the seeded KILOMETER unit).
    // The ReferenceData query is distributed-cached (24 h; measurement units are near-static). An unknown code resolves to a
    // null Body (the query returns null, never throws) — the caller treats null/inactive/non-KILOMETER as a fail-loud reject.
    [AizenRemoteCallGet("/api/v1/reference-data/measurement-units/by-code/{code}")]
    Task<AizenApiResponse<SrMeasurementUnitDetailDto?>> GetMeasurementUnitByCode(string code);

    // R1 — point-in-time FX resolve for the S3 offer-creation snapshot (no re-valuation after acceptance).
    [AizenRemoteCallGet("/api/v1/reference-data/exchange-rates/resolve?fromCurrencyCode={fromCurrencyCode}&toCurrencyCode={toCurrencyCode}&asOfUtc={asOfUtc}")]
    Task<AizenApiResponse<SrExchangeRateResolveDto>> ResolveExchangeRate(string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc);

    // S2c — R4 marine lookup items for a group (drives S2a definition group-resolution, S2b value validation, and the
    // S2d acceptance-snapshot label). Reuses the existing GetLookupItemsByGroup query. Envelope-correct; an unknown group
    // resolves to an empty Body (the ReferenceData query never throws for a missing group) — the caller treats
    // empty-for-a-Lookup as "unknown group" and fails loud.
    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/lookup-items/{groupCode}?onlyActive={onlyActive}")]
    Task<AizenApiResponse<List<SrLookupItemDto>>> GetLookupItemsByGroup(string groupCode, bool onlyActive);
}

/// <summary>S2c — minimal projection of an R4 lookup item. <see cref="Code"/> is the language-neutral key; per the R4
/// convention <see cref="Name"/> is the Turkish display label and <see cref="Description"/> the English one.</summary>
public sealed class SrLookupItemDto
{
    public string Code { get; set; } = default!;
    public string GroupCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SrCityValidationDto
{
    public string CityCode { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    /// <summary>S4 — the city's display label (denormalized into the acceptance travel snapshot; no re-lookup afterwards).</summary>
    public string? Name { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SrMeasurementUnitDto
{
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; }
}

/// <summary>S4 — minimal projection of the R3 by-code measurement-unit result (used to confirm a Travel PerKm unitCode is the KILOMETER unit).</summary>
public sealed class SrMeasurementUnitDetailDto
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
