using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.Interface.Repository;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Content.Repository.Seed;

/// <summary>
/// Optional demo content seed (C9), OFF by default. Enable with <c>Content:Seed:Demo=true</c> so a fresh
/// environment has a few published items across types / surfaces / languages to render. Idempotent:
/// skips if the demo items already exist.
/// </summary>
public sealed class ContentDemoSeed
{
    private const string GuardSlug = "demo-welcome-marineos";

    private readonly IContentItemRepository _items;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContentDemoSeed> _logger;

    public ContentDemoSeed(IContentItemRepository items, IConfiguration configuration, ILogger<ContentDemoSeed> logger)
    {
        _items = items;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!_configuration.GetValue("Content:Seed:Demo", false))
            return;

        if (await _items.GetBySlugAsync(GuardSlug, ct) is not null)
            return; // already seeded — idempotent

        var now = DateTimeOffset.UtcNow;
        foreach (var doc in BuildDemoItems(now))
            await _items.AddAsync(doc, ct);

        _logger.LogInformation("Content demo seed inserted {Count} items.", 3);
    }

    private static IEnumerable<ContentItemDocument> BuildDemoItems(DateTimeOffset now)
    {
        yield return Item(
            ContentType.Blog, GuardSlug, ContentSurface.MarineOsWeb, ContentSlot.HomeHero, now.AddDays(-1),
            ("tr", "Inktavia Marine OS'e hoş geldiniz", "Denizcilik operasyonlarınız için tek platform."),
            ("en", "Welcome to Inktavia Marine OS", "One platform for your marine operations."));

        yield return Item(
            ContentType.Announcement, "demo-provider-announcement", ContentSurface.Provider, ContentSlot.Feed, now.AddDays(-2),
            ("tr", "Sağlayıcı paneli güncellemesi", "Yeni teklif akışı yayında."));

        yield return Item(
            ContentType.ReleaseNote, "demo-app-release", ContentSurface.MobileParticipant, ContentSlot.Feed, now.AddHours(-3),
            ("tr", "Mobil uygulama 1.2.0", "Performans iyileştirmeleri ve hata düzeltmeleri."),
            ("en", "Mobile app 1.2.0", "Performance improvements and bug fixes."));
    }

    private static ContentItemDocument Item(
        ContentType type, string slug, ContentSurface surface, ContentSlot slot, DateTimeOffset publishAt,
        params (string Lang, string Title, string Summary)[] translations)
    {
        var doc = new ContentItemDocument
        {
            Type = type,
            Slug = slug,
            Status = ContentStatus.Published,
            PublishAt = publishAt,
            PublishedAt = publishAt,
            DateKey = publishAt.UtcDateTime.ToString("yyyy-MM-dd"),
            DefaultLanguage = "tr",
            Placements = { new ContentPlacement { Surface = surface, Slot = slot, Position = 1 } },
            Audience = new ContentAudience { Type = ContentAudienceType.Public },
            Tags = { "demo" },
            AuthorUserId = 0,
            CreatedAt = publishAt,
            UpdatedAt = publishAt,
        };
        foreach (var (lang, title, summary) in translations)
            doc.Translations.Add(new ContentTranslation { Lang = lang, Title = title, Summary = summary });

        if (type == ContentType.ReleaseNote)
            doc.ReleaseNote = new ContentReleaseNoteBlock { AppTarget = "MobileParticipant", Version = "1.2.0", Platform = ReleaseNotePlatform.All };

        return doc;
    }
}
