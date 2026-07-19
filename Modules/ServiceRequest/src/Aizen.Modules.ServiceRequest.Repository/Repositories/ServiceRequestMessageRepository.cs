using Aizen.Modules.ServiceRequest.Abstraction.Enum;
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

    public Task<bool> HasOwnerMessageAsync(long serviceRequestId, CancellationToken ct = default)
        => _db.ServiceRequestMessages.AnyAsync(
            x => x.ServiceRequestId == serviceRequestId
                 && x.SenderType == ServiceRequestMessageSenderType.Owner
                 && !x.IsDeleted, ct);

    public Task<bool> HasOfferMessageForOfferAsync(long serviceRequestId, long offerId, CancellationToken ct = default)
        => _db.ServiceRequestMessages.AnyAsync(
            x => x.ServiceRequestId == serviceRequestId
                 && x.MessageType == ServiceRequestMessageType.Offer
                 && x.Content.Contains($"offer:{offerId}")
                 && !x.IsDeleted, ct);

    public Task<bool> HasSystemMessageAsync(long serviceRequestId, string statusCode, CancellationToken ct = default)
        => _db.ServiceRequestMessages.AnyAsync(
            x => x.ServiceRequestId == serviceRequestId
                 && x.MessageType == ServiceRequestMessageType.StatusChange
                 && x.Content == statusCode
                 && !x.IsDeleted, ct);

    public Task AddAsync(ServiceRequestMessageEntity entity, CancellationToken ct = default)
        => _db.ServiceRequestMessages.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestMessageEntity entity) => _db.ServiceRequestMessages.Update(entity);
}
