namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface IReferenceDataCacheKeyService
{
    string CurrencyList();
    string CurrencyDetail(long id);
    string BaseCurrency();
    string ExchangeRate(string fromCode, string toCode);
    string ExchangeRatesByCurrency(string currencyCode);
    string LookupGroupList();
    string LookupGroupDetail(long id);
    string LookupGroupTree();
    string LookupItemsByGroup(string groupCode);
    string MeasurementUnitList();
    string MeasurementUnitDetail(long id);
    string MeasurementUnitsByType(string unitType);
    string CountryList();
    string CountryDetail(string countryCode);
    string CitiesByCountry(string countryCode);
    string DistrictsByCity(string countryCode, string cityCode);
    string NeighborhoodsByDistrict(string countryCode, string cityCode, string districtCode);
    string SystemParameter(string key);
    string SystemParameterList();
}
