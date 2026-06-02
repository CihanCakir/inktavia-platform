namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface ILocationValidationService
{
    Task<bool> CountryExistsAsync(string countryCode, CancellationToken cancellationToken = default);
    Task<bool> CityExistsAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default);
    Task<bool> DistrictExistsAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default);
}
