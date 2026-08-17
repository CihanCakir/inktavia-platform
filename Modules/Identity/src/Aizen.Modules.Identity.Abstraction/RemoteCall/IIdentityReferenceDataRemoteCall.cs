using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.Identity.Abstraction.RemoteCall;

/// <summary>
/// Identity → ReferenceData module calls. Used to validate city/country codes against the canonical
/// vocabulary. A city code that is not in ReferenceData must be rejected — not stored, not guessed.
/// </summary>
public interface IIdentityReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities/{cityCode}")]
    Task<AizenApiResponse<CityValidationDto>> GetCity(string countryCode, string cityCode);
}

/// <summary>Minimal DTO — we only need to know the city exists.</summary>
public sealed class CityValidationDto
{
    public string CityCode { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
}
