using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Aizen.Modules.Notification.Repository.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// P1 — NewsletterSubscriberRepository over InMemory EF: the admin paged list combines status/lang/date filters and
/// pages (newest first), and the CSV export source returns ONLY Confirmed rows.
/// </summary>
public sealed class NewsletterSubscriberRepositoryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static NotificationDbContext NewDb()
        => new(new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase($"newsletter-{Guid.NewGuid():N}").Options);

    private static async Task<NewsletterSubscriberRepository> Seeded(params NewsletterSubscriberEntity[] rows)
    {
        var db = NewDb();
        await db.NewsletterSubscribers.AddRangeAsync(rows);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return new NewsletterSubscriberRepository(db);
    }

    private static NewsletterSubscriberEntity Row(
        string email, NewsletterSubscriberStatus status, string lang, DateTimeOffset createdAt)
        => NewsletterSubscriberEntity.CreateSeed(email, lang, status, createdAt,
            confirmedAt: status == NewsletterSubscriberStatus.Confirmed ? createdAt : null);

    // 10
    [Fact]
    public async Task Paged_list_filters_by_status_lang_and_date_range_and_pages()
    {
        var repo = await Seeded(
            Row("a@x.com", NewsletterSubscriberStatus.Confirmed,   "tr", T0.AddDays(1)),   // match
            Row("b@x.com", NewsletterSubscriberStatus.Confirmed,   "tr", T0.AddDays(2)),   // match
            Row("c@x.com", NewsletterSubscriberStatus.Confirmed,   "tr", T0.AddDays(3)),   // match
            Row("d@x.com", NewsletterSubscriberStatus.Pending,     "tr", T0.AddDays(2)),   // status mismatch
            Row("e@x.com", NewsletterSubscriberStatus.Confirmed,   "en", T0.AddDays(2)),   // lang mismatch
            Row("f@x.com", NewsletterSubscriberStatus.Confirmed,   "tr", T0.AddDays(40))); // out of date range

        var (items, total) = await repo.GetPagedAsync(
            NewsletterSubscriberStatus.Confirmed, "tr", null,
            from: T0, to: T0.AddDays(30), skip: 0, take: 2);

        total.Should().Be(3, "three Confirmed+tr rows fall inside the date window");
        items.Should().HaveCount(2, "page size 2");
        items[0].Email.Should().Be("c@x.com");   // newest first (CreatedAt desc)
        items[1].Email.Should().Be("b@x.com");

        var (page2, _) = await repo.GetPagedAsync(
            NewsletterSubscriberStatus.Confirmed, "tr", null, T0, T0.AddDays(30), skip: 2, take: 2);
        page2.Should().ContainSingle().Which.Email.Should().Be("a@x.com");
    }

    // 11
    [Fact]
    public async Task Confirmed_export_contains_only_confirmed_rows()
    {
        var repo = await Seeded(
            Row("conf1@x.com", NewsletterSubscriberStatus.Confirmed,    "tr", T0.AddDays(1)),
            Row("conf2@x.com", NewsletterSubscriberStatus.Confirmed,    "en", T0.AddDays(2)),
            Row("pend@x.com",  NewsletterSubscriberStatus.Pending,      "tr", T0.AddDays(3)),
            Row("unsub@x.com", NewsletterSubscriberStatus.Unsubscribed, "tr", T0.AddDays(4)),
            Row("supp@x.com",  NewsletterSubscriberStatus.Suppressed,   "tr", T0.AddDays(5)));

        var rows = await repo.GetConfirmedForExportAsync();

        rows.Should().HaveCount(2);
        rows.Select(r => r.Status).Should().OnlyContain(s => s == NewsletterSubscriberStatus.Confirmed);
        rows.Select(r => r.Email).Should().BeEquivalentTo(new[] { "conf1@x.com", "conf2@x.com" });
    }
}
