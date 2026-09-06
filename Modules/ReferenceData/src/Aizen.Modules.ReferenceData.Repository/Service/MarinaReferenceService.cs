using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Service;

// ⚠️ GEO ON LOAN — same architectural exception as ServiceRequest/GeoHelper. Live geo search belongs to the
// future GeoDiscovery module. This is a Haversine expression evaluated in SQL over the (small) active marina
// catalog — no PostGIS/NetTopologySuite. If the catalog ever grows past a few hundred rows, add a bounding-box
// prefilter (see ServiceRequestRepository) before the distance sort.
public sealed class MarinaReferenceService : IMarinaReferenceService
{
    private const double EarthRadiusMeters = 6_371_000.0;

    private readonly IMarinaRepository _repo;
    private readonly ReferenceDataDbContext _dbContext;

    public MarinaReferenceService(IMarinaRepository repo, ReferenceDataDbContext dbContext)
    {
        _repo = repo;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MarinaNearbyDto>> GetNearbyAsync(double latitude, double longitude, int limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 50);

        var results = await _dbContext.Marinas
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new MarinaNearbyDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                Type = m.Type,
                CountryCode = m.CountryCode,
                CityCode = m.CityCode,
                Province = m.Province,
                District = m.District,
                Latitude = m.Latitude,
                Longitude = m.Longitude,
                // Haversine, computed in SQL. Math.PI/360.0 folds the ×0.5 half-angle into the constant.
                DistanceMeters = EarthRadiusMeters * 2.0 * Math.Asin(Math.Sqrt(
                    Math.Pow(Math.Sin(((double)m.Latitude - latitude) * Math.PI / 360.0), 2) +
                    Math.Cos(latitude * Math.PI / 180.0) * Math.Cos((double)m.Latitude * Math.PI / 180.0) *
                    Math.Pow(Math.Sin(((double)m.Longitude - longitude) * Math.PI / 360.0), 2)))
            })
            .OrderBy(x => x.DistanceMeters)
            .ThenBy(x => x.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<MarinaDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        if (entity is null) return null;

        return new MarinaDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Type = entity.Type,
            CountryCode = entity.CountryCode,
            CityCode = entity.CityCode,
            Province = entity.Province,
            District = entity.District,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            IsActive = entity.IsActive
        };
    }
}
