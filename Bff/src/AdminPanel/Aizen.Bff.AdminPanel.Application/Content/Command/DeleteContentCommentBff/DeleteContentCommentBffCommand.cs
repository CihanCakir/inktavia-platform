using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Delete a comment. Returns the deleted comment (module echoes it back).</summary>
public sealed class DeleteContentCommentBffCommand : AizenCommand<ContentCommentBffDto>
{
    public string CommentId { get; init; } = default!;
}
