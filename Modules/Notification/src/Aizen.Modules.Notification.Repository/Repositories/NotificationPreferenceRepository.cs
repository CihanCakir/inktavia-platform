using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationDbContext _db;

    public NotificationPreferenceRepository(NotificationDbContext db) => _db = db;

    public Task<List<NotificationPreferenceEntity>> GetByUserAsync(long userId, CancellationToken ct)
        => _db.NotificationPreferences.Where(x => x.UserId == userId).ToListAsync(ct);

    public async Task UpsertAsync(
        long userId, NotificationCategory category, NotificationChannel channel, bool enabled, CancellationToken ct)
    {
        var existing = await _db.NotificationPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Category == category && x.Channel == channel, ct);

        if (existing is not null)
        {
            existing.SetEnabled(enabled);
            _db.NotificationPreferences.Update(existing);
        }
        else
        {
            await _db.NotificationPreferences.AddAsync(
                NotificationPreferenceEntity.Create(userId, category, channel, enabled), ct);
        }

        await _db.SaveChangesAsync(ct);
    }
}
