using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NotificationTemplateContentRepository : INotificationTemplateContentRepository
{
    private readonly NotificationDbContext _db;

    public NotificationTemplateContentRepository(NotificationDbContext db) => _db = db;

    public async Task<List<NotificationTemplateContentEntity>> GetPublishedByTemplateCodeAndChannelAsync(
        string templateCode, NotificationChannel channel, CancellationToken ct)
    {
        var code = templateCode.ToUpperInvariant();

        // notification_templates.TemplateCode → TemplateId üzerinden Published içerikleri (tüm locale/version) çek.
        var query =
            from c in _db.NotificationTemplateContents
            join t in _db.NotificationTemplates on c.TemplateId equals t.Id
            where t.TemplateCode == code
                  && c.Channel == channel
                  && c.Status == TemplateContentStatus.Published
            select c;

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(NotificationTemplateContentEntity entity, CancellationToken ct)
    {
        await _db.NotificationTemplateContents.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationTemplateContentEntity entity, CancellationToken ct)
    {
        _db.NotificationTemplateContents.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
