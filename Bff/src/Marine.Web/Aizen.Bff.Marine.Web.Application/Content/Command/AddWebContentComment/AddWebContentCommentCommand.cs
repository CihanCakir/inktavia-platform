using Aizen.Bff.Marine.Web.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Content.Command.AddWebContentComment;

/// <summary>
/// POST /api/v1/web/me/content/items/{id}/comments — the authenticated participant comments on a content item.
/// Author identity is never in the body; it rides the resolved identity holder → assertion headers.
/// </summary>
public sealed class AddWebContentCommentCommand : AizenCommand<WebMyCommentDto>
{
    public string ContentId { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? ParentCommentId { get; set; }
}
