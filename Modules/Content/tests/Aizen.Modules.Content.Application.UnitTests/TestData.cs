using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Domain.ValueObjects;
using MongoDB.Bson;

namespace Aizen.Modules.Content.Application.UnitTests;

internal static class TestData
{
    public static ContentItemDocument Item(
        ContentStatus status = ContentStatus.Published,
        ContentSurface surface = ContentSurface.MarineOsWeb,
        ContentAudienceType audience = ContentAudienceType.Public,
        DateTimeOffset? publishAt = null,
        DateTimeOffset? expireAt = null,
        int position = 1,
        DateTimeOffset? pinnedUntil = null,
        string defaultLang = "tr",
        (string Lang, string Title)[]? translations = null,
        string? slug = null)
    {
        var doc = new ContentItemDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Type = ContentType.Blog,
            Slug = slug ?? "s-" + Guid.NewGuid().ToString("N")[..8],
            Status = status,
            PublishAt = publishAt,
            ExpireAt = expireAt,
            DefaultLanguage = defaultLang,
            Placements = { new ContentPlacement { Surface = surface, Slot = ContentSlot.Feed, Position = position, PinnedUntil = pinnedUntil } },
            Audience = new ContentAudience { Type = audience },
            AuthorUserId = 1,
            CreatedAt = publishAt ?? DateTimeOffset.UtcNow,
        };
        foreach (var (lang, title) in translations ?? new[] { ("tr", "Başlık") })
            doc.Translations.Add(new ContentTranslation { Lang = lang, Title = title });
        return doc;
    }

    public static ContentCommentDocument Comment(string contentId, ContentCommentStatus status, long author = 500)
        => new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ContentId = contentId,
            AuthorUserId = author,
            Body = "body",
            Status = status,
        };
}
