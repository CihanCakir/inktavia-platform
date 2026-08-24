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

    public Task<List<NotificationTemplateContentEntity>> GetAllByTemplateIdAsync(long templateId, CancellationToken ct)
        => _db.NotificationTemplateContents
            .Where(c => c.TemplateId == templateId)
            .OrderBy(c => c.Channel).ThenBy(c => c.Locale).ThenByDescending(c => c.Version)
            .ToListAsync(ct);

    public Task<NotificationTemplateContentEntity?> GetCurrentDraftAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct)
    {
        var loc = locale.ToLowerInvariant();
        return _db.NotificationTemplateContents
            .Where(c => c.TemplateId == templateId && c.Channel == channel && c.Locale == loc
                        && c.Status == TemplateContentStatus.Draft)
            .OrderByDescending(c => c.Version)
            .FirstOrDefaultAsync(ct);
    }

    public Task<NotificationTemplateContentEntity?> GetCurrentPublishedAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct)
    {
        var loc = locale.ToLowerInvariant();
        return _db.NotificationTemplateContents
            .Where(c => c.TemplateId == templateId && c.Channel == channel && c.Locale == loc
                        && c.Status == TemplateContentStatus.Published)
            .OrderByDescending(c => c.Version)
            .FirstOrDefaultAsync(ct);
    }

    public Task<List<NotificationTemplateContentEntity>> GetVersionsAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct)
    {
        var loc = locale.ToLowerInvariant();
        return _db.NotificationTemplateContents
            .Where(c => c.TemplateId == templateId && c.Channel == channel && c.Locale == loc)
            .OrderByDescending(c => c.Version)
            .ToListAsync(ct);
    }

    public async Task<int> GetMaxVersionAsync(
        long templateId, NotificationChannel channel, string locale, CancellationToken ct)
    {
        var loc = locale.ToLowerInvariant();
        // Boş küme durumunda MaxAsync patlamasın diye önce filtreli sorgu, sonra nullable max.
        var max = await _db.NotificationTemplateContents
            .Where(c => c.TemplateId == templateId && c.Channel == channel && c.Locale == loc)
            .Select(c => (int?)c.Version)
            .MaxAsync(ct);
        return max ?? 0;
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
