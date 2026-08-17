using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

public sealed class UserDeviceTokenEntity : AizenEntity
{
    public long           UserId       { get; private set; }
    public string         DeviceToken  { get; private set; } = default!;
    public PushPlatform   Platform     { get; private set; }
    public bool           IsActive     { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset LastActiveAt { get; private set; }

    private UserDeviceTokenEntity() { }

    public static UserDeviceTokenEntity Create(long userId, string token, PushPlatform platform)
    {
        return new UserDeviceTokenEntity
        {
            UserId       = userId,
            DeviceToken  = token,
            Platform     = platform,
            IsActive     = true,
            RegisteredAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow,
        };
    }

    public void Refresh()    => LastActiveAt = DateTimeOffset.UtcNow;
    public void Deactivate() => IsActive = false;
}
