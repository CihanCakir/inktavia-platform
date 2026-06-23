using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("ReferenceData admin BFF remote call", "Defines synchronous BFF-to-ReferenceData calls for lookup, currency, location and measurement data.")]
public interface IReferenceDataAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups")]
    Task<AizenApiResponse<IReadOnlyList<LookupGroupDto>>> GetLookupGroups(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/tree")]
    Task<AizenApiResponse<IReadOnlyList<LookupGroupTreeDto>>> GetLookupGroupTree(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/reference-data/lookup-groups/lookup-items/{groupCode}")]
    Task<AizenApiResponse<IReadOnlyList<LookupItemDto>>> GetLookupItemsByGroupCode(
        string groupCode,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/reference-data/lookup-groups")]
    Task<AizenApiResponse<LookupGroupDto>> CreateLookupGroup(
        [AizenRemoteCallBody] CreateLookupGroupRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallPost("/api/v1/admin/reference-data/lookup-items")]
    Task<AizenApiResponse<LookupItemDto>> CreateLookupItem(
        [AizenRemoteCallBody] CreateLookupItemRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/reference-data/currencies")]
    Task<AizenApiResponse<IReadOnlyList<CurrencyDto>>> GetCurrencies(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/reference-data/currencies/{currencyId}")]
    Task<AizenApiResponse<CurrencyDto?>> GetCurrencyById(
        long currencyId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/reference-data/locations/countries")]
    Task<AizenApiResponse<IReadOnlyList<CountryDto>>> GetCountries(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities")]
    Task<AizenApiResponse<IReadOnlyList<CityDto>>> GetCities(
        string countryCode,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/reference-data/measurement-units")]
    Task<AizenApiResponse<IReadOnlyList<MeasurementUnitDto>>> GetMeasurementUnits(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] string? type = null);

    [AizenRemoteCallGet("/api/v1/reference-data/system-parameters")]
    Task<AizenApiResponse<IReadOnlyList<SystemParameterDto>>> GetSystemParameters(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);
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
