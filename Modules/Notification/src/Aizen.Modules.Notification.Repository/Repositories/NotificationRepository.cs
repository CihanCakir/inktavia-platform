using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db) => _db = db;

    public Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct)
        => _db.Notifications
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct)
        => _db.Notifications.CountAsync(x => x.RecipientUserId == userId && x.ReadAt == null, ct);

    public async Task AddAsync(NotificationEntity entity, CancellationToken ct)
    {
        await _db.Notifications.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationEntity entity, CancellationToken ct)
    {
        _db.Notifications.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BulkMarkAsReadAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.Notifications
            .Where(x => x.RecipientUserId == userId && x.ReadAt == null)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.ReadAt, now)
                 .SetProperty(x => x.Status, NotificationStatus.Read), ct);
    }
}
