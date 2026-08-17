using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Queries.GetAdminContentList;

/// <summary>
/// Admin content list (§8): filter by type/status/surface/tag, paged. Includes ALL statuses
/// (Draft/Scheduled/Published/Archived) — unlike the public feed. Soft-deleted items are excluded
/// by the repository's global filter.
/// </summary>
public sealed class GetAdminContentListQuery : AizenQuery<ContentFeedResponse>
{
    public ContentType? Type { get; set; }
    public ContentStatus? Status { get; set; }
    public ContentSurface? Surface { get; set; }
    public string? Tag { get; set; }

    public string Lang { get; set; } = "tr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
