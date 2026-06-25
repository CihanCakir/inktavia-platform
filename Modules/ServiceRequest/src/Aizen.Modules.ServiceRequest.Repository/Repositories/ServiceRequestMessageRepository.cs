using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

[DocumentationInfo("ServiceRequest message repository", "EF Core implementation of IServiceRequestMessageRepository.")]
public sealed class ServiceRequestMessageRepository : IServiceRequestMessageRepository
{
    private readonly ServiceRequestDbContext _db;

    public ServiceRequestMessageRepository(ServiceRequestDbContext db) => _db = db;

    public async Task<IReadOnlyList<ServiceRequestMessageEntity>> GetByServiceRequestIdAsync(long serviceRequestId, int skip, int take, CancellationToken ct = default)
        => await _db.ServiceRequestMessages
            .AsNoTracking()
            .Where(x => x.ServiceRequestId == serviceRequestId && !x.IsDeleted)
            .OrderBy(x => x.CreateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(long serviceRequestId, long recipientUserId, CancellationToken ct = default)
        => _db.ServiceRequestMessages.CountAsync(
            x => x.ServiceRequestId == serviceRequestId
                 && x.SenderUserId != recipientUserId
                 && !x.IsRead
                 && !x.IsDeleted,
            ct);

    public Task AddAsync(ServiceRequestMessageEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestMessages.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestMessageEntity entity) => _db.ServiceRequestMessages.Update(entity);
}
