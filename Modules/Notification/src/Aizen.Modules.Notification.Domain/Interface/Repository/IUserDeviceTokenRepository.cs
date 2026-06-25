using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Domain.Interface.Repository;

public interface IUserDeviceTokenRepository
{
    Task<List<UserDeviceTokenEntity>> GetActiveByUserAsync(long userId, CancellationToken ct = default);
    Task<UserDeviceTokenEntity?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task UpsertAsync(long userId, string token, PushPlatform platform, CancellationToken ct = default);
    Task DeactivateAsync(string token, CancellationToken ct = default);
}
