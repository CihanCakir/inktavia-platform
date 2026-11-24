using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NewsletterSubscriberRepository : INewsletterSubscriberRepository
{
    private readonly NotificationDbContext _db;

    public NewsletterSubscriberRepository(NotificationDbContext db) => _db = db;

    public async Task AddAsync(NewsletterSubscriberEntity entity, CancellationToken ct = default)
    {
        await _db.NewsletterSubscribers.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NewsletterSubscriberEntity entity, CancellationToken ct = default)
    {
        _db.NewsletterSubscribers.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public Task<NewsletterSubscriberEntity?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default)
        => _db.NewsletterSubscribers.FirstOrDefaultAsync(x => x.Email == normalizedEmail, ct);

    public Task<NewsletterSubscriberEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.NewsletterSubscribers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<NewsletterSubscriberEntity?> GetByConfirmationTokenAsync(string token, CancellationToken ct = default)
        => _db.NewsletterSubscribers.FirstOrDefaultAsync(x => x.ConfirmationToken == token, ct);

    public Task<NewsletterSubscriberEntity?> GetByUnsubscribeTokenAsync(string token, CancellationToken ct = default)
        => _db.NewsletterSubscribers.FirstOrDefaultAsync(x => x.UnsubscribeToken == token, ct);

    public Task<int> CountRecentByIpHashAsync(string ipHash, DateTimeOffset sinceUtc, CancellationToken ct = default)
        => _db.NewsletterSubscribers.CountAsync(x => x.IpHash == ipHash && x.CreatedAt >= sinceUtc, ct);

    public async Task<(List<NewsletterSubscriberEntity> Items, int Total)> GetPagedAsync(
        NewsletterSubscriberStatus? status, string? lang, string? search,
        DateTimeOffset? from, DateTimeOffset? to, int skip, int take, CancellationToken ct = default)
    {
        var query = _db.NewsletterSubscribers.AsNoTracking().AsQueryable();

        if (status is not null)
            query = query.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(lang))
        {
            var l = lang.Trim().ToLowerInvariant();
            query = query.Where(x => x.Lang == l);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Email is stored lower-cased → lower the needle and Contains (LIKE in PG, supported by InMemory).
            var needle = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Email.Contains(needle));
        }

        if (from is not null)
            query = query.Where(x => x.CreatedAt >= from);
        if (to is not null)
            query = query.Where(x => x.CreatedAt <= to);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Dictionary<NewsletterSubscriberStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
    {
        var grouped = await _db.NewsletterSubscribers
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped.ToDictionary(g => g.Status, g => g.Count);
    }

    public Task<int> CountCreatedSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct = default)
        => _db.NewsletterSubscribers.CountAsync(x => x.CreatedAt >= sinceUtc, ct);

    public Task<List<NewsletterSubscriberEntity>> GetConfirmedForExportAsync(CancellationToken ct = default)
        => _db.NewsletterSubscribers.AsNoTracking()
            .Where(x => x.Status == NewsletterSubscriberStatus.Confirmed)
            .OrderBy(x => x.ConfirmedAt).ThenBy(x => x.Id)
            .ToListAsync(ct);
}
