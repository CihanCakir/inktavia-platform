using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.Catalog;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("ReferenceData admin BFF remote call",
    "Defines synchronous BFF-to-ReferenceData calls for lookup, currency, location and measurement data. " +
    "Auth headers are injected automatically by AdminPanelBffAuthDelegatingHandler.")]
public interface IReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups")]
    Task<AizenApiResponse<IReadOnlyList<LookupGroupDto>>> GetLookupGroups();

    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/tree")]
    Task<AizenApiResponse<IReadOnlyList<LookupGroupTreeDto>>> GetLookupGroupTree();

    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/lookup-items/{groupCode}")]
    Task<AizenApiResponse<IReadOnlyList<LookupItemDto>>> GetLookupItemsByGroupCode(string groupCode);

    [AizenRemoteCallPost("/api/v1/admin/reference-data/lookup-groups")]
    Task<AizenApiResponse<LookupGroupDto>> CreateLookupGroup(
        [AizenRemoteCallBody] CreateLookupGroupRequest request);

    [AizenRemoteCallPost("/api/v1/admin/reference-data/lookup-items")]
    Task<AizenApiResponse<LookupItemDto>> CreateLookupItem(
        [AizenRemoteCallBody] CreateLookupItemRequest request);

    [AizenRemoteCallGet("/api/v1/reference-data/currencies")]
    Task<AizenApiResponse<IReadOnlyList<CurrencyDto>>> GetCurrencies();

    [AizenRemoteCallGet("/api/v1/reference-data/currencies/{currencyId}")]
    Task<AizenApiResponse<CurrencyDto?>> GetCurrencyById(long currencyId);

    [AizenRemoteCallGet("/api/v1/reference-data/locations/countries")]
    Task<AizenApiResponse<IReadOnlyList<CountryDto>>> GetCountries();

    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities")]
    Task<AizenApiResponse<IReadOnlyList<CityDto>>> GetCities(string countryCode);

    [AizenRemoteCallGet("/api/v1/reference-data/measurement-units")]
    Task<AizenApiResponse<IReadOnlyList<MeasurementUnitDto>>> GetMeasurementUnits(
        [Refit.Query] string? type = null);

    [AizenRemoteCallGet("/api/v1/reference-data/system-parameters")]
    Task<AizenApiResponse<IReadOnlyList<SystemParameterDto>>> GetSystemParameters();

    // ── Brand/model catalog — admin CRUD + review queue (module /api/v1/admin/reference-data/*-catalog) ──
    // Reads (browse; onlyActive=false so admin sees inactive) reuse the public read controller.
    [AizenRemoteCallGet("/api/v1/reference-data/vessel-catalog/brands")]
    Task<AizenApiResponse<List<VesselBrandDto>>> GetVesselBrands([Refit.Query] string? search = null, [Refit.Query] bool onlyActive = false, [Refit.Query] int take = 50);
    [AizenRemoteCallGet("/api/v1/reference-data/vessel-catalog/brands/{brandId}/models")]
    Task<AizenApiResponse<List<VesselModelDto>>> GetVesselModels(long brandId, [Refit.Query] string? search = null, [Refit.Query] string? typeCode = null, [Refit.Query] bool onlyActive = false, [Refit.Query] int take = 50);
    [AizenRemoteCallGet("/api/v1/reference-data/engine-catalog/brands")]
    Task<AizenApiResponse<List<EngineBrandDto>>> GetEngineBrands([Refit.Query] string? search = null, [Refit.Query] bool onlyActive = false, [Refit.Query] int take = 50);
    [AizenRemoteCallGet("/api/v1/reference-data/engine-catalog/brands/{brandId}/models")]
    Task<AizenApiResponse<List<EngineModelDto>>> GetEngineModels(long brandId, [Refit.Query] string? search = null, [Refit.Query] string? typeCode = null, [Refit.Query] bool onlyActive = false, [Refit.Query] int take = 50);

    // Review queue
    [AizenRemoteCallGet("/api/v1/admin/reference-data/vessel-catalog/brands/review")]
    Task<AizenApiResponse<List<VesselBrandDto>>> GetVesselBrandsForReview([Refit.Query] int skip = 0, [Refit.Query] int take = 20);
    [AizenRemoteCallGet("/api/v1/admin/reference-data/vessel-catalog/models/review")]
    Task<AizenApiResponse<List<VesselModelDto>>> GetVesselModelsForReview([Refit.Query] int skip = 0, [Refit.Query] int take = 20);
    [AizenRemoteCallGet("/api/v1/admin/reference-data/engine-catalog/brands/review")]
    Task<AizenApiResponse<List<EngineBrandDto>>> GetEngineBrandsForReview([Refit.Query] int skip = 0, [Refit.Query] int take = 20);
    [AizenRemoteCallGet("/api/v1/admin/reference-data/engine-catalog/models/review")]
    Task<AizenApiResponse<List<EngineModelDto>>> GetEngineModelsForReview([Refit.Query] int skip = 0, [Refit.Query] int take = 20);

    // Vessel CRUD
    [AizenRemoteCallPost("/api/v1/admin/reference-data/vessel-catalog/brands")]
    Task<AizenApiResponse<VesselBrandDto>> CreateVesselBrand([AizenRemoteCallBody] CreateVesselBrandRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/brands/{id}")]
    Task<AizenApiResponse<VesselBrandDto>> UpdateVesselBrand(long id, [AizenRemoteCallBody] UpdateVesselBrandRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/brands/{id}/approve")]
    Task<AizenApiResponse<BoolResult>> ApproveVesselBrand(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/brands/{id}/activate")]
    Task<AizenApiResponse<BoolResult>> ActivateVesselBrand(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/brands/{id}/deactivate")]
    Task<AizenApiResponse<BoolResult>> DeactivateVesselBrand(long id);
    [AizenRemoteCallPost("/api/v1/admin/reference-data/vessel-catalog/models")]
    Task<AizenApiResponse<VesselModelDto>> CreateVesselModel([AizenRemoteCallBody] CreateVesselModelRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/models/{id}")]
    Task<AizenApiResponse<VesselModelDto>> UpdateVesselModel(long id, [AizenRemoteCallBody] UpdateVesselModelRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/models/{id}/approve")]
    Task<AizenApiResponse<BoolResult>> ApproveVesselModel(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/models/{id}/activate")]
    Task<AizenApiResponse<BoolResult>> ActivateVesselModel(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/vessel-catalog/models/{id}/deactivate")]
    Task<AizenApiResponse<BoolResult>> DeactivateVesselModel(long id);

    // Engine CRUD
    [AizenRemoteCallPost("/api/v1/admin/reference-data/engine-catalog/brands")]
    Task<AizenApiResponse<EngineBrandDto>> CreateEngineBrand([AizenRemoteCallBody] CreateEngineBrandRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/brands/{id}")]
    Task<AizenApiResponse<EngineBrandDto>> UpdateEngineBrand(long id, [AizenRemoteCallBody] UpdateEngineBrandRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/brands/{id}/approve")]
    Task<AizenApiResponse<BoolResult>> ApproveEngineBrand(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/brands/{id}/activate")]
    Task<AizenApiResponse<BoolResult>> ActivateEngineBrand(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/brands/{id}/deactivate")]
    Task<AizenApiResponse<BoolResult>> DeactivateEngineBrand(long id);
    [AizenRemoteCallPost("/api/v1/admin/reference-data/engine-catalog/models")]
    Task<AizenApiResponse<EngineModelDto>> CreateEngineModel([AizenRemoteCallBody] CreateEngineModelRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/models/{id}")]
    Task<AizenApiResponse<EngineModelDto>> UpdateEngineModel(long id, [AizenRemoteCallBody] UpdateEngineModelRequest req);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/models/{id}/approve")]
    Task<AizenApiResponse<BoolResult>> ApproveEngineModel(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/models/{id}/activate")]
    Task<AizenApiResponse<BoolResult>> ActivateEngineModel(long id);
    [AizenRemoteCallPut("/api/v1/admin/reference-data/engine-catalog/models/{id}/deactivate")]
    Task<AizenApiResponse<BoolResult>> DeactivateEngineModel(long id);
}

public sealed class LookupGroupListResult { public List<LookupGroupDto>? Items { get; set; } }
public sealed class LookupGroupTreeResult { public List<LookupGroupTreeDto>? Items { get; set; } }
public sealed class LookupItemListResult { public List<LookupItemDto>? Items { get; set; } }
public sealed class CurrencyListResult { public List<CurrencyDto>? Items { get; set; } }
public sealed class CurrencyDetailResult { public CurrencyDto? Currency { get; set; } }
public sealed class CountryListResult { public List<CountryDto>? Items { get; set; } }
public sealed class CityListResult { public List<CityDto>? Items { get; set; } }
public sealed class MeasurementUnitListResult { public List<MeasurementUnitDto>? Items { get; set; } }
public sealed class SystemParameterListResult { public List<SystemParameterDto>? Items { get; set; } }
