using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.Marine.Web.Application.Common.RemoteClients;

/// <summary>
/// ReferenceData module client for the public website (W3). Countries and cities are served by the module's
/// <c>[AllowAnonymous]</c> LocationController; lookup-items are served by LookupController, which is not anonymous —
/// it is authorized by the BFF service account's <c>reference_data_read</c> client role. Every call is sent with the
/// service token by the outgoing auth handler, so both paths work without a participant identity. Responses are
/// enveloped (<see cref="AizenApiResponse{T}"/>); handlers unwrap <c>.Body</c>.
/// </summary>
public interface IReferenceDataRemoteCall : IAizenRemoteCall
{
    // Country reference (public / AllowAnonymous on the module, but sent with the service token anyway).
    [AizenRemoteCallGet("/api/v1/reference-data/locations/countries")]
    Task<AizenApiResponse<List<CountryDto>>> GetCountries([Refit.Query] bool onlyActive = true);

    // Cities of a country (public / AllowAnonymous on the module).
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities")]
    Task<AizenApiResponse<List<CityDto>>> GetCitiesByCountry(string countryCode, [Refit.Query] bool onlyActive = true);

    // Lookup options for a group (e.g. VESSEL_TYPE / FUEL_TYPE / SERVICE_PROVIDER_CATEGORY).
    // Requires the reference_data_read client role on the marine-web-bff service account.
    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/lookup-items/{groupCode}")]
    Task<AizenApiResponse<List<LookupItemDto>>> GetLookupItems(string groupCode, [Refit.Query] bool onlyActive = true);
}
