using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Query.GetNotificationHistoryDetail;
using Aizen.Modules.Notification.Application.Query.GetNotificationHistoryPaged;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// Faz 28.5 — Geçmiş sorgu handler'ları: sayfa/boyut güvenli aralığa çekilir, filtreler repo'ya olduğu gibi geçer,
/// entity→DTO eşlemesi tam; detay bulunamazsa null döner. Ev tarzı: elle yazılmış fake repo (filtre argümanlarını kaydeder).
/// </summary>
public sealed class NotificationHistoryQueryHandlerTests
{
    private sealed class FakeRepo : INotificationRepository
    {
        private readonly List<NotificationEntity> _items;
        private readonly int _total;
        private readonly NotificationEntity? _byId;

        // Kaydedilen çağrı argümanları (passthrough doğrulaması için).
        public (DateTimeOffset? From, DateTimeOffset? To, NotificationChannel? Channel, NotificationStatus? Status,
            string? TemplateCode, long? RecipientUserId, long? CampaignId, int Skip, int Take)? LastCall;

        public FakeRepo(List<NotificationEntity>? items = null, int total = 0, NotificationEntity? byId = null)
        {
            _items = items ?? new();
            _total = total;
            _byId  = byId;
        }

        public Task<(List<NotificationEntity> Items, int TotalCount)> GetHistoryPagedAsync(
            DateTimeOffset? from, DateTimeOffset? to, NotificationChannel? channel, NotificationStatus? status,
            string? templateCode, long? recipientUserId, long? campaignId, int skip, int take, CancellationToken ct = default)
        {
            LastCall = (from, to, channel, status, templateCode, recipientUserId, campaignId, skip, take);
            return Task.FromResult((_items, _total));
        }

        public Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default) => Task.FromResult(_byId);

        public Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountByRecipientAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(NotificationEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(NotificationEntity entity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> BulkMarkAsReadAsync(long userId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static NotificationEntity Sample() => NotificationEntity.CreateSeed(
        recipientUserId: 42, NotificationType.ServiceRequestCreated, NotificationChannel.Email, "SR_CREATED_INAPP",
        "Hello", "Body text", metadataJson: "{\"k\":1}",
        createdAtUtc: new DateTimeOffset(2026, 2, 1, 10, 0, 0, TimeSpan.Zero),
        readAtUtc: null, referenceType: "ServiceRequest", referenceId: 7, locale: "tr", deepLink: "app://sr/7");

    // ── Paged handler ──────────────────────────────────────────────────────────
    [Fact]
    public async Task Paged_clamps_page_and_pageSize_and_computes_skip()
    {
        var repo = new FakeRepo(items: new(), total: 0);
        var handler = new GetNotificationHistoryPagedQueryHandler(repo);

        var result = await handler.Handle(
            new GetNotificationHistoryPagedQuery { Page = 0, PageSize = 999 }, CancellationToken.None);

        // page<1 → 1; pageSize>200 → 20; skip=(1-1)*20=0
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        repo.LastCall!.Value.Skip.Should().Be(0);
        repo.LastCall.Value.Take.Should().Be(20);
    }

    [Fact]
    public async Task Paged_passes_all_filters_through_to_repository()
    {
        var repo = new FakeRepo(items: new(), total: 0);
        var handler = new GetNotificationHistoryPagedQueryHandler(repo);
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to   = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        await handler.Handle(new GetNotificationHistoryPagedQuery
        {
            From = from, To = to, Channel = NotificationChannel.Email, Status = NotificationStatus.Failed,
            TemplateCode = "SR_CREATED_INAPP", RecipientUserId = 42, CampaignId = 77, Page = 3, PageSize = 10,
        }, CancellationToken.None);

        var c = repo.LastCall!.Value;
        c.From.Should().Be(from);
        c.To.Should().Be(to);
        c.Channel.Should().Be(NotificationChannel.Email);
        c.Status.Should().Be(NotificationStatus.Failed);
        c.TemplateCode.Should().Be("SR_CREATED_INAPP");
        c.RecipientUserId.Should().Be(42);
        c.CampaignId.Should().Be(77);
        c.Skip.Should().Be(20);   // (3-1)*10
        c.Take.Should().Be(10);
    }

    [Fact]
    public async Task Paged_maps_entity_fields_to_list_item_dto()
    {
        var repo = new FakeRepo(items: new() { Sample() }, total: 1);
        var handler = new GetNotificationHistoryPagedQueryHandler(repo);

        var result = await handler.Handle(new GetNotificationHistoryPagedQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        var item = result.Items.Should().ContainSingle().Subject;
        item.RecipientUserId.Should().Be(42);
        item.Channel.Should().Be(NotificationChannel.Email);
        item.TemplateCode.Should().Be("SR_CREATED_INAPP");
        item.Title.Should().Be("Hello");
        item.Locale.Should().Be("tr");
        item.Status.Should().Be(NotificationStatus.Sent);
        item.SentAt.Should().NotBeNull();
    }

    // ── Detail handler ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Detail_maps_full_row_including_body_and_metadata()
    {
        var repo = new FakeRepo(byId: Sample());
        var handler = new GetNotificationHistoryDetailQueryHandler(repo);

        var result = await handler.Handle(new GetNotificationHistoryDetailQuery { Id = 1 }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Body.Should().Be("Body text");
        result.MetadataJson.Should().Be("{\"k\":1}");
        result.DeepLink.Should().Be("app://sr/7");
        result.ReferenceType.Should().Be("ServiceRequest");
        result.ReferenceId.Should().Be(7);
    }

    [Fact]
    public async Task Detail_returns_null_when_not_found()
    {
        var repo = new FakeRepo(byId: null);
        var handler = new GetNotificationHistoryDetailQueryHandler(repo);

        var result = await handler.Handle(new GetNotificationHistoryDetailQuery { Id = 999 }, CancellationToken.None);

        result.Should().BeNull();
    }
}
