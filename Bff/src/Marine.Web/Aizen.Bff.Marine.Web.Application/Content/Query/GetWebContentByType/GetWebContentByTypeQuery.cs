using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentByType;

/// <summary>
/// GET /api/v1/web/content/by-type/{type} — the MarineOsWeb feed narrowed to a single content type.
/// Surface pinned by the BFF; caller supplies type + lang/paging.
/// </summary>
public sealed class GetWebContentByTypeQuery : AizenQuery<ContentFeedResponse>
{
    public ContentType Type { get; set; }
    public string Lang { get; set; } = "tr";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
