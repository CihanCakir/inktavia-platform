using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

public interface IReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities")]
    Task<AizenApiResponse<List<CityDto>>> GetCitiesByCountry(string countryCode, [Refit.Query] bool onlyActive = true);

    // Lookup options for a group (VESSEL_TYPE / FUEL_TYPE / ENGINE_TYPE / HULL_MATERIAL / SERVICE_PROVIDER_CATEGORY).
    // Requires the reference_data_read client role on the mobile BFF service account.
    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/lookup-items/{groupCode}")]
    Task<AizenApiResponse<List<LookupItemDto>>> GetLookupItems(string groupCode, [Refit.Query] bool onlyActive = true);

    // Country reference (public/AllowAnonymous on the module, but sent with the service token anyway).
    [AizenRemoteCallGet("/api/v1/reference-data/locations/countries")]
    Task<AizenApiResponse<List<CountryDto>>> GetCountries([Refit.Query] bool onlyActive = true);
}
