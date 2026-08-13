using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;
using Aizen.Modules.Content.Domain.MongoDocuments;
using Aizen.Modules.Content.Domain.ValueObjects;

namespace Aizen.Modules.Content.Application.Services;

/// <summary>
/// Applies the public visibility window, surface-relative ordering and paging to a candidate set,
/// then projects to <see cref="ContentItemSummaryDto"/> rows.
///
/// The publish-window predicate (PublishAt &lt;= now &amp;&amp; (ExpireAt == null || now &lt; ExpireAt)) is
/// applied here in memory rather than in the Mongo query: the driver serializes DateTimeOffset as a
/// composite value, so range filters on it do not translate to reliable server-side comparisons.
/// Ordering is by the placement matched for the requested surface (pinned first, then Position, then
/// PublishAt desc) — a per-item derived key, so paging is also done here (see the PERF note in the callers).
/// </summary>
public static class ContentFeedProjection
{
    public static ContentFeedResponse Build(
        IEnumerable<ContentItemDocument> candidates,
        ContentSurface surface,
        string lang,
        int page,
        int pageSize,
        DateTimeOffset now)
    {
        var visible = candidates
            .Where(d => d.PublishAt is not null
                        && d.PublishAt <= now
                        && (d.ExpireAt is null || now < d.ExpireAt))
            .Select(d => new { Doc = d, Placement = MatchPlacement(d, surface) })
            .Where(x => x.Placement is not null)
            .OrderByDescending(x => IsPinned(x.Placement!, now))   // pinned first
            .ThenBy(x => x.Placement!.Position)                     // then placement position asc
            .ThenByDescending(x => x.Doc.PublishAt)                 // then newest first
            .ToList();

        var total = visible.Count;
        var items = visible
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ContentMapper.ToSummary(x.Doc, lang))
            .ToList();

        return new ContentFeedResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total,
        };
    }

    private static ContentPlacement? MatchPlacement(ContentItemDocument doc, ContentSurface surface)
        => doc.Placements
            .Where(p => p.Surface == surface)
            .OrderBy(p => p.Position)
            .FirstOrDefault();

    private static bool IsPinned(ContentPlacement placement, DateTimeOffset now)
        => placement.PinnedUntil is not null && placement.PinnedUntil > now;
}
