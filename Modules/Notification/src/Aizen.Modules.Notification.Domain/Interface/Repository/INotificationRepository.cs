using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationRepository
{
    Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Faz 28.5 — Admin gönderim geçmişi: TÜM kanallardaki gönderilmiş bildirimler üzerinde sayfalı sorgu
    /// (inbox'tan farkı: InApp'e kırpılmaz — Email/Push dahil "gönderilen her şey" görünür). Tüm filtreler opsiyonel
    /// ve birleştirilebilir; hepsi SQL'e itilir (bellekte filtreleme yok). Sıralama en yeni önce (CreatedAt DESC).
    /// Alıcı araması v1'de YALNIZ recipientUserId iledir; e-posta ile arama, mevcut bir Identity ucu olmadığı için
    /// bilinçli olarak sonraki faza bırakıldı.
    /// </summary>
    Task<(List<NotificationEntity> Items, int TotalCount)> GetHistoryPagedAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        NotificationChannel? channel,
        NotificationStatus? status,
        string? templateCode,
        long? recipientUserId,
        long? campaignId,
        int skip,
        int take,
        CancellationToken ct = default);
    Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByRecipientAsync(long userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task AddAsync(NotificationEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationEntity entity, CancellationToken ct = default);
    Task<int> BulkMarkAsReadAsync(long userId, CancellationToken ct = default);
}
