using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;

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

    // Nearest marinas to a device position (ordered by ascending distance). Reference read — the service token
    // authenticates it; no owner assertion needed.
    [AizenRemoteCallGet("/api/v1/reference-data/marinas/nearby")]
    Task<AizenApiResponse<List<MarinaNearbyDto>>> GetNearbyMarinas(
        [Refit.Query] double lat, [Refit.Query] double lng, [Refit.Query] int limit = 10);

    // Resolve a single marina (id → name + coordinates). Used to denormalize a marina pick into the vessel's
    // selected-location before the Vessel-module write.
    [AizenRemoteCallGet("/api/v1/reference-data/marinas/{id}")]
    Task<AizenApiResponse<MarinaDto>> GetMarinaById(long id);

    // ── Brand/model catalog (vessel wizard) — reference reads + "not in list" submissions ──
    [AizenRemoteCallGet("/api/v1/reference-data/vessel-catalog/brands")]
    Task<AizenApiResponse<List<VesselBrandDto>>> GetVesselBrands([Refit.Query] string? search = null, [Refit.Query] bool onlyActive = true, [Refit.Query] int take = 20);

    [AizenRemoteCallGet("/api/v1/reference-data/vessel-catalog/brands/{brandId}/models")]
    Task<AizenApiResponse<List<VesselModelDto>>> GetVesselModels(long brandId, [Refit.Query] string? search = null, [Refit.Query] string? typeCode = null, [Refit.Query] bool onlyActive = true, [Refit.Query] int take = 20);

    [AizenRemoteCallGet("/api/v1/reference-data/vessel-catalog/models/{id}")]
    Task<AizenApiResponse<VesselModelDto>> GetVesselModelById(long id);

    [AizenRemoteCallPost("/api/v1/reference-data/vessel-catalog/brands")]
    Task<AizenApiResponse<VesselBrandDto>> SubmitVesselBrand([AizenRemoteCallBody] SubmitVesselBrandRequest req);

    [AizenRemoteCallPost("/api/v1/reference-data/vessel-catalog/brands/{brandId}/models")]
    Task<AizenApiResponse<VesselModelDto>> SubmitVesselModel(long brandId, [AizenRemoteCallBody] SubmitVesselModelRequest req);

    [AizenRemoteCallGet("/api/v1/reference-data/engine-catalog/brands")]
    Task<AizenApiResponse<List<EngineBrandDto>>> GetEngineBrands([Refit.Query] string? search = null, [Refit.Query] bool onlyActive = true, [Refit.Query] int take = 20);

    [AizenRemoteCallGet("/api/v1/reference-data/engine-catalog/brands/{brandId}/models")]
    Task<AizenApiResponse<List<EngineModelDto>>> GetEngineModels(long brandId, [Refit.Query] string? search = null, [Refit.Query] string? typeCode = null, [Refit.Query] bool onlyActive = true, [Refit.Query] int take = 20);

    [AizenRemoteCallGet("/api/v1/reference-data/engine-catalog/models/{id}")]
    Task<AizenApiResponse<EngineModelDto>> GetEngineModelById(long id);

    [AizenRemoteCallPost("/api/v1/reference-data/engine-catalog/brands")]
    Task<AizenApiResponse<EngineBrandDto>> SubmitEngineBrand([AizenRemoteCallBody] SubmitEngineBrandRequest req);

    [AizenRemoteCallPost("/api/v1/reference-data/engine-catalog/brands/{brandId}/models")]
    Task<AizenApiResponse<EngineModelDto>> SubmitEngineModel(long brandId, [AizenRemoteCallBody] SubmitEngineModelRequest req);
}
