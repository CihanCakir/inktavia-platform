using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest work log repository", "EF Core implementation of IServiceRequestWorkLogRepository.")]
public sealed class ServiceRequestWorkLogRepository : IServiceRequestWorkLogRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestWorkLogRepository(ServiceRequestDbContext db) => _db = db;

    public async Task<IReadOnlyList<ServiceRequestWorkLogEntity>> GetByAssignmentIdAsync(long assignmentId, CancellationToken ct = default)
        => await _db.ServiceRequestWorkLogs
            .AsNoTracking()
            .Where(x => x.ServiceRequestAssignmentId == assignmentId && !x.IsDeleted)
            .OrderBy(x => x.LoggedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceRequestWorkLogEntity>> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => await _db.ServiceRequestWorkLogs
            .AsNoTracking()
            .Where(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted)
            .OrderBy(x => x.LoggedAt)
            .ToListAsync(ct);

    public Task AddAsync(ServiceRequestWorkLogEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestWorkLogs.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestWorkLogEntity entity) => _db.ServiceRequestWorkLogs.Update(entity);
}
