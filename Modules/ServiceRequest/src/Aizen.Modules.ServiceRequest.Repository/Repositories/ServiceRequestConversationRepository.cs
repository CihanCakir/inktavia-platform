using Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Repository.Repositories;

public sealed class ServiceRequestConversationRepository : IServiceRequestConversationRepository
{
    private readonly ServiceRequestDbContext _db;
    public ServiceRequestConversationRepository(ServiceRequestDbContext db) => _db = db;

    public async Task<IReadOnlyList<ServiceRequestConversationEntity>> GetListAsync(string? filter, CancellationToken ct = default)
    {
        var query = _db.Conversations.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(filter) && filter.ToUpper() != "ALL")
            query = query.Where(x => x.Status.ToLower() == filter.ToLower());
        return await query.Include(x => x.Messages).OrderByDescending(x => x.LastMessageAt).ToListAsync(ct);
    }

    public Task<ServiceRequestConversationEntity?> GetByIdWithMessagesAsync(long id, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Messages).ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ServiceRequestConversationEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.Conversations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task AddAsync(ServiceRequestConversationEntity entity, CancellationToken ct = default)
        => _db.Conversations.AddAsync(entity, ct).AsTask();

    public void Update(ServiceRequestConversationEntity entity) => _db.Conversations.Update(entity);
}
