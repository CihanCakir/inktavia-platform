using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.MongoDocuments;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Shared visibility predicates. "Engageable" = an authenticated participant may comment/favorite it:
/// Published and inside its publish window. (Audience is NOT restricted here — a logged-in participant
/// may engage with participant-targeted content too; soft-deleted items are already excluded by the
/// repository's global filter.)
/// </summary>
public static class ContentVisibility
{
    public static bool IsEngageable(ContentItemDocument item, DateTimeOffset now)
        => item.Status == ContentStatus.Published
           && item.PublishAt is not null && item.PublishAt <= now
           && (item.ExpireAt is null || now < item.ExpireAt);
}
