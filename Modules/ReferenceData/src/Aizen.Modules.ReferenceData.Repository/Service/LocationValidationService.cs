using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Repository.Service;

public sealed class LocationValidationService : ILocationValidationService
{
    private readonly ILocationRepository _repo;

    public LocationValidationService(ILocationRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> CountryExistsAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetCountryAsync(countryCode, cancellationToken);
        return doc != null;
    }

    public async Task<bool> CityExistsAsync(string countryCode, string cityCode, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetCityAsync(countryCode, cityCode, cancellationToken);
        return doc != null;
    }

    public async Task<bool> DistrictExistsAsync(string countryCode, string cityCode, string districtCode, CancellationToken cancellationToken = default)
    {
        var doc = await _repo.GetDistrictAsync(countryCode, cityCode, districtCode, cancellationToken);
        return doc != null;
    }
}
