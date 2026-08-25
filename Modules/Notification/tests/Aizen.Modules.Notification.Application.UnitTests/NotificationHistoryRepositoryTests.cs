using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Aizen.Modules.Notification.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// Faz 28.5 — GetHistoryPagedAsync: filtreler (tarih aralığı + kanal + durum + templateCode + recipient) birleştirilebilir
/// ve tümü sorguya uygulanır; sıralama en yeni önce (CreatedAt DESC, Id tiebreak); sayfalama total ile tutarlı.
/// Inbox'un aksine Channel==InApp kırpması YOKTUR (Email/Push satırları da görünür). InMemory EF ile.
/// </summary>
public sealed class NotificationHistoryRepositoryTests
{
    private static NotificationDbContext NewDb()
        => new(new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase($"nhist-{Guid.NewGuid():N}").Options);

    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static NotificationEntity Row(
        DateTimeOffset createdAt,
        NotificationChannel channel = NotificationChannel.InApp,
        NotificationStatus status = NotificationStatus.Sent,
        string templateCode = "SR_CREATED_INAPP",
        long recipient = 1000)
    {
        // CreateSeed CreatedAt'i kontrol eder ama Status'ü daima Sent yazar; farklı durum istenirse davranış metoduyla değiştir.
        var e = NotificationEntity.CreateSeed(
            recipient, NotificationType.ServiceRequestCreated, channel, templateCode,
            "title", "body", metadataJson: null, createdAtUtc: createdAt, readAtUtc: null);
        if (status == NotificationStatus.Failed) e.MarkAsFailed();
        return e;
    }

    private static async Task<NotificationRepository> Seeded(params NotificationEntity[] rows)
    {
        var db = NewDb();
        await db.Notifications.AddRangeAsync(rows);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return new NotificationRepository(db);
    }

    [Fact]
    public async Task Combined_date_channel_status_filters_return_only_matching_rows()
    {
        var repo = await Seeded(
            Row(T0.AddDays(0),  NotificationChannel.Email, NotificationStatus.Sent),    // in range, match
            Row(T0.AddDays(1),  NotificationChannel.Email, NotificationStatus.Failed),  // status mismatch
            Row(T0.AddDays(2),  NotificationChannel.InApp, NotificationStatus.Sent),    // channel mismatch
            Row(T0.AddDays(30), NotificationChannel.Email, NotificationStatus.Sent));   // out of date range

        var (items, total) = await repo.GetHistoryPagedAsync(
            from: T0.AddDays(-1), to: T0.AddDays(10),
            channel: NotificationChannel.Email, status: NotificationStatus.Sent,
            templateCode: null, recipientUserId: null, campaignId: null, skip: 0, take: 20, ct: default);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].Channel.Should().Be(NotificationChannel.Email);
        items[0].Status.Should().Be(NotificationStatus.Sent);
        items[0].CreatedAt.Should().Be(T0);
    }

    [Fact]
    public async Task Orders_newest_first_by_createdAt_desc()
    {
        var repo = await Seeded(
            Row(T0.AddDays(1)),
            Row(T0.AddDays(3)),
            Row(T0.AddDays(2)));

        var (items, _) = await repo.GetHistoryPagedAsync(
            null, null, null, null, null, null, campaignId: null, skip: 0, take: 20, ct: default);

        items.Select(x => x.CreatedAt).Should().ContainInOrder(
            T0.AddDays(3), T0.AddDays(2), T0.AddDays(1));
    }

    [Fact]
    public async Task TemplateCode_and_recipient_filters_are_combinable()
    {
        var repo = await Seeded(
            Row(T0, templateCode: "SR_CREATED_INAPP", recipient: 1000),
            Row(T0, templateCode: "PAYMENT_CAPTURED_INAPP", recipient: 1000),
            Row(T0, templateCode: "SR_CREATED_INAPP", recipient: 2000));

        var (items, total) = await repo.GetHistoryPagedAsync(
            null, null, null, null,
            templateCode: "sr_created_inapp",   // case-insensitive: entity stores upper, filter uppercases
            recipientUserId: 1000, campaignId: null, skip: 0, take: 20, ct: default);

        total.Should().Be(1);
        items[0].TemplateCode.Should().Be("SR_CREATED_INAPP");
        items[0].RecipientUserId.Should().Be(1000);
    }

    [Fact]
    public async Task Includes_all_channels_not_just_inapp()
    {
        var repo = await Seeded(
            Row(T0, NotificationChannel.InApp),
            Row(T0.AddMinutes(1), NotificationChannel.Email),
            Row(T0.AddMinutes(2), NotificationChannel.Push),
            Row(T0.AddMinutes(3), NotificationChannel.Sms));

        var (items, total) = await repo.GetHistoryPagedAsync(
            null, null, null, null, null, null, campaignId: null, skip: 0, take: 20, ct: default);

        total.Should().Be(4);
        items.Select(x => x.Channel).Should().Contain(new[]
        {
            NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.Push, NotificationChannel.Sms,
        });
    }

    [Fact]
    public async Task CampaignId_filter_returns_only_that_campaigns_rows()
    {
        var db = NewDb();
        await db.Notifications.AddRangeAsync(
            NotificationEntity.Create(1000, NotificationType.AdminBroadcast, NotificationChannel.InApp, "ADMIN_CAMPAIGN_CUSTOM", "t", "b", campaignId: 5),
            NotificationEntity.Create(1000, NotificationType.AdminBroadcast, NotificationChannel.InApp, "ADMIN_CAMPAIGN_CUSTOM", "t", "b", campaignId: 9),
            NotificationEntity.Create(1000, NotificationType.ServiceRequestCreated, NotificationChannel.InApp, "X", "t", "b"));  // kampanyasız
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repo = new NotificationRepository(db);

        var (items, total) = await repo.GetHistoryPagedAsync(
            null, null, null, null, null, null, campaignId: 5, skip: 0, take: 20, ct: default);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].CampaignId.Should().Be(5);
    }

    [Fact]
    public async Task Paging_slices_result_and_total_counts_full_match()
    {
        var rows = Enumerable.Range(0, 5).Select(i => Row(T0.AddMinutes(i))).ToArray();
        var repo = await Seeded(rows);

        var (items, total) = await repo.GetHistoryPagedAsync(
            null, null, null, null, null, null, campaignId: null, skip: 2, take: 2, ct: default);

        total.Should().Be(5);
        items.Should().HaveCount(2);
        // en yeni önce: skip 2 → 3. ve 4. en yeniler (dakikalar: 2, 1)
        items.Select(x => x.CreatedAt).Should().ContainInOrder(T0.AddMinutes(2), T0.AddMinutes(1));
    }
}
