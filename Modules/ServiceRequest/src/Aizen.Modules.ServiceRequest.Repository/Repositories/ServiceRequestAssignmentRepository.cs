using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest assignment repository", "EF Core implementation of IServiceRequestAssignmentRepository.")]
public sealed class ServiceRequestAssignmentRepository : IServiceRequestAssignmentRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestAssignmentRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestAssignmentEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequestAssignments
            .Include(x => x.WorkLogs)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestAssignmentEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => _db.ServiceRequestAssignments
            .Include(x => x.WorkLogs)
            .FirstOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceRequestAssignmentEntity>> GetByProviderProfileIdAsync(long providerProfileId, int skip, int take, CancellationToken ct = default)
        => await _db.ServiceRequestAssignments
            .AsNoTracking()
            .Where(x => x.ProviderProfileId == providerProfileId && !x.IsDeleted)
            .OrderByDescending(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task AddAsync(ServiceRequestAssignmentEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestAssignments.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestAssignmentEntity entity) => _db.ServiceRequestAssignments.Update(entity);
}
