using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface INewsletterSubscriberRepository
{
    Task AddAsync(NewsletterSubscriberEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NewsletterSubscriberEntity entity, CancellationToken ct = default);

    /// <summary>Lookup by the normalised (trim + lower-case) email for the uniqueness/idempotency check.</summary>
    Task<NewsletterSubscriberEntity?> GetByEmailAsync(string normalizedEmail, CancellationToken ct = default);

    Task<NewsletterSubscriberEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<NewsletterSubscriberEntity?> GetByConfirmationTokenAsync(string token, CancellationToken ct = default);
    Task<NewsletterSubscriberEntity?> GetByUnsubscribeTokenAsync(string token, CancellationToken ct = default);

    /// <summary>P1 abuse heuristic — how many sign-ups share this IP hash since <paramref name="sinceUtc"/>.</summary>
    Task<int> CountRecentByIpHashAsync(string ipHash, DateTimeOffset sinceUtc, CancellationToken ct = default);

    /// <summary>Admin paged list (newest first) with combinable status / lang / free-text / date-range filters.</summary>
    Task<(List<NewsletterSubscriberEntity> Items, int Total)> GetPagedAsync(
        NewsletterSubscriberStatus? status, string? lang, string? search,
        DateTimeOffset? from, DateTimeOffset? to, int skip, int take, CancellationToken ct = default);

    /// <summary>Count per status for the dashboard (single grouped query).</summary>
    Task<Dictionary<NewsletterSubscriberStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);

    Task<int> CountCreatedSinceAsync(DateTimeOffset sinceUtc, CancellationToken ct = default);

    /// <summary>Confirmed rows only, oldest first — the CSV export source.</summary>
    Task<List<NewsletterSubscriberEntity>> GetConfirmedForExportAsync(CancellationToken ct = default);
}
