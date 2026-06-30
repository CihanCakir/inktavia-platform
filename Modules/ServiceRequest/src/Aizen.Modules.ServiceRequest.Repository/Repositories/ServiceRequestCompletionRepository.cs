using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest completion repository", "EF Core implementation of IServiceRequestCompletionRepository.")]
public sealed class ServiceRequestCompletionRepository : IServiceRequestCompletionRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestCompletionRepository(ServiceRequestDbContext db) => _db = db;

    public Task<ServiceRequestCompletionEntity?> GetByServiceRequestIdAsync(long serviceRequestId, CancellationToken ct = default)
        => _db.ServiceRequestCompletions
            .FirstOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted, ct);

    public Task AddAsync(ServiceRequestCompletionEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestCompletions.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestCompletionEntity entity) => _db.ServiceRequestCompletions.Update(entity);
}
