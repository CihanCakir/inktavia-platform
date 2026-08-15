using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebMyContentComments;

/// <summary>
/// GET /api/v1/web/me/content/items/{id}/my-comments — the participant's own comments on an item (any status),
/// so they can see whether each is Pending/Approved/etc.
/// </summary>
public sealed class GetWebMyContentCommentsQuery : AizenQuery<List<WebMyCommentDto>>
{
    public string ContentId { get; set; } = default!;
}
