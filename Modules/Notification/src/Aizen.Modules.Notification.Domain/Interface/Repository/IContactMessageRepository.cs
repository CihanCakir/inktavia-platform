using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface IContactMessageRepository
{
    Task AddAsync(ContactMessageEntity entity, CancellationToken ct = default);

    /// <summary>M4 spam heuristic — how many tickets share this IP hash since <paramref name="sinceUtc"/> (rate check).</summary>
    Task<int> CountRecentByIpHashAsync(string ipHash, DateTimeOffset sinceUtc, CancellationToken ct = default);
}
