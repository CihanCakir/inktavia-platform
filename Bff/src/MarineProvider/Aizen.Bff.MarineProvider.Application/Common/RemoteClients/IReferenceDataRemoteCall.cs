using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface IReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities")]
    Task<AizenApiResponse<List<CityDto>>> GetCitiesByCountry(string countryCode, [Refit.Query] bool onlyActive = true);

    // Lookup options for one group (VESSEL_TYPE / FUEL_TYPE / … ) — read-only reference for provider form dropdowns.
    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/lookup-items/{groupCode}")]
    Task<AizenApiResponse<List<LookupItemDto>>> GetLookupItems(string groupCode, [Refit.Query] bool onlyActive = true);

    // FAZ17 (#72) — ReferenceData bu ucu (LocationController "countries", [AllowAnonymous]) BAŞINDAN BERİ sunuyordu;
    // eksik olan yalnız provider BFF passthrough'suydu (ölçüldü 2026-08-22). Ülke listesi de şehirler gibi BFF'ten
    // gelmeli — statik/hardcoded değil. Pilot (yalnız Türkiye) kısıtı burada DEĞİL, tek yerde (frontend config).
    [AizenRemoteCallGet("/api/v1/reference-data/locations/countries")]
    Task<AizenApiResponse<List<CountryDto>>> GetCountries([Refit.Query] bool onlyActive = true);
}
