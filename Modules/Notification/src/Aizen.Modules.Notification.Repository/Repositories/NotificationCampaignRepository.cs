using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NotificationCampaignRepository : INotificationCampaignRepository
{
    private readonly NotificationDbContext _db;

    public NotificationCampaignRepository(NotificationDbContext db) => _db = db;

    public async Task AddAsync(NotificationCampaignEntity entity, CancellationToken ct)
    {
        await _db.NotificationCampaigns.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<NotificationCampaignEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.NotificationCampaigns.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task UpdateAsync(NotificationCampaignEntity entity, CancellationToken ct)
    {
        _db.NotificationCampaigns.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(List<NotificationCampaignEntity> Items, int TotalCount)> GetPagedAsync(
        int skip, int take, CancellationToken ct)
    {
        var q = _db.NotificationCampaigns.AsQueryable();
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);
        return (items, total);
    }
}
