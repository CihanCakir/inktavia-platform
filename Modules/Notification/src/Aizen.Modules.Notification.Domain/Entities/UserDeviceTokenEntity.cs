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

    /// <summary>Web Push subscription endpoint URL. Null for FCM/APNs.</summary>
    public string? Endpoint { get; private set; }

    /// <summary>Web Push P-256 Diffie-Hellman key. Null for FCM/APNs.</summary>
    public string? P256dhKey { get; private set; }

    /// <summary>Web Push authentication secret. Null for FCM/APNs.</summary>
    public string? AuthKey { get; private set; }

    protected UserDeviceTokenEntity() { }

    /// <summary>Creates a device token subscription for FCM or APNs.</summary>
    public static UserDeviceTokenEntity CreateDeviceToken(long userId, string token, PushPlatform platform)
    {
        if (platform == PushPlatform.WebPush)
            throw new ArgumentException("Use CreateWebPush for WebPush subscriptions.", nameof(platform));

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

    /// <summary>Creates a Web Push subscription with the three required values.</summary>
    public static UserDeviceTokenEntity CreateWebPush(long userId, string endpoint, string p256dhKey, string authKey)
    {
        return new UserDeviceTokenEntity
        {
            UserId       = userId,
            DeviceToken  = endpoint,
            Platform     = PushPlatform.WebPush,
            Endpoint     = endpoint,
            P256dhKey    = p256dhKey,
            AuthKey      = authKey,
            IsActive     = true,
            RegisteredAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Backwards-compatible factory — routes to CreateDeviceToken.</summary>
    public static UserDeviceTokenEntity Create(long userId, string token, PushPlatform platform)
        => CreateDeviceToken(userId, token, platform);

    public void Refresh()    => LastActiveAt = DateTimeOffset.UtcNow;
    public void Deactivate() => IsActive = false;
}
