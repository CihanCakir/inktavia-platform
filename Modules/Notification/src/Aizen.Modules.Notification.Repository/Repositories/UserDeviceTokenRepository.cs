using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly NotificationDbContext _db;

    public UserDeviceTokenRepository(NotificationDbContext db) => _db = db;

    public Task<List<UserDeviceTokenEntity>> GetActiveByUserAsync(long userId, CancellationToken ct)
        => _db.UserDeviceTokens.Where(x => x.UserId == userId && x.IsActive).ToListAsync(ct);

    public Task<UserDeviceTokenEntity?> GetByTokenAsync(string token, CancellationToken ct)
        => _db.UserDeviceTokens.FirstOrDefaultAsync(x => x.DeviceToken == token, ct);

    public async Task UpsertAsync(long userId, string token, PushPlatform platform, CancellationToken ct)
    {
        var existing = await GetByTokenAsync(token, ct);
        if (existing is not null)
        {
            existing.Refresh();
            _db.UserDeviceTokens.Update(existing);
        }
        else
        {
            var entity = UserDeviceTokenEntity.Create(userId, token, platform);
            await _db.UserDeviceTokens.AddAsync(entity, ct);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(string token, CancellationToken ct)
    {
        var entity = await GetByTokenAsync(token, ct);
        if (entity is null) return;
        entity.Deactivate();
        _db.UserDeviceTokens.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
