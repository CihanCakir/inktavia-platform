using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.DeleteContentComment;

/// <summary>Soft-delete a comment (§8). Returns the pre-delete snapshot.</summary>
public sealed class DeleteContentCommentCommand : AizenCommand<ContentCommentDto>
{
    public string CommentId { get; set; } = default!;
}
