using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Vessel.Repository.Repositories;

[DocumentationInfo("Vessel specification repository", "EF Core implementation of IVesselSpecificationRepository.")]
public sealed class VesselSpecificationRepository : IVesselSpecificationRepository
{
    private readonly VesselDbContext _db;

    public VesselSpecificationRepository(VesselDbContext db) => _db = db;

    public Task<VesselSpecificationEntity?> GetByVesselIdAsync(long vesselId, CancellationToken ct = default)
        => _db.VesselSpecifications.FirstOrDefaultAsync(x => x.VesselId == vesselId, ct);

    public Task AddAsync(VesselSpecificationEntity entity, CancellationToken ct = default)
        => _db.VesselSpecifications.AddAsync(entity, ct).AsTask();

    public void Update(VesselSpecificationEntity entity) => _db.VesselSpecifications.Update(entity);

    public void Remove(VesselSpecificationEntity entity) => _db.VesselSpecifications.Remove(entity);
}
