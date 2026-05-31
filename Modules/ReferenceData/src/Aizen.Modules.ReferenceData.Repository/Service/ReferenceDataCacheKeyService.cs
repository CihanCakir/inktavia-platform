using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class ReferenceDataCacheKeyService : IReferenceDataCacheKeyService
{
    private const string Prefix = "refdata";

    public string CurrencyList() => $"{Prefix}:currency:list";
    public string CurrencyDetail(long id) => $"{Prefix}:currency:{id}";
    public string BaseCurrency() => $"{Prefix}:currency:base";
    public string ExchangeRate(string fromCode, string toCode) => $"{Prefix}:exchangerate:{fromCode.ToUpperInvariant()}:{toCode.ToUpperInvariant()}";
    public string ExchangeRatesByCurrency(string currencyCode) => $"{Prefix}:exchangerate:by:{currencyCode.ToUpperInvariant()}";
    public string LookupGroupList() => $"{Prefix}:lookupgroup:list";
    public string LookupGroupDetail(long id) => $"{Prefix}:lookupgroup:{id}";
    public string LookupGroupTree() => $"{Prefix}:lookupgroup:tree";
    public string LookupItemsByGroup(string groupCode) => $"{Prefix}:lookupitem:group:{groupCode.ToUpperInvariant()}";
    public string MeasurementUnitList() => $"{Prefix}:measurement:list";
    public string MeasurementUnitDetail(long id) => $"{Prefix}:measurement:{id}";
    public string MeasurementUnitsByType(string unitType) => $"{Prefix}:measurement:type:{unitType}";
    public string CountryList() => $"{Prefix}:location:country:list";
    public string CountryDetail(string countryCode) => $"{Prefix}:location:country:{countryCode.ToUpperInvariant()}";
    public string CitiesByCountry(string countryCode) => $"{Prefix}:location:city:{countryCode.ToUpperInvariant()}";
    public string DistrictsByCity(string countryCode, string cityCode) => $"{Prefix}:location:district:{countryCode.ToUpperInvariant()}:{cityCode.ToUpperInvariant()}";
    public string NeighborhoodsByDistrict(string countryCode, string cityCode, string districtCode) => $"{Prefix}:location:neighborhood:{countryCode.ToUpperInvariant()}:{cityCode.ToUpperInvariant()}:{districtCode.ToUpperInvariant()}";
    public string SystemParameter(string key) => $"{Prefix}:sysparam:{key}";
    public string SystemParameterList() => $"{Prefix}:sysparam:list";
}
