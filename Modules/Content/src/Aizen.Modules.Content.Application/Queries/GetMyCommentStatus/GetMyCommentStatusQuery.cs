using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Queries.GetMyCommentStatus;

/// <summary>
/// The caller's own comments on a content item, any status (§8) — so a participant can see whether
/// their comment is Pending/Approved/Rejected/Hidden. Identity from the token.
/// </summary>
public sealed class GetMyCommentStatusQuery : AizenQuery<List<ContentCommentDto>>
{
    public string ContentId { get; set; } = default!;
}
