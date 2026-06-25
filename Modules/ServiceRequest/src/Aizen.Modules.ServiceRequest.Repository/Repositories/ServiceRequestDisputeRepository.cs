using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest dispute repository", "EF Core implementation of IServiceRequestDisputeRepository.")]
public sealed class ServiceRequestDisputeRepository : IServiceRequestDisputeRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestDisputeRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestDisputeEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ServiceRequestDisputes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestDisputeEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => _db.ServiceRequestDisputes.FirstOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ServiceRequestDisputeEntity>> GetAllOpenAsync(int skip, int take, CancellationToken ct = default)
        => await _db.ServiceRequestDisputes
            .AsNoTracking()
            .Where(x => x.Status == ServiceRequestDisputeStatus.Open && !x.IsDeleted)
            .OrderByDescending(x => x.OpenedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task AddAsync(ServiceRequestDisputeEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestDisputes.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestDisputeEntity entity) => _db.ServiceRequestDisputes.Update(entity);
}
