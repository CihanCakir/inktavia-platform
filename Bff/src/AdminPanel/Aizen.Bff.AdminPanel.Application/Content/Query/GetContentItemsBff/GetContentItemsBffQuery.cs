using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

/// <summary>
/// Paged admin item list (all statuses). Filters map 1:1 to the module's AdminContentListQueryParams —
/// type/status/surface/tag/lang only. categorySlug/search/date-range are intentionally NOT exposed
/// because the module's admin list endpoint cannot honour them (exposing would silently drop the filter).
/// </summary>
public sealed class GetContentItemsBffQuery : AizenQuery<ContentFeedBffResult>
{
    public ContentType? Type { get; init; }
    public ContentStatus? Status { get; init; }
    public ContentSurface? Surface { get; init; }
    public string? Tag { get; init; }
    public string Lang { get; init; } = "tr";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
