using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INotificationCampaignRepository
{
    Task AddAsync(NotificationCampaignEntity entity, CancellationToken ct = default);
    Task<NotificationCampaignEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task UpdateAsync(NotificationCampaignEntity entity, CancellationToken ct = default);

    /// <summary>Sayfalı kampanya listesi, en yeni önce (CreatedAt DESC, Id tiebreak).</summary>
    Task<(List<NotificationCampaignEntity> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, CancellationToken ct = default);
}
