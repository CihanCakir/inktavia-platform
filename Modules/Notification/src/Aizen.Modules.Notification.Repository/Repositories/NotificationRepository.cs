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

    // Faz 28.5 — Admin gönderim geçmişi (NotificationTemplateRepository.GetPagedAsync Faz 28.3 desenini birebir izler):
    // AsQueryable üstüne koşullu .Where zinciri, hepsi SQL'e itilir; Skip/Take'ten ÖNCE CountAsync; sıralama yalnız
    // öğe çekiminde. Inbox'un aksine Channel==InApp KIRPMASI YOKTUR — admin tüm kanallardaki gönderimi görür.
    public async Task<(List<NotificationEntity> Items, int TotalCount)> GetHistoryPagedAsync(
        DateTimeOffset? from, DateTimeOffset? to, NotificationChannel? channel, NotificationStatus? status,
        string? templateCode, long? recipientUserId, long? campaignId, int skip, int take, CancellationToken ct)
    {
        var q = _db.Notifications.AsQueryable();

        // Tarih aralığı CreatedAt üzerinde (timestamptz range filtresi güvenli — GroupBy değil).
        if (from.HasValue)
            q = q.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue)
            q = q.Where(x => x.CreatedAt <= to.Value);

        if (channel.HasValue)
            q = q.Where(x => x.Channel == channel.Value);

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(templateCode))
        {
            // TemplateCode büyük harf saklanır (entity Create'te ToUpperInvariant); filtre de eşleşmesi için büyütülür.
            var tc = templateCode.Trim().ToUpperInvariant();
            q = q.Where(x => x.TemplateCode == tc);
        }

        if (recipientUserId.HasValue)
            q = q.Where(x => x.RecipientUserId == recipientUserId.Value);

        if (campaignId.HasValue)
            q = q.Where(x => x.CampaignId == campaignId.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)            // deterministic tiebreaker (en yeni önce)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    // BE_NF1 (D4) — the inbox is the list of canonical LOGICAL notifications = the InApp channel row. Email + web/FCM
    // push are delivery mechanisms, not list rows. Without the Channel filter, once N-F2 emits Email/Push rows the same
    // logical event would appear as a duplicate second row (count + list). Filter every recipient read to InApp so list,
    // total count, unread count, and bulk-mark all agree on the one canonical row. Harmless today (all rows are InApp).
    public Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct)
        => _db.Notifications
            .Where(x => x.RecipientUserId == userId && x.Channel == NotificationChannel.InApp)
            .OrderByDescending(x => x.ReadAt == null)   // unread first
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)                // deterministic tiebreaker
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountByRecipientAsync(long userId, CancellationToken ct)
        => _db.Notifications.CountAsync(x => x.RecipientUserId == userId && x.Channel == NotificationChannel.InApp, ct);

    public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct)
        => _db.Notifications.CountAsync(x => x.RecipientUserId == userId && x.Channel == NotificationChannel.InApp && x.ReadAt == null, ct);

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

    public async Task<int> BulkMarkAsReadAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        // FAZ16 (#34): okunmuşluk YALNIZ ReadAt ile işaretlenir. Eskiden Status da Read'e eziliyordu; InApp satırları
        // artık gerçek gönderim durumu (Sent/Failed) taşıdığından bu ezme "iletildi mi?" gerçeğini yok ederdi.
        // Okunmuşluk her sorguda ReadAt==null ile ölçülür (IsRead, GetUnreadCount, sıralama) — Status'e bakılmaz.
        return await _db.Notifications
            .Where(x => x.RecipientUserId == userId && x.Channel == NotificationChannel.InApp && x.ReadAt == null)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.ReadAt, now), ct);
    }
}
