using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// FAZ16 (#34/#69) — InApp ve Push dispatcher'larının artık her iki sonucu da KALICI yazdığını ve "Pending satırı =
/// bildirim HİÇ iletilmedi" değişmezini kanıtlar. FAZ15 yaklaşımı: kalıcılık sınırını (UpdateAsync) kaydet, DİZİYİ
/// doğrula — niyeti değil. Ayrıca Push'un N-cihaz sonuç kuralı ve APNs (NotImplemented) davranışı test edilir.
/// </summary>
public sealed class InAppAndPushPersistenceFaz16Tests
{
    private sealed class FakeRepo : INotificationRepository
    {
        public List<NotificationStatus> Persisted { get; } = new();
        public Task UpdateAsync(NotificationEntity entity, CancellationToken ct = default)
        {
            Persisted.Add(entity.Status);   // enum value type → çağrı anındaki değer sıra korunarak yakalanır
            return Task.CompletedTask;
        }
        public Task AddAsync(NotificationEntity entity, CancellationToken ct = default) => Task.CompletedTask;
        public Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(List<NotificationEntity> Items, int TotalCount)> GetHistoryPagedAsync(DateTimeOffset? from, DateTimeOffset? to, NotificationChannel? channel, NotificationStatus? status, string? templateCode, long? recipientUserId, long? campaignId, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByRecipientAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> BulkMarkAsReadAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static NotificationEntity NewNotification(NotificationChannel channel) =>
        NotificationEntity.Create(42, NotificationType.ServiceRequestCreated, channel, "TPL", "Konu", "Gövde");

    // ── InApp ────────────────────────────────────────────────────────────────────────────────────────────

    private sealed class FakePusher : IInAppNotificationPusher
    {
        private readonly bool _throw;
        public FakePusher(bool @throw) => _throw = @throw;
        public Task PushToUserAsync(long userId, InAppNotificationPayload payload, CancellationToken ct)
            => _throw ? throw new InvalidOperationException("hub down") : Task.CompletedTask;
    }

    [Fact]
    public async Task InApp_success_persists_Sending_then_Sent()
    {
        var repo = new FakeRepo();
        var sut = new InAppNotificationDispatcher(new FakePusher(@throw: false), repo,
            NullLogger<InAppNotificationDispatcher>.Instance);

        await sut.DispatchAsync(NewNotification(NotificationChannel.InApp), CancellationToken.None);

        repo.Persisted.Should().Equal(NotificationStatus.Sending, NotificationStatus.Sent);
    }

    [Fact]
    public async Task InApp_failure_persists_Sending_then_Failed()
    {
        var repo = new FakeRepo();
        var sut = new InAppNotificationDispatcher(new FakePusher(@throw: true), repo,
            NullLogger<InAppNotificationDispatcher>.Instance);

        await sut.DispatchAsync(NewNotification(NotificationChannel.InApp), CancellationToken.None);

        repo.Persisted.Should().Equal(NotificationStatus.Sending, NotificationStatus.Failed);
    }

    // ── Push ─────────────────────────────────────────────────────────────────────────────────────────────

    // DeviceToken "fail" içeriyorsa fırlatır; aksi halde "fcm-ref-{token}" döner.
    private sealed class FakeFcm : IFcmSender
    {
        public List<string> CalledWith { get; } = new();
        public Task<string> SendAsync(string deviceToken, string title, string body, string? dataJson, CancellationToken ct)
        {
            CalledWith.Add(deviceToken);
            if (deviceToken.Contains("fail")) throw new InvalidOperationException("fcm error");
            return Task.FromResult($"fcm-ref-{deviceToken}");
        }
    }

    private sealed class UnusedWebPush : IPushSender
    {
        public Task<string> SendAsync(UserDeviceTokenEntity subscription, string title, string body, string? dataJson, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class FakeTokenRepo : IUserDeviceTokenRepository
    {
        private readonly List<UserDeviceTokenEntity> _tokens;
        public List<string> Deactivated { get; } = new();
        public FakeTokenRepo(params (string token, PushPlatform platform)[] tokens)
            => _tokens = tokens.Select(t => UserDeviceTokenEntity.CreateDeviceToken(42, t.token, t.platform)).ToList();
        public Task<List<UserDeviceTokenEntity>> GetActiveByUserAsync(long userId, CancellationToken ct = default)
            => Task.FromResult(_tokens);
        public Task DeactivateAsync(string token, CancellationToken ct = default) { Deactivated.Add(token); return Task.CompletedTask; }
        public Task<UserDeviceTokenEntity?> GetByTokenAsync(string token, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpsertAsync(long userId, string token, PushPlatform platform, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpsertWebPushAsync(long userId, string endpoint, string p256dhKey, string authKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeactivateByEndpointAsync(string endpoint, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static PushNotificationDispatcher Push(FakeRepo repo, FakeTokenRepo tokens, FakeFcm fcm) =>
        new(tokens, repo, fcm, new UnusedWebPush(), NullLogger<PushNotificationDispatcher>.Instance);

    [Fact]
    public async Task Push_three_tokens_one_success_persists_Sent_with_first_successful_ref_as_sample()
    {
        var repo = new FakeRepo();
        // sıra: fail, ok, fail → tek başarılı = "ok"; ref kuralı = İLK başarılı ref (örnek).
        var tokens = new FakeTokenRepo(("fail-1", PushPlatform.Fcm), ("ok", PushPlatform.Fcm), ("fail-2", PushPlatform.Fcm));
        var fcm = new FakeFcm();
        var notification = NewNotification(NotificationChannel.Push);

        await Push(repo, tokens, fcm).DispatchAsync(notification, CancellationToken.None);

        repo.Persisted.Should().Equal(NotificationStatus.Sending, NotificationStatus.Sent);
        notification.DeliveryProviderRef.Should().Be("fcm-ref-ok");   // ilk (ve tek) başarılı ref = örnek
        fcm.CalledWith.Should().Equal("fail-1", "ok", "fail-2");       // tüm token'lar denendi, hiçbiri terk edilmedi
    }

    [Fact]
    public async Task Push_all_tokens_fail_persists_Failed()
    {
        var repo = new FakeRepo();
        var tokens = new FakeTokenRepo(("fail-1", PushPlatform.Fcm), ("fail-2", PushPlatform.Fcm));
        var notification = NewNotification(NotificationChannel.Push);

        await Push(repo, tokens, new FakeFcm()).DispatchAsync(notification, CancellationToken.None);

        repo.Persisted.Should().Equal(NotificationStatus.Sending, NotificationStatus.Failed);
        notification.DeliveryProviderRef.Should().BeNull();
    }

    [Fact]
    public async Task Push_with_APNs_token_still_attempts_the_others()
    {
        var repo = new FakeRepo();
        // APNs (NotImplemented) araya konur; rethrow ETMEMELİ — sonraki FCM token denenmeli.
        var tokens = new FakeTokenRepo(("apns-tok", PushPlatform.Apns), ("ok", PushPlatform.Fcm));
        var fcm = new FakeFcm();
        var notification = NewNotification(NotificationChannel.Push);

        await Push(repo, tokens, fcm).DispatchAsync(notification, CancellationToken.None);

        fcm.CalledWith.Should().Contain("ok");                          // APNs atlandı, FCM yine de denendi
        repo.Persisted.Should().Equal(NotificationStatus.Sending, NotificationStatus.Sent);
        notification.DeliveryProviderRef.Should().Be("fcm-ref-ok");
    }

    [Fact]
    public async Task Push_no_tokens_persists_Failed_not_left_Pending()
    {
        var repo = new FakeRepo();
        var notification = NewNotification(NotificationChannel.Push);

        await Push(repo, new FakeTokenRepo(), new FakeFcm()).DispatchAsync(notification, CancellationToken.None);

        // Hedef yok → belirsiz Pending BIRAKILMAZ; terminal iletilememe = Failed.
        repo.Persisted.Should().Equal(NotificationStatus.Failed);
        notification.Status.Should().NotBe(NotificationStatus.Pending);
    }
}
