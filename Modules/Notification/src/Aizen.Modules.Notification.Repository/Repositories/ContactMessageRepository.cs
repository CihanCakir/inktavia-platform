using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class ContactMessageRepository : IContactMessageRepository
{
    private readonly NotificationDbContext _db;

    public ContactMessageRepository(NotificationDbContext db) => _db = db;

    public async Task AddAsync(ContactMessageEntity entity, CancellationToken ct = default)
    {
        await _db.ContactMessages.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountRecentByIpHashAsync(string ipHash, DateTimeOffset sinceUtc, CancellationToken ct = default)
        => _db.ContactMessages.CountAsync(x => x.IpHash == ipHash && x.CreatedAt >= sinceUtc, ct);
}
