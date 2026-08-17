using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentComments;

/// <summary>
/// GET /api/v1/web/content/items/{id}/comments — approved comments for a content item (paged).
/// {id} is the Content string id (from the feed/detail DTOs).
/// </summary>
public sealed class GetWebContentCommentsQuery : AizenQuery<WebContentCommentsResponse>
{
    public string ContentId { get; set; } = default!;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
