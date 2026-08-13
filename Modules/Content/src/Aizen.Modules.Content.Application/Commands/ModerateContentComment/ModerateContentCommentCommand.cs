using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;
using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Application.Commands.ModerateContentComment;

/// <summary>
/// Moderate a comment (§8): set its target status to Approved, Rejected or Hidden. Pending is not a
/// valid moderation target (C3 ambiguity #3).
/// </summary>
public sealed class ModerateContentCommentCommand : AizenCommand<ContentCommentDto>
{
    public string CommentId { get; set; } = default!;
    public ContentCommentStatus Status { get; set; }
    public string? Reason { get; set; }
}
