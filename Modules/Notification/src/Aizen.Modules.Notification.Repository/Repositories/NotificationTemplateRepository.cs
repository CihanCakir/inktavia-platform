using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _db;

    public NotificationTemplateRepository(NotificationDbContext db) => _db = db;

    public Task<NotificationTemplateEntity?> GetByCodeAsync(string templateCode, CancellationToken ct)
        => _db.NotificationTemplates.FirstOrDefaultAsync(x => x.TemplateCode == templateCode.ToUpperInvariant(), ct);

    public Task<NotificationTemplateEntity?> GetActiveByTypeAndChannelAsync(
        NotificationType type, NotificationChannel channel, CancellationToken ct)
        => _db.NotificationTemplates.FirstOrDefaultAsync(
            x => x.Type == type && x.Channel == channel && x.IsActive, ct);

    public Task<List<NotificationTemplateEntity>> GetAllAsync(CancellationToken ct)
        => _db.NotificationTemplates.OrderBy(x => x.Type).ThenBy(x => x.Channel).ToListAsync(ct);

    public async Task AddAsync(NotificationTemplateEntity entity, CancellationToken ct)
    {
        await _db.NotificationTemplates.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationTemplateEntity entity, CancellationToken ct)
    {
        _db.NotificationTemplates.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
