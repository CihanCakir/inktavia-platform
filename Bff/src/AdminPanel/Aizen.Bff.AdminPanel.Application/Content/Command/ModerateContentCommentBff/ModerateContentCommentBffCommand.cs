using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Moderate a comment (Approved/Rejected/Hidden). Returns the moderated comment.</summary>
public sealed class ModerateContentCommentBffCommand : AizenCommand<ContentCommentBffDto>
{
    public string CommentId { get; init; } = default!;
    public ModerateContentCommentRequest Body { get; init; } = default!;
}
