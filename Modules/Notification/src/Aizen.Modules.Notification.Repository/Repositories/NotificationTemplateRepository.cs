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

    public async Task<(List<NotificationTemplateEntity> Items, int TotalCount)> GetPagedAsync(
        NotificationChannel? channel, string? locale, TemplateContentStatus? status, bool? enabled,
        string? search, int skip, int take, CancellationToken ct)
    {
        var q = _db.NotificationTemplates.AsQueryable();

        if (channel.HasValue)
            q = q.Where(t => t.Channel == channel.Value);

        if (enabled.HasValue)
            q = q.Where(t => t.IsActive == enabled.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var sUpper = s.ToUpperInvariant();
            // Kod büyük harfle saklanır; ad serbest. İkisinden birinde geçmesi yeterli.
            q = q.Where(t => t.TemplateCode.Contains(sUpper) || t.Name.Contains(s));
        }

        // locale/status içerik satırı üzerinden EXISTS ile eşleşir (template'in o locale/status'ta içeriği var mı?).
        if (!string.IsNullOrWhiteSpace(locale))
        {
            var loc = locale.ToLowerInvariant();
            q = q.Where(t => _db.NotificationTemplateContents.Any(c => c.TemplateId == t.Id && c.Locale == loc));
        }

        if (status.HasValue)
            q = q.Where(t => _db.NotificationTemplateContents.Any(c => c.TemplateId == t.Id && c.Status == status.Value));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(t => t.Type).ThenBy(t => t.Channel)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

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
