using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Queries.GetAdminContentComments;

/// <summary>
/// Moderation queue (§8): a content item's comments in ANY status, paged, optionally filtered by status
/// (e.g. Pending) so moderators can work the queue.
/// </summary>
public sealed class GetAdminContentCommentsQuery : AizenQuery<ContentCommentsResponse>
{
    public string ContentId { get; set; } = default!;
    public ContentCommentStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
