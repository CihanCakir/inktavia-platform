using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// FAZ15 (#34) — e-posta gönderiminin KALICI yazıldığını ve "Pending satırı = e-posta HİÇ gönderilmedi" değişmezini
/// kanıtlar. Eskiden yalnız failure yazılıyordu; başarı in-memory MarkAsSent ile kaybolup satır Pending kalıyordu.
///   • Başarılı gönderim → satır KALICI olarak Sent + provider ref taşır (persist sırası: Sending → Sent).
///   • Başarısız gönderim → satır KALICI Failed (persist sırası: Sending → Failed).
///   • Transport'tan SONRA çökme → satır "hiç denenmemiş" (Pending) görünmez: transport çalışırken KALICI durum
///     zaten Sending'dir, dolayısıyla o andan sonraki bir çökme Sending bırakır, asla Pending.
/// </summary>
public sealed class EmailDispatcherPersistenceFaz15Tests
{
    private sealed class FakeRepo : INotificationRepository
    {
        // UpdateAsync çağrıldığı ANDA entity.Status'ün değerini yakalar (enum value type → sıra korunur).
        public List<NotificationStatus> Persisted { get; } = new();
        public NotificationStatus? Last => Persisted.Count > 0 ? Persisted[^1] : (NotificationStatus?)null;

        public Task UpdateAsync(NotificationEntity entity, CancellationToken ct = default)
        {
            Persisted.Add(entity.Status);
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

    private sealed class FakeEmailSender : IEmailSender
    {
        private readonly FakeRepo _repo;
        private readonly bool _throw;
        public string ReturnedRef { get; } = "<abc123@inktavia.com>";
        // Transport çalıştığı ANDA satırın KALICI (repo'ya son yazılmış) durumu — before-send yazımının kanıtı.
        public NotificationStatus? DurableStatusAtSendTime { get; private set; }

        public FakeEmailSender(FakeRepo repo, bool @throw) { _repo = repo; _throw = @throw; }

        public Task<string> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
        {
            DurableStatusAtSendTime = _repo.Last;
            if (_throw) throw new InvalidOperationException("smtp down");
            return Task.FromResult(ReturnedRef);
        }
    }

    private sealed class UnusedIdentity : INotificationIdentityRemoteCall
    {
        public Task<AizenApiResponse<List<ProviderForAreaResult>>> GetProvidersForArea(string cityCode, string? categoryCode = null, int take = 500) => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAdminUserIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAllProviderProfileIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<List<long>>> GetAllParticipantProfileIds() => throw new NotImplementedException();
        public Task<AizenApiResponse<ParticipantProfileIdResult>> GetParticipantProfileIdByUserId(long userId) => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfileContactEmailResult>> GetProfileContactEmail(long profileId) => throw new NotImplementedException();
        public Task<AizenApiResponse<ProfilePreferredLanguageResult>> GetProfilePreferredLanguage(long profileId) => throw new NotImplementedException();
    }

    // recipientEmail'i metadata'dan çözdürürüz → identity remote-call'a hiç gidilmez.
    private static NotificationEntity NewEmailNotification() =>
        NotificationEntity.Create(
            recipientUserId: 42,
            type: NotificationType.ServiceRequestCreated,
            channel: NotificationChannel.Email,
            templateCode: "TPL",
            title: "Konu",
            body: "<p>gövde</p>",
            metadataJson: "{\"recipientEmail\":\"test@inktavia.com\"}");

    [Fact]
    public async Task Successful_send_persists_Sent_row_carrying_the_provider_ref()
    {
        var repo = new FakeRepo();
        var sender = new FakeEmailSender(repo, @throw: false);
        var sut = new EmailNotificationDispatcher(sender, repo, new UnusedIdentity(),
            NullLogger<EmailNotificationDispatcher>.Instance);
        var notification = NewEmailNotification();

        await sut.DispatchAsync(notification, CancellationToken.None);

        // Başarı ARTIK kalıcı (eskiden hiç yazılmıyordu) ve sıra Pending'ten çıkışı gösteriyor.
        repo.Persisted.Should().Equal(NotificationStatus.Sending, NotificationStatus.Sent);
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.DeliveryProviderRef.Should().Be(sender.ReturnedRef);
        notification.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Failing_send_persists_a_Failed_row()
    {
        var repo = new FakeRepo();
        var sender = new FakeEmailSender(repo, @throw: true);
        var sut = new EmailNotificationDispatcher(sender, repo, new UnusedIdentity(),
            NullLogger<EmailNotificationDispatcher>.Instance);
        var notification = NewEmailNotification();

        await sut.DispatchAsync(notification, CancellationToken.None);

        repo.Persisted.Should().Equal(NotificationStatus.Sending, NotificationStatus.Failed);
        notification.Status.Should().Be(NotificationStatus.Failed);
    }

    [Fact]
    public async Task Row_leaves_Pending_before_the_transport_runs_so_a_crash_after_send_is_not_untried()
    {
        var repo = new FakeRepo();
        var sender = new FakeEmailSender(repo, @throw: false);
        var sut = new EmailNotificationDispatcher(sender, repo, new UnusedIdentity(),
            NullLogger<EmailNotificationDispatcher>.Instance);

        await sut.DispatchAsync(NewEmailNotification(), CancellationToken.None);

        // Transport çalıştığı anda satır zaten KALICI olarak Pending DIŞINDA (Sending). Bu yüzden SendMailAsync
        // döndükten sonra Sent yazımından önce bir çökme olsa bile satır Sending kalır — asla Pending/"denenmemiş".
        sender.DurableStatusAtSendTime.Should().Be(NotificationStatus.Sending);
        sender.DurableStatusAtSendTime.Should().NotBe(NotificationStatus.Pending);
    }
}
