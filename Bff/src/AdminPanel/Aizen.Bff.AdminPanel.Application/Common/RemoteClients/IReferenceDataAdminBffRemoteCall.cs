using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;

namespace Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

[DocumentationInfo("ReferenceData admin BFF remote call", "Defines synchronous BFF-to-ReferenceData calls for lookup, currency, location and measurement data.")]
public interface IReferenceDataAdminBffRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/lookup-groups")]
    Task<AizenApiResponse<LookupGroupListResult>> GetLookupGroups(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/lookup-groups/tree")]
    Task<AizenApiResponse<LookupGroupTreeResult>> GetLookupGroupTree(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/lookup-groups/{groupCode}/items")]
    Task<AizenApiResponse<LookupItemListResult>> GetLookupItemsByGroupCode(
        string groupCode,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/currencies")]
    Task<AizenApiResponse<CurrencyListResult>> GetCurrencies(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/currencies/{currencyId}")]
    Task<AizenApiResponse<CurrencyDetailResult>> GetCurrencyById(
        long currencyId,
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/locations/countries")]
    Task<AizenApiResponse<CountryListResult>> GetCountries(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken);

    [AizenRemoteCallGet("/api/v1/locations/cities")]
    Task<AizenApiResponse<CityListResult>> GetCities(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] long? countryId = null);

    [AizenRemoteCallGet("/api/v1/measurement-units")]
    Task<AizenApiResponse<MeasurementUnitListResult>> GetMeasurementUnits(
        [AizenRemoteCallHeader("Authorization")] string authorization,
        [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
        [Refit.Query] string? type = null);

    [AizenRemoteCallGet("/api/v1/system-parameters")]
    Task<AizenApiResponse<SystemParameterListResult>> GetSystemParameters(
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
