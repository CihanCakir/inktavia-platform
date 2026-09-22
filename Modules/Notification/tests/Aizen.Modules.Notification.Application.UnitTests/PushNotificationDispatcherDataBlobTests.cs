using System.Text.Json;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// Faz 7 — PushNotificationDispatcher artık transport'a MetadataJson yerine NotificationSentPushConsumer ile AYNI
/// şekli (notificationId + referenceType/referenceId) + açık `url` (DeepLink) gönderir. Kampanya push'unun
/// tıklanabilirliği bu `url`'e bağlıdır (kampanyaların referenceType/referenceId'si yoktur).
/// </summary>
public sealed class PushNotificationDispatcherDataBlobTests
{
    private sealed class NoopRepo : INotificationRepository
    {
        public Task UpdateAsync(NotificationEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddAsync(NotificationEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(List<NotificationEntity> Items, int TotalCount)> GetHistoryPagedAsync(DateTimeOffset? from, DateTimeOffset? to, NotificationChannel? channel, NotificationStatus? status, string? templateCode, long? recipientUserId, long? campaignId, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByRecipientAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> BulkMarkAsReadAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class CapturingFcm : IFcmSender
    {
        public string? LastDataJson { get; private set; }
        public Task<string> SendAsync(string deviceToken, string title, string body, string? dataJson, CancellationToken ct)
        {
            LastDataJson = dataJson;
            return Task.FromResult("fcm-ref");
        }
    }

    private sealed class CapturingWebPush : IPushSender
    {
        public string? LastDataJson { get; private set; }
        public Task<string> SendAsync(UserDeviceTokenEntity subscription, string title, string body, string? dataJson, CancellationToken ct)
        {
            LastDataJson = dataJson;
            return Task.FromResult("web-ref");
        }
    }

    private sealed class TokenRepo : IUserDeviceTokenRepository
    {
        private readonly List<UserDeviceTokenEntity> _tokens;
        public TokenRepo(params (string token, PushPlatform platform)[] tokens)
            => _tokens = tokens.Select(t => t.platform == PushPlatform.WebPush
                ? UserDeviceTokenEntity.CreateWebPush(42, t.token, "p256dh", "auth")
                : UserDeviceTokenEntity.CreateDeviceToken(42, t.token, t.platform)).ToList();
        public Task<List<UserDeviceTokenEntity>> GetActiveByUserAsync(long userId, CancellationToken ct = default) => Task.FromResult(_tokens);
        public Task DeactivateAsync(string token, CancellationToken ct = default) => Task.CompletedTask;
        public Task<UserDeviceTokenEntity?> GetByTokenAsync(string token, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpsertAsync(long userId, string token, PushPlatform platform, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpsertWebPushAsync(long userId, string endpoint, string p256dhKey, string authKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeactivateByEndpointAsync(string endpoint, CancellationToken ct = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task Fcm_send_receives_new_blob_with_url_deeplink()
    {
        var notification = NotificationEntity.Create(
            42, NotificationType.AdminBroadcast, NotificationChannel.Push, "ADMIN_CAMPAIGN_CUSTOM",
            "Konu", "Gövde", metadataJson: "{\"campaignId\":7}", referenceType: null, referenceId: null,
            locale: "tr", deepLink: "/app/campaigns/7");
        var fcm = new CapturingFcm();

        var sut = new PushNotificationDispatcher(
            new TokenRepo(("ok", PushPlatform.Fcm)), new NoopRepo(), fcm, new CapturingWebPush(),
            NullLogger<PushNotificationDispatcher>.Instance);

        await sut.DispatchAsync(notification, CancellationToken.None);

        fcm.LastDataJson.Should().NotBeNull();
        fcm.LastDataJson.Should().NotBe(notification.MetadataJson);   // artık MetadataJson gönderilmiyor
        using var doc = JsonDocument.Parse(fcm.LastDataJson!);
        var root = doc.RootElement;
        root.GetProperty("url").GetString().Should().Be("/app/campaigns/7");
        root.GetProperty("notificationId").GetInt64().Should().Be(notification.Id);
        root.GetProperty("referenceType").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("referenceId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task WebPush_send_receives_new_blob_with_domain_refs_flowing()
    {
        // Domain Channel=Push gönderiminde referenceType/referenceId hâlâ akar; url de eklenir.
        var notification = NotificationEntity.Create(
            42, NotificationType.ServiceRequestCreated, NotificationChannel.Push, "TPL",
            "Konu", "Gövde", metadataJson: null, referenceType: "ServiceRequest", referenceId: 99,
            locale: "en", deepLink: "/sr/99");
        var web = new CapturingWebPush();

        var sut = new PushNotificationDispatcher(
            new TokenRepo(("ok", PushPlatform.WebPush)), new NoopRepo(), new CapturingFcm(), web,
            NullLogger<PushNotificationDispatcher>.Instance);

        await sut.DispatchAsync(notification, CancellationToken.None);

        web.LastDataJson.Should().NotBeNull();
        using var doc = JsonDocument.Parse(web.LastDataJson!);
        var root = doc.RootElement;
        root.GetProperty("url").GetString().Should().Be("/sr/99");
        root.GetProperty("referenceType").GetString().Should().Be("ServiceRequest");
        root.GetProperty("referenceId").GetInt64().Should().Be(99);
        root.GetProperty("notificationId").GetInt64().Should().Be(notification.Id);
    }
}
